// Based on https://github.com/tpeczek/Demo.AspNetCore.PushNotifications/blob/58f9c836651ce9d9f50d68f16cc55f9e312eb722/Demo.AspNetCore.PushNotifications/wwwroot/scripts/push-notifications-controller.js

function urlB64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }

    return outputArray;
}

async function discardPushSubscription(pushSubscription) {
    await fetch('/api/push-subscriptions/' + encodeURIComponent(pushSubscription.endpoint), {
        method: 'DELETE'
    });
}

async function storePushSubscription(pushSubscription) {
    return await fetch('/api/push-subscriptions', {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify(pushSubscription)
    });
}

async function retrievePublicKey() {
    let response = await fetch('/api/push-subscriptions/public-key');
    if (response.ok) {
        let applicationServerPublicKeyBase64 = await response.text();
        if (!applicationServerPublicKeyBase64)
            return null;
        return urlB64ToUint8Array(applicationServerPublicKeyBase64);
    } else {
        return Promise.reject(response.status + ' ' + response.statusText);
    }
}
