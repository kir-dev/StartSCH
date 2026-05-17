import {customElement, property, queryAssignedElements} from "lit/decorators.js";
import {css, html, LitElement} from "lit";
import {ConnectedButtonSelectOption} from "./connected-button-select-option";

@customElement('connected-button-select')
export class ConnectedButtonSelect extends LitElement {
    static styles = css`
        :host {
            display: grid;
            grid-auto-flow: column;
            grid-auto-columns: 1fr;
            overflow-x: auto;
            gap: 2px;
        }
        
        ::slotted(*) {
            --shape: 8px;
        }
        
        ::slotted(:first-child) {
            --shape-start-start: 20px;
            --shape-start-end: 8px;
            --shape-end-end: 8px;
            --shape-end-start: 20px;
        }
        
        ::slotted(:last-child) {
            --shape-start-start: 8px;
            --shape-start-end: 20px;
            --shape-end-end: 20px;
            --shape-end-start: 8px;
        }

        ::slotted(:first-child:last-child) {
            --shape-start-start: 20px;
            --shape-start-end: 20px;
            --shape-end-end: 20px;
            --shape-end-start: 20px;
        }
        
        ::slotted(:not([selected])) {
            --md-sys-color-primary: var(--md-sys-color-primary-container);
            --md-sys-color-on-primary: var(--md-sys-color-on-primary-container);
        }

        ::slotted([selected]) {
            --shape-start-start: 20px;
            --shape-start-end: 20px;
            --shape-end-end: 20px;
            --shape-end-start: 20px;
        }
        
        ::slotted(:active) {
            --shape-start-start: 8px;
            --shape-start-end: 8px;
            --shape-end-end: 8px;
            --shape-end-start: 8px;
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
        if (!this.value) return;
        this._optionElements.forEach(opt => opt.selected = (opt.value === this.value));
    }
    
    itemClick(e: Event) {
        const opt = e.target as ConnectedButtonSelectOption;
        this.value = opt.value;
        const event = new Event("change", {composed: true, bubbles: true});
        this.dispatchEvent(event);
    }
}
