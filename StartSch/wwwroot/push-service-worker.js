// Based on https://github.com/tpeczek/Demo.AspNetCore.PushNotifications/blob/58f9c836651ce9d9f50d68f16cc55f9e312eb722/Demo.AspNetCore.PushNotifications/wwwroot/scripts/service-workers/push-service-worker.js

import * as PushNotificationsController from "/push-notifications-controller.js";

/** @param {PushEvent} event */
async function handlePush(event) {
    await self.registration.showNotification('Demo.AspNetCore.PushNotifications', {
        body: event.data.text(),
        icon: '/images/push-notification-icon.png'
    });
}

self.addEventListener(
    'push',
    /** @param {PushEvent} event*/event => event.waitUntil(handlePush(event)));

/** @param {{oldSubscription?: PushSubscription, newSubscription?: PushSubscription}} event */
self.addEventListener(
    'pushsubscriptionchange',
    event => {
        event.waitUntil((async () => {
            if (event.oldSubscription)
                await PushNotificationsController.discardPushSubscription(event.oldSubscription);

            // the firebase sdk says an empty newSubscription means unsubscription. we shall trust them
            // https://github.com/firebase/firebase-js-sdk/blob/1625f7a95cc3ffb666845db0a8044329be74b5be/packages/messaging/src/listeners/sw-listeners.ts#L61
            if (!event.newSubscription) return;

            await PushNotificationsController.storePushSubscription(event.newSubscription);
        })());
    });

self.addEventListener('notificationclick', event => event.notification.close());