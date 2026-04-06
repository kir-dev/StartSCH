import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement} from "lit";

@customElement('connected-button-select-option')
export class ConnectedButtonSelectOption extends LitElement {
    static styles = css`
        :host {
            flex-grow: 1;
        }
    `;

    @property() value = "";
    @property({reflect: true, useDefault: true, type: Boolean}) selected = false;

    render() {
        return html`
            <expressive-button class="small">
                <slot></slot>
            </expressive-button>
        `;
    }
}
