// Based on https://github.com/tpeczek/Demo.AspNetCore.PushNotifications/blob/58f9c836651ce9d9f50d68f16cc55f9e312eb722/Demo.AspNetCore.PushNotifications/wwwroot/scripts/service-workers/push-service-worker.js

// firefox doesn't do modules in service workers because why would it and why would they document that
importScripts("./indexed-db-kv-store.js"); // needed by push-notifications-controller.js
importScripts("./push-notifications-controller.js");

/** @param {PushEvent} event */
async function handlePush(event) {
    const message = event.data.json();
    message.icon ??= "/android-chrome-192x192.png";
    await self.registration.showNotification(message.title, {
        body: message.body,
        icon: message.icon
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
                await unregisterPushEndpoint(event.oldSubscription.endpoint);

            // the firebase sdk says an empty newSubscription means unsubscription. we shall trust them
            // https://github.com/firebase/firebase-js-sdk/blob/1625f7a95cc3ffb666845db0a8044329be74b5be/packages/messaging/src/listeners/sw-listeners.ts#L61
            if (!event.newSubscription) return;

            await registerPushSubscription(event.newSubscription);
        })());
    });

self.addEventListener('notificationclick', event => event.notification.close());