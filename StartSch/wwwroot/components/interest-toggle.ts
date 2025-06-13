import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement} from "lit";
import {InterestIndex} from "../interest-index";

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
    
    @property({type: Boolean, reflect: true, useDefault: true}) selected: boolean = false;
    @property({type: Number}) interestId: number = 0;
    @property() icon: string = "";
    
    async handleToggled() {
        this.selected = !this.selected;
        if (this.selected) {
            await fetch(`/api/interests/${this.interestId}/subscriptions`, {
                method: 'PUT',
            });
            InterestIndex.subscriptions.add(this.interestId);
        }
        else {
            await fetch(`/api/interests/${this.interestId}/subscriptions`, {
                method: 'DELETE',
            });
            InterestIndex.subscriptions.delete(this.interestId);
        }
    }
    
    protected render() {
        super.render();
        return html`
            <md-outlined-icon-button toggle @click="${this.handleToggled}" ?selected="${this.selected}">
                <md-icon>
                    ${this.icon}
                </md-icon>
            </md-outlined-icon-button>
        `;
    }
}
