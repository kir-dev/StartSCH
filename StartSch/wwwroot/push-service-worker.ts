/// <reference lib="webworker" />

import {registerPushSubscription, unregisterPushEndpoint} from "./push-notifications-controller";

// https://www.devextent.com/create-service-worker-typescript/
declare const self: ServiceWorkerGlobalScope;

// Based on https://github.com/tpeczek/Demo.AspNetCore.PushNotifications/blob/58f9c836651ce9d9f50d68f16cc55f9e312eb722/Demo.AspNetCore.PushNotifications/wwwroot/scripts/service-workers/push-service-worker.js

async function handlePush(event: PushEvent) {
    const message = event.data!.json();
    message.icon ??= "/android-chrome-192x192.png";
    await self.registration.showNotification(message.title, {
        body: message.body,
        icon: message.icon
    });
}

self.addEventListener(
    'push',
    (event: PushEvent) => event.waitUntil(handlePush(event)));

// https://w3c.github.io/push-api/#pushsubscriptionchangeevent-interface
interface PushSubscriptionChangeEvent extends ExtendableEvent {
    readonly newSubscription?: PushSubscription;
    readonly oldSubscription?: PushSubscription;
}

self.addEventListener(
    'pushsubscriptionchange',
    // @ts-ignore
    (event: PushSubscriptionChangeEvent) => {
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