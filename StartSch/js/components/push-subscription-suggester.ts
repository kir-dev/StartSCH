import {SignalWatcher} from "@lit-labs/signals";
import {css, html, LitElement, nothing} from "lit";
import * as PushSubscriptions from "../push-subscriptions";
import {customElement} from "lit/decorators.js";

declare global {
    interface HTMLElementTagNameMap {
        'push-subscription-suggester': PushSubscriptionSuggester;
    }
}

@customElement('push-subscription-suggester')
export class PushSubscriptionSuggester extends SignalWatcher(LitElement) {
    static styles = css`
        .card {
            margin: 16px 0;
            border-radius: 16px;
            border: solid 1px var(--md-sys-color-outline-variant);
            padding: 12px;
            font-size: 14px;
            color: var(--md-sys-color-on-surface-variant);
        }
        
        .buttons {
            display: flex;
            gap: 8px;
            margin-top: 12px;
        }
    `;
    
    protected render() {
        if (!PushSubscriptions.suggestSubscribing.get())
            return;

        if (PushSubscriptions.permissionState.get() === "denied")
            return html`
                <div class="card">
                    Letiltottad az oldal számára az értesítések küldését. Ha szeretnél értesítéseket kapni ezen
                    az eszközön, engedélyezd ezt a böngésződ beállításiban.
                    <div class="buttons">
                        <expressive-button
                            class="extra-small text round"
                            @click="${() => PushSubscriptions.noPushOnThisDevice.set(true)}">
                            Inkább másik eszközön
                        </expressive-button>
                    </div>
                </div>
            `;

        return html`
            <div class="card">
                Szeretnél értesítéseket kapni ezen az eszközön?
                <div class="buttons">
                    <expressive-button
                        class="extra-small filled round"
                        @click="${PushSubscriptions.registerDevice}">
                        Feliratkozás
                    </expressive-button>
                    <expressive-button
                        class="extra-small tonal round"
                        @click="${() => PushSubscriptions.noPushOnThisDevice.set(true)}">
                        Inkább másik eszközön
                    </expressive-button>
                </div>
            </div>
        `;
    }
}
