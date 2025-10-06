import {Signal} from "signal-polyfill";
import {SignalSet} from "signal-utils/set";
import {InterestIndex} from "./interest-index";
import * as PushSubscriptionsController from "./push-notifications-controller";
import * as KvStore from "./indexed-db-kv-store";

// whether the user has followed any push interests
export const pushEnabled = new Signal.Computed(() => {
    for (const interestId of InterestIndex.subscriptions) {
        if (InterestIndex.interests.get(interestId)?.name.includes('Push')) {
            return true;
        }
    }
    return false;
});

const NoPushLocalStorageKey = "NoPush";

// true if the user doesn't want to receive push notifications on this device
export const noPushOnThisDevice = new Signal.State<boolean>(
    localStorage.getItem(NoPushLocalStorageKey) !== null
);

// Currently registered endpoints
export const pushEndpointHashes = new SignalSet<string>(window.registeredPushEndpointHashes);

// All endpoints exposed by this device
export const deviceEndpoints = new SignalSet<string>();

export const shouldShowPushWarning = new Signal.Computed(() => {
    return pushEndpointHashes.size === 0 && !noPushOnThisDevice.get();
});

export const permissionState = new Signal.State<NotificationPermission>(Notification.permission);

export async function registerDevice() {
    noPushOnThisDevice.set(false);
    localStorage.removeItem(NoPushLocalStorageKey);
}

export async function unregisterDevice() {
    
}

// the user doesn't want to receive push notifications on this device
export async function disablePushOnThisDevice() {
    noPushOnThisDevice.set(true);
    localStorage.setItem(NoPushLocalStorageKey, "");
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

// register device
// receive and store endpoint
// wait
// endpoint exposed by the browser changed
// we have to unregister the old endpoint and register the new one
function validateRegistrations(): Promise<void> {
    // only do this once per page load
    return validateCache ??= (async () => {
        const subscription = await getPushSubscription();
        const registeredEndpoint = await KvStore.get("pushEndpoint");

        if (registeredEndpoint)
            deviceEndpoints.add(registeredEndpoint);
        if (subscription)
            deviceEndpoints.add(subscription.endpoint);

        if (registeredEndpoint && subscription?.endpoint !== registeredEndpoint) {
            await PushSubscriptionsController.unregisterPushEndpoint(registeredEndpoint);
        }

        // why no register?
    })();
}
let validateCache: Promise<void>;

async function validateRegistrationsCore() {
}

async function getPushSubscription(): Promise<PushSubscription | null> {
    const permission = Notification.permission;
    if (permission !== 'granted') return null;
    return await (await getServiceWorker()).pushManager.getSubscription();
}

function refreshPermissionState() {
    permissionState.set(Notification.permission)
}
