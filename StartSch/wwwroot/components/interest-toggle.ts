import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement} from "lit";

declare global {
    interface HTMLElementTagNameMap {
        'interest-toggle': InterestToggle;
    }
}

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
        if (this.toggled)
            await fetch(`/api/interests/${this.interestId}/subscriptions`, {
                method: 'PUT',
            });
        else
            await fetch(`/api/interests/${this.interestId}/subscriptions`, {
                method: 'DELETE',
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
