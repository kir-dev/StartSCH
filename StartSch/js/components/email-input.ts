import {html, LitElement} from "lit";
import {customElement, property} from "lit/decorators.js";

@customElement('email-input')
export class EmailInput extends LitElement {
    @property({attribute: "disabled-value"})
    disabledValue: string = "";
    
    @property()
    value: string = "";
    
    @property()
    name: string = "";

    protected render() {
        return html`
            <div style="display: flex; flex-direction: column; gap: 8px; padding: 8px 0">
                <md-outlined-text-field
                    type="email"
                    name="${this.name}"
                    value="${this.value}"
                    @input="${(e: Event) => this.value = (e.target as HTMLInputElement).value}">
                </md-outlined-text-field>
                <expressive-button
                    type="submit"
                    class="small round filled standard"
                    ?soft-disabled="${this.value === this.disabledValue}">
                    Mentés
                </expressive-button>
            </div>
        `;
    }

    protected createRenderRoot() {
        return this;
    }
}
