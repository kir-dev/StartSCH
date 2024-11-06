// Based on https://github.com/tpeczek/Demo.AspNetCore.PushNotifications/blob/58f9c836651ce9d9f50d68f16cc55f9e312eb722/Demo.AspNetCore.PushNotifications/wwwroot/scripts/push-notifications.js

import * as PushNotificationsController from "/push-notifications-controller.js";

let applicationServerPublicKey;
let pushServiceWorkerRegistration = await navigator.serviceWorker.register(
    '/push-service-worker.js',
    {type: "module"});

const getPushSubscriptionState = async () => {
    const permission =  Notification.permission;
    if (permission !== 'granted') return permission;
    return await pushServiceWorkerRegistration.pushManager.getSubscription() ? 'subscribed' : 'not subscribed';
};

window.subscribeToPushNotifications = async () => {
    const permissionState =  await Notification.requestPermission();
    if (permissionState !== 'granted') return await getPushSubscriptionState();

    applicationServerPublicKey ??= await PushNotificationsController.retrievePublicKey();
    const pushSubscription = await pushServiceWorkerRegistration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: applicationServerPublicKey
    })
    await PushNotificationsController.storePushSubscription(pushSubscription)
    return 'subscribed';
};

window.unsubscribeFromPushNotifications = async () => {
    const pushSubscription = await pushServiceWorkerRegistration.pushManager.getSubscription();
    if (!pushSubscription) return await getPushSubscriptionState();
    await pushSubscription.unsubscribe();
    await PushNotificationsController.discardPushSubscription(pushSubscription)
    return await getPushSubscriptionState();
};

window.getPushSubscriptionState = getPushSubscriptionState;