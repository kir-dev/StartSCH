import {html, LitElement} from "lit";
import {customElement} from "lit/decorators.js";
import * as PushSubscriptions from "../push-subscriptions";
import {SignalWatcher} from "@lit-labs/signals";

@customElement('push-subscription-preferences')
export class PushSubscriptionPreferences extends SignalWatcher(LitElement) {
    protected render() {
        if (PushSubscriptions.registeredEndpointHashes.size === 0 && !PushSubscriptions.pushInterestsFollowed.get())
            return;

        return html`
            <div style="display: flex; min-width: 300px; max-width: 500px">
                <section style="background-color: var(--md-sys-color-surface-container-high);
                    padding: 8px 16px; flex: 1;
                    border-radius: 16px">
                    <h2 style="font-size: 20px">Push értesítések fogadása</h2>
                    Állapot:
                    ${PushSubscriptions.isBusy.get()
                        ? html`...`
                        : this.renderContent()
                    }
                </section>
            </div>
        `;
    }

    private renderContent() {
        const otherCount = PushSubscriptions.countOfSubscriptionsOnOtherDevices.get();
        const isActiveOnThisDevice = PushSubscriptions.isThisDeviceRegistered.get();
        const denied = PushSubscriptions.permissionState.get() === 'denied';

        if (isActiveOnThisDevice) {
            return html`
                ${otherCount > 0
                    ? html`feliratkozva ezen és ${otherCount} másik eszközön`
                    : html`feliratkozva ezen az eszközön`
                }
                <div style="display: flex; justify-content: end; margin-top: 8px">
                    <expressive-button
                        class="small text round standard"
                        @click=${PushSubscriptions.unregisterDevice}>
                            Leiratkozás${otherCount > 0 ? ' ezen az eszközön' : ''}
                    </expressive-button>
                </div>
            `;
        }

        return html`
            ${otherCount > 0
                ? html`feliratkozva ${otherCount} másik eszközön`
                : html`
                    <span style="color: var(--md-sys-color-error); font-weight: bold">
                        nincs feliratkozott eszköz
                    </span>
                `
            }
            ${denied
                ? html`
                    <div style="margin-top: 8px; font-size: 14px; color: var(--md-sys-color-on-surface-variant)">
                        Letiltottad az értesítések küldését.<br>
                        Ha szeretnél értesítéseket kapni ezen az eszközön, engedélyezd az oldal számára
                        az értesítések küldését a böngésződ beállításiban.
                    </div>
                `
                : null
            }
            <div style="display: flex; justify-content: end; margin-top: 8px">
                <expressive-button
                    class="small ${otherCount > 0 ? 'tonal standard' : 'filled'} round"
                    @click=${PushSubscriptions.registerDevice}
                    .disabled=${denied}>
                        Feliratkozás${otherCount > 0 ? ' ezen az eszközön is' : ''}
                </expressive-button>
            </div>
        `;
    }
}
