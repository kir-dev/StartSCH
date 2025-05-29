import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement} from "lit";

@customElement('interest-toggle')
export class InterestToggle extends LitElement {
    static styles = css`
        md-icon-button {
            --md-icon-button-icon-color: var(--md-sys-color-on-surface);
        }
    `;
    
    @property({type: Boolean, reflect: true, useDefault: true}) toggled: boolean = false;
    @property({type: Number}) interestId: number = 0;
    @property() icon: string = "";
    
    async handleToggled() {
        this.toggled = !this.toggled;
        const headers = {
            RequestVerificationToken: document.cookie.split("; ")
                .find((row) => row.startsWith("XSRF-TOKEN="))
                ?.split("=")[1] ?? ""
        }
        if (this.toggled)
            await fetch(`/api/interests/${this.interestId}/subscriptions`, {
                method: 'PUT',
                headers,
            });
        else
            await fetch(`/api/interests/${this.interestId}/subscriptions`, {
                method: 'DELETE',
                headers,
            });
    }
    
    protected render() {
        super.render();
        return html`
            <md-outlined-icon-button toggle @click="${this.handleToggled}">
                <md-icon>
                    ${this.icon}
                </md-icon>
            </md-outlined-icon-button>
        `;
    }
}
