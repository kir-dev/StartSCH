﻿/// <reference lib="webworker" />

import {registerPushSubscription, unregisterPushEndpoint} from "./push-notifications-controller";

// https://www.devextent.com/create-service-worker-typescript/
declare const self: ServiceWorkerGlobalScope;

// Based on https://github.com/tpeczek/Demo.AspNetCore.PushNotifications/blob/58f9c836651ce9d9f50d68f16cc55f9e312eb722/Demo.AspNetCore.PushNotifications/wwwroot/scripts/service-workers/push-service-worker.js

// service workers by default only update after every tab has been closed so as not to mess with caching,
// we can disable that as we don't use the service worker for caching
self.addEventListener("install", event => event.waitUntil(self.skipWaiting()));
self.addEventListener("activate", event => event.waitUntil(self.clients.claim()));

async function handlePush(event: PushEvent) {
    const message = event.data!.json();
    message.icon ??= "/android-chrome-192x192.png";
    await self.registration.showNotification(message.title, {
        body: message.body,
        icon: message.icon,
        data: {url: message.url},
    });
}

self.addEventListener('push', event => event.waitUntil(handlePush(event)));

// https://w3c.github.io/push-api/#pushsubscriptionchangeevent-interface
interface PushSubscriptionChangeEvent extends ExtendableEvent {
    readonly newSubscription?: PushSubscription;
    readonly oldSubscription?: PushSubscription;
}

async function handlePushSubscriptionChange(event: PushSubscriptionChangeEvent) {
    if (event.oldSubscription)
        await unregisterPushEndpoint(event.oldSubscription.endpoint);

    // the firebase sdk says an empty newSubscription means unsubscription. we shall trust them
    // https://github.com/firebase/firebase-js-sdk/blob/1625f7a95cc3ffb666845db0a8044329be74b5be/packages/messaging/src/listeners/sw-listeners.ts#L61
    if (!event.newSubscription) return;

    await registerPushSubscription(event.newSubscription);
}

self.addEventListener('pushsubscriptionchange',
    // @ts-ignore https://github.com/microsoft/TypeScript/issues/44729
    (event: PushSubscriptionChangeEvent) => event.waitUntil(handlePushSubscriptionChange(event))
);

// doesn't work in firefox android https://bugzilla.mozilla.org/show_bug.cgi?id=1825910
async function handleNotificationClick(event: NotificationEvent) {
    event.notification.close();
    const urlToOpen = event.notification.data.url;
    await self.clients.openWindow(urlToOpen);
}

self.addEventListener("notificationclick", event => event.waitUntil(handleNotificationClick(event)));