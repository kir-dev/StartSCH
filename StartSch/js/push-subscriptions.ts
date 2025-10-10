import {Signal} from "signal-polyfill";
import {SignalSet} from "signal-utils/set";
import {InterestIndex} from "./interest-index";
import {effect} from "signal-utils/subtle/microtask-effect";
import {computeSha256} from "./utils";
import * as KvStore from "./indexed-db-kv-store";
import * as PushSubscriptionsController from "./push-subscriptions-controller";

export const isBusy = new Signal.State(false);

// whether the user has followed any push interests
export const pushInterestsFollowed = new Signal.Computed(() => {
    for (const interestId of InterestIndex.subscriptions) {
        if (InterestIndex.interests.get(interestId)?.name.includes('Push')) {
            return true;
        }
    }
    return false;
});

// true if the user doesn't want to receive push notifications on this device
export const noPushOnThisDevice = new Signal.State(
    localStorage.getItem("NoPush") !== null
);

effect(() => {
    if (noPushOnThisDevice.get())
        localStorage.setItem("NoPush", "");
    else
        localStorage.removeItem("NoPush");
});

// Currently registered endpoints
export const registeredEndpointHashes = new SignalSet(window.registeredPushEndpointHashesGlobal);

export const hasRegisteredDevices = new Signal.Computed(() =>
    registeredEndpointHashes.size !== 0
);

// All endpoints that came from this device
export const deviceEndpointHashes = new SignalSet<string>();

export const suggestSubscribing = new Signal.Computed(() =>
    pushInterestsFollowed.get() && !hasRegisteredDevices.get() && !noPushOnThisDevice.get() && !isBusy.get()
);

let currentEndpoint: string | null = null;
export const currentEndpointHash = new Signal.State<string | null>(null);

effect(() => {
    const endpoint = currentEndpointHash.get();
    if (endpoint)
        deviceEndpointHashes.add(endpoint);
});

export const isThisDeviceRegistered = new Signal.Computed(() => {
    const current = currentEndpointHash.get();
    return current !== null
        && registeredEndpointHashes.has(current);
});

export const permissionState = new Signal.State(Notification.permission);

export const countOfSubscriptionsOnOtherDevices = new Signal.Computed(() =>
    [...registeredEndpointHashes.keys()]
        .filter(x => !deviceEndpointHashes.has(x))
        .length
);

// Initialize
(async () => {
    isBusy.set(true);
    try {
        // subscribe to the permissions query
        const permissionStatus = await navigator.permissions.query({name: 'notifications'});
        permissionStatus.onchange = async () => {
            permissionState.set(Notification.permission);

            // unsubscribe if the permission has been revoked after the page has been loaded
            if (currentEndpoint && Notification.permission !== "granted") {
                const endpoint = currentEndpoint;
                const hash = currentEndpointHash.get();
                currentEndpoint = null;
                currentEndpointHash.set(null);
                registeredEndpointHashes.delete(hash!);
                await PushSubscriptionsController.unregisterPushEndpoint(endpoint);
            }
        };

        const storedEndpoint = await KvStore.get(PushSubscriptionsController.PushEndpointLocalStorageKey);
        if (storedEndpoint && Notification.permission !== "granted") {
            // user revoked notification permission while the site was closed
            const hash = await computeSha256(storedEndpoint);
            deviceEndpointHashes.add(hash);
            await PushSubscriptionsController.unregisterPushEndpoint(storedEndpoint);
        }

        const subscription = await (await getServiceWorker()).pushManager.getSubscription();
        if (subscription) {
            currentEndpoint = subscription.endpoint;
            const hash = await computeSha256(currentEndpoint);
            currentEndpointHash.set(hash);

            // the endpoint changed without the service worker being notified.
            // might want to log, as this really shouldn't happen.
            // the old endpoint is unregistered automatically
            if (storedEndpoint && storedEndpoint !== currentEndpoint)
                await PushSubscriptionsController.registerPushSubscription(subscription);
        }
    } catch (e) {
        console.error(e);
    } finally {
        isBusy.set(false);
    }
})();

window.beforeSignOut = async (event: SubmitEvent) => {
    event.preventDefault();

    noPushOnThisDevice.set(false);
    await unregisterDevice();

    (event.target as HTMLFormElement).submit();
};

export async function registerDevice() {
    if (isBusy.get())
        throw new Error("isBusy");
    isBusy.set(true);

    try {
        noPushOnThisDevice.set(false);

        const permissionState = await Notification.requestPermission();
        if (permissionState !== 'granted')
            return;

        const pushSubscription = await (await getServiceWorker()).pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: await getVapidPublicKey()
        });
        await PushSubscriptionsController.registerPushSubscription(pushSubscription);

        const hash = await computeSha256(pushSubscription.endpoint);
        currentEndpointHash.set(hash);
        registeredEndpointHashes.add(hash);
        currentEndpoint = pushSubscription.endpoint;
    } catch (e) {
        console.error(e);
    } finally {
        isBusy.set(false);
    }
}

export async function unregisterDevice() {
    if (isBusy.get())
        throw new Error("isBusy");
    isBusy.set(true);

    try {
        const pushSubscription = await (await getServiceWorker()).pushManager.getSubscription();
        if (!pushSubscription)
            return;
        await pushSubscription.unsubscribe();
        await PushSubscriptionsController.unregisterPushEndpoint(pushSubscription.endpoint)
        const hash = await computeSha256(pushSubscription.endpoint);
        currentEndpointHash.set(null);
        registeredEndpointHashes.delete(hash);
        currentEndpoint = null;
    } catch (e) {
        console.error(e);
    } finally {
        isBusy.set(false);
    }
}

// based on https://github.com/firebase/firebase-js-sdk/blob/ccbf7ba36f/packages/messaging/src/helpers/registerDefaultSw.ts#L43
function getServiceWorker(): Promise<ServiceWorkerRegistration> {
    return serviceWorkerCache
        ??= navigator.serviceWorker
        .register('sw.js')
        .then(async sw => {
            if (localStorage.getItem('serviceWorkerFingerprint') === window.serviceWorkerFingerprint)
                return sw;
            await sw.update();
            localStorage.setItem('serviceWorkerFingerprint', window.serviceWorkerFingerprint);
            return sw;
        });
}

let serviceWorkerCache: Promise<ServiceWorkerRegistration>;

function getVapidPublicKey(): Promise<BufferSource> {
    return vapidPublicKeyCache ??= PushSubscriptionsController.retrievePublicKey()
}

let vapidPublicKeyCache: Promise<BufferSource>;
