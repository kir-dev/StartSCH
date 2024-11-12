// Based on https://github.com/tpeczek/Demo.AspNetCore.PushNotifications/blob/58f9c836651ce9d9f50d68f16cc55f9e312eb722/Demo.AspNetCore.PushNotifications/wwwroot/scripts/push-notifications.js

let applicationServerPublicKey;
let serviceWorkerCache;

function getServiceWorker() {
    return serviceWorkerCache ??= navigator.serviceWorker
        .register('/push-service-worker.js')
        // ensure the sw is always updated
        // https://github.com/firebase/firebase-js-sdk/blob/a9f844066045d8567ae143bae77d184ac227690d/packages/messaging/src/helpers/registerDefaultSw.ts#L34-L41
        .then(sw => sw.update())
}

async function getPushSubscriptionState() {
    const permission = Notification.permission;
    if (permission !== 'granted') return permission;
    return await (await getServiceWorker()).pushManager.getSubscription() ? 'subscribed' : 'not subscribed';
}

window.subscribeToPushNotifications = async () => {
    const permissionState = await Notification.requestPermission();
    if (permissionState !== 'granted') return await getPushSubscriptionState();

    applicationServerPublicKey ??= await retrievePublicKey();
    const pushSubscription = await (await getServiceWorker()).pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: applicationServerPublicKey
    })
    await storePushSubscription(pushSubscription)
    return 'subscribed';
};

window.unsubscribeFromPushNotifications = async () => {
    const pushSubscription = await (await getServiceWorker()).pushManager.getSubscription();
    if (!pushSubscription) return await getPushSubscriptionState();
    await pushSubscription.unsubscribe();
    await discardPushSubscription(pushSubscription)
    return await getPushSubscriptionState();
};

window.getPushSubscriptionState = getPushSubscriptionState;