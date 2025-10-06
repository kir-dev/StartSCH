// Based on https://github.com/tpeczek/Demo.AspNetCore.PushNotifications/blob/58f9c836651ce9d9f50d68f16cc55f9e312eb722/Demo.AspNetCore.PushNotifications/wwwroot/scripts/push-notifications-controller.js

import * as KvStore from "./indexed-db-kv-store";

function urlB64ToUint8Array(base64String: string) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }

    return outputArray;
}

export async function unregisterPushEndpoint(endpoint: string) {
    await fetch('/api/push-subscriptions/' + encodeURIComponent(endpoint), {
        method: 'DELETE'
    });

    await KvStore.remove("pushEndpoint")
}

export async function registerPushSubscription(pushSubscription: PushSubscription) {
    const oldEndpoint = await KvStore.get("pushEndpoint");
    if (oldEndpoint)
        await unregisterPushEndpoint(oldEndpoint);

    await fetch('/api/push-subscriptions', {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(pushSubscription)
    });

    await KvStore.set("pushEndpoint", pushSubscription.endpoint);
}

export async function retrievePublicKey(): Promise<BufferSource> {
    let response = await fetch('/api/push-subscriptions/public-key');
    if (response.ok) {
        let applicationServerPublicKeyBase64 = await response.text();
        return urlB64ToUint8Array(applicationServerPublicKeyBase64);
    } else {
        return Promise.reject(response.status + ' ' + response.statusText);
    }
}
