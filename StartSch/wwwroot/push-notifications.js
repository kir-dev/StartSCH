// Based on https://github.com/tpeczek/Demo.AspNetCore.PushNotifications/blob/58f9c836651ce9d9f50d68f16cc55f9e312eb722/Demo.AspNetCore.PushNotifications/wwwroot/scripts/push-notifications.js

let applicationServerPublicKey;
let serviceWorkerCache;

/** Keeps track of all push endpoints exposed by this browser since the page loaded.
 *  @type {Set<string>} */
const prevDeviceEndpoints = new Set();

function getServiceWorker() {
    return serviceWorkerCache ??= navigator.serviceWorker
        .register('/push-service-worker.js')
        // ensure the sw is always updated
        // https://github.com/firebase/firebase-js-sdk/blob/a9f844066045d8567ae143bae77d184ac227690d/packages/messaging/src/helpers/registerDefaultSw.ts#L34-L41
        .then(sw => sw.update())
}

/** @returns {PushSubscription | null} */
async function getPushSubscription() {
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

/** @returns {Promise<{prevDeviceEndpoints: [string], currentEndpoint: string | undefined, permissionState: string}>} */
window.getPushSubscriptionState = async function() {
    await checkSubscriptionRegistration();

    const permission = Notification.permission;
    const subscription = await getPushSubscription();
    return {
        permissionState: permission,
        currentEndpoint: subscription?.endpoint,
        prevDeviceEndpoints: [...prevDeviceEndpoints],
    };
};

/** @returns {Promise<string | null>} */
window.subscribeToPushNotifications = async () => {
    document.cookie = 'No-Push=; Expires=Thu, 01 Jan 1970 00:00:00 UTC; SameSite=Lax; Path=/';

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
};

window.unsubscribeFromPushNotifications = async () => {
    const pushSubscription = await (await getServiceWorker()).pushManager.getSubscription();
    if (!pushSubscription) return await getPushSubscriptionState();
    await pushSubscription.unsubscribe();
    await unregisterPushEndpoint(pushSubscription.endpoint)
};
