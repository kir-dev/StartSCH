import {customElement, property, queryAssignedElements} from "lit/decorators.js";
import {css, html, LitElement} from "lit";
import {ConnectedButtonSelectOption} from "./connected-buton-select-option";

@customElement('connected-button-select')
export class ConnectedButtonSelect extends LitElement {
    static styles = css`
        :host {
            display: flex;
            justify-content: stretch;
        }
        
        ::slotted(:not([selected])) {
            --md-sys-color-primary: var(--md-sys-color-primary-container);
            --md-sys-color-on-primary: var(--md-sys-color-on-primary-container);
        }
    `;
    
    @property() value = "";
    
    // https://lit.dev/docs/components/shadow-dom/#query-assigned-nodes
    @queryAssignedElements() _optionElements!: Array<ConnectedButtonSelectOption>;
    
    render() {
        return html`
            <slot @click="${this.itemClick}"></slot>
        `;
    }
    
    updated() {
        this._optionElements.forEach(opt => opt.selected = (opt.value === this.value));
    }
    
    itemClick(e: Event) {
        const opt = e.target as ConnectedButtonSelectOption;
        this.value = opt.value;
        const event = new Event("change", {composed: true, bubbles: true});
        this.dispatchEvent(event);
    }
}
