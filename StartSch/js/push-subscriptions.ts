// register:
//   request permission
//   

import {Signal} from "signal-polyfill";
import {SignalSet} from "signal-utils/set";
import {InterestIndex} from "./interest-index";
import * as PushSubscriptionsController from "./push-subscriptions-controller";
import * as KvStore from "./indexed-db-kv-store";
import {retrievePublicKey, unregisterPushEndpoint} from "./push-subscriptions-controller";
import {ExternalSignal} from "./ExternalSignal";
import {effect} from "signal-utils/subtle/microtask-effect";
import {computeSha256} from "./utils";

export const isBusy = new Signal.State(false);

// whether the user has followed any push interests
export const pushInterest = new Signal.Computed(() => {
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

const storedEndpoint = localStorage.getItem(PushSubscriptionsController.PushEndpointLocalStorageKey);

// All endpoints that came from this device
export const deviceEndpointHashes = new SignalSet<string>();

export const shouldShowPushWarning = new Signal.Computed(() =>
    pushInterest.get() && !hasRegisteredDevices.get() && !noPushOnThisDevice.get() && !isBusy.get()
);

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

// initialize:
// 1. subscribe to permissions query
// 2. get the stored endpoint and hash it
// 3. add stored endpoint to device endpoints
// 4. unregister if the notification permission has been revoked
// 5. get current subscription
(async () => {
    isBusy.set(true);
    try {
        const permissionStatus = await navigator.permissions.query({ name: 'notifications' });
        permissionStatus.onchange = () => {
            permissionState.set(Notification.permission);
        };
        
        if (storedEndpoint) {
            const hash = await computeSha256(storedEndpoint);
            deviceEndpointHashes.add(hash);

            if (Notification.permission !== "granted")
                await PushSubscriptionsController.unregisterPushEndpoint(storedEndpoint);
        }

        const subscription = await getPushSubscription();
        if (subscription) {
            const hash = await computeSha256(subscription.endpoint);
            currentEndpointHash.set(hash);
        }
    } catch (e) {
        console.error(e);
    } finally {
        isBusy.set(false);
    }
})();

export async function registerDevice() {
    if (isBusy.get()) return;
    isBusy.set(true);

    try {
        noPushOnThisDevice.set(false);

        const permissionState = await Notification.requestPermission();
        if (permissionState !== 'granted')
            return;

        const sw = await getServiceWorker();
        const pushSubscription = await sw.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: await getVapidPublicKey()
        })
        await PushSubscriptionsController.registerPushSubscription(pushSubscription)

        const hash = await computeSha256(pushSubscription.endpoint);
        currentEndpointHash.set(hash);
    } finally {
        isBusy.set(false);
    }
}

export async function unregisterDevice() {
    if (isBusy.get()) return;
    isBusy.set(true);

    try {
        const pushSubscription = await (await getServiceWorker()).pushManager.getSubscription();
        if (!pushSubscription)
            return;
        await pushSubscription.unsubscribe();
        await unregisterPushEndpoint(pushSubscription.endpoint)
    } finally {
        isBusy.set(false);
    }
}

// the user doesn't want to receive push notifications on this device
export async function disablePushOnThisDevice() {
    noPushOnThisDevice.set(true);
}

// based on https://github.com/firebase/firebase-js-sdk/blob/ccbf7ba36f/packages/messaging/src/helpers/registerDefaultSw.ts#L43
function getServiceWorker(): Promise<ServiceWorkerRegistration> {
    return serviceWorkerCache
        ??= navigator.serviceWorker
        .register('sw.js')
        .then(async sw => {
            if (!window)
                return sw;
            if (localStorage.getItem('serviceWorkerFingerprint') !== window.serviceWorkerFingerprint) {
                await sw.update();
                localStorage.setItem('serviceWorkerFingerprint', window.serviceWorkerFingerprint);
            }
            return sw;
        });
}

let serviceWorkerCache: Promise<ServiceWorkerRegistration>;

function getVapidPublicKey(): Promise<BufferSource> {
    return vapidPublicKeyCache ??= PushSubscriptionsController.retrievePublicKey()
}

let vapidPublicKeyCache: Promise<BufferSource>;

// INLINE?
async function getPushSubscription(): Promise<PushSubscription | null> {
    const permission = Notification.permission;
    if (permission !== 'granted') return null;
    return await (await getServiceWorker()).pushManager.getSubscription();
}
