// Based on https://github.com/tpeczek/Demo.AspNetCore.PushNotifications/blob/58f9c836651ce9d9f50d68f16cc55f9e312eb722/Demo.AspNetCore.PushNotifications/wwwroot/scripts/push-notifications.js

import {registerPushSubscription, retrievePublicKey, unregisterPushEndpoint} from "./push-notifications-controller";
import * as kvStore from "./indexed-db-kv-store";

let applicationServerPublicKey;
let serviceWorkerCache: Promise<ServiceWorkerRegistration>;

// Keeps track of all push endpoints exposed by this browser since the page loaded.
const prevDeviceEndpoints = new Set<string>();

// https://github.com/firebase/firebase-js-sdk/blob/ccbf7ba36f/packages/messaging/src/helpers/registerDefaultSw.ts#L43
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

getServiceWorker().then(r => {});

async function getPushSubscription(): Promise<PushSubscription | null> {
    const permission = Notification.permission;
    if (permission !== 'granted') return null;
    return await (await getServiceWorker()).pushManager.getSubscription();
}

// unregister invalid/outdated endpoints, so they don't show up in the active count
async function checkSubscriptionRegistration() {
    const subscription = await getPushSubscription();
    const registeredEndpoint = await kvStore.get("pushEndpoint");

    if (registeredEndpoint)
        prevDeviceEndpoints.add(registeredEndpoint);
    if (subscription)
        prevDeviceEndpoints.add(subscription.endpoint);

    if (registeredEndpoint && subscription?.endpoint !== registeredEndpoint) {
        await unregisterPushEndpoint(registeredEndpoint);
    }
}

export type PushSubscriptionState = {
    prevDeviceEndpoints: string[];
    currentEndpoint: string | undefined;
    permissionState: NotificationPermission;
};

export async function getPushSubscriptionState(): Promise<PushSubscriptionState> {
    await checkSubscriptionRegistration();

    const permission = Notification.permission;
    const subscription = await getPushSubscription();
    return {
        permissionState: permission,
        currentEndpoint: subscription?.endpoint,
        prevDeviceEndpoints: [...prevDeviceEndpoints],
    };
}

export async function subscribeToPushNotifications(): Promise<string | null> {
    document.cookie = 'No-Push=; Expires=Thu, 01 Jan 1970 00:00:00 UTC; SameSite=Lax; Path=/; Secure';

    const permissionState = await Notification.requestPermission();
    if (permissionState !== 'granted')
        return null;

    applicationServerPublicKey ??= await retrievePublicKey();
    const pushSubscription = await (await getServiceWorker()).pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: applicationServerPublicKey
    })
    await registerPushSubscription(pushSubscription)

    prevDeviceEndpoints.add(pushSubscription.endpoint);
    return pushSubscription.endpoint;
}

export async function unsubscribeFromPushNotifications() {
    const pushSubscription = await (await getServiceWorker()).pushManager.getSubscription();
    if (!pushSubscription)
        return;
    await pushSubscription.unsubscribe();
    await unregisterPushEndpoint(pushSubscription.endpoint)
}

// Used by LogOutForm.razor
window.beforeSignOut = async (event: SubmitEvent) => {
    event.preventDefault();

    await unsubscribeFromPushNotifications();
    document.cookie = 'No-Push=; Expires=Thu, 01 Jan 1970 00:00:00 UTC; SameSite=Lax; Path=/; Secure';

    (event.target as HTMLFormElement).submit();
};
