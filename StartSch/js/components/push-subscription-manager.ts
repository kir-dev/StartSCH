import {css, html, LitElement} from "lit";
import {customElement, property, state} from "lit/decorators.js";
import {
    getPushSubscriptionState,
    PushSubscriptionState,
    subscribeToPushNotifications,
    unsubscribeFromPushNotifications
} from "../push-notifications";
import {computeSha256} from "../utils";

@customElement('push-subscription-manager')
export class PushSubscriptionManager extends LitElement {
    static styles = css`
        .loading-indicator {
            height: 4px;
            width: 100%;
            background: linear-gradient(90deg, #0000 33%, var(--md-sys-color-primary) 50%, #0000 66%);
            background-size: 300% 100%;
            animation: l1 1s infinite linear;
        }

        @keyframes l1 {
            0% {
                background-position: right
            }
        }
    `;

    constructor() {
        super();
        void this.refreshSubscriptionState();
    }

    @property({attribute: 'registered-endpoint-hashes', type: Array}) registeredEndpointHashes: string[] = [];

    @state() private pushSubscriptionState?: PushSubscriptionState;
    @state() private hashedCurrentEndpoint?: string;
    @state() private hashedPrevDeviceEndpoints: string[] = [];
    @state() private busy: boolean = false;

    private async refreshSubscriptionState() {
        const state = await getPushSubscriptionState();
        this.pushSubscriptionState = state;
        this.hashedPrevDeviceEndpoints = await Promise.all(state.prevDeviceEndpoints.map(e => computeSha256(e)));
        this.hashedCurrentEndpoint = state.currentEndpoint ? await computeSha256(state.currentEndpoint) : undefined;
    }

    private async subscribe() {
        this.busy = true;
        try {
            const endpoint = await subscribeToPushNotifications();
            if (endpoint) {
                const hashed = await computeSha256(endpoint);
                if (!this.registeredEndpointHashes.includes(hashed))
                    this.registeredEndpointHashes = [...this.registeredEndpointHashes, hashed];
            }
        } finally {
            await this.refreshSubscriptionState();
            this.busy = false;
        }
    }

    private async unsubscribe() {
        this.busy = true;
        try {
            await unsubscribeFromPushNotifications();
        } finally {
            await this.refreshSubscriptionState();
            this.busy = false;
        }
    }

    protected render() {
        const state = this.pushSubscriptionState;
        const loading = this.busy || state?.permissionState === undefined;
        return html`
            <div style="display: flex; min-width: 300px; max-width: 500px">
                <section style="background-color: var(--md-sys-color-surface-container-high);
                    padding: 8px 16px; flex: 1;
                    border-radius: 16px">
                    <h2 style="font-size: 20px">Push értesítések</h2>
                    Állapot:
                    ${loading
                        ? html`
                            ...
                            <div style="height: 48px; display: flex; align-items: end">
                                <div class="loading-indicator"></div>
                            </div>
                        `
                        : this.renderContent()
                    }
                </section>
            </div>
        `;
    }

    private renderContent() {
        const state = this.pushSubscriptionState!;
        const otherCount = this.registeredEndpointHashes
            .filter(x => !this.hashedPrevDeviceEndpoints.includes(x))
            .length;

        const isActiveOnThisDevice = this.hashedCurrentEndpoint !== undefined
            && this.registeredEndpointHashes.includes(this.hashedCurrentEndpoint);

        if (isActiveOnThisDevice) {
            return html`
                ${otherCount > 0
                    ? html`aktív ezen és ${otherCount} másik eszközön`
                    : html`aktív ezen az eszközön`
                }
                <div style="display: flex; justify-content: end; margin-top: 8px">
                    <md-text-button @click=${this.unsubscribe}>
                        Leiratkozás ezen az eszközön
                    </md-text-button>
                </div>
            `;
        }

        return html`
            ${otherCount > 0
                ? html`aktív ${otherCount} másik eszközön`
                : html`
                    <span style="color: var(--md-sys-color-error); font-weight: bold">
                        inaktív
                    </span>
                `
            }
            ${state.permissionState === 'denied'
                ? html`
                    <div style="margin-top: 8px; font-size: 14px; color: var(--md-sys-color-on-surface-variant)">
                        Letiltottad az oldal számára az értesítések küldését. Ha szeretnél értesítéseket kapni ezen az
                        eszközön, engedélyezd az értesítéseket a böngésződ beállításiban, majd frissítsd az oldalt.
                    </div>
                `
                : null
            }
            <div style="display: flex; justify-content: end; margin-top: 8px">
                <md-filled-button @click=${this.subscribe} .disabled=${state.permissionState === 'denied'}>
                    Beállítás ezen az eszközön ${otherCount > 0 ? 'is' : ''}
                </md-filled-button>
            </div>
        `;
    }
}
