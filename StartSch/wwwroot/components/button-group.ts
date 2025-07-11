import {customElement} from "lit/decorators.js";
import {css, html, LitElement} from "lit";

declare global {
    interface HTMLElementTagNameMap {
        'button-group': ButtonGroup;
    }
}

@customElement('button-group')
export class ButtonGroup extends LitElement {
    static styles = css`
        :host {
            display: flex;
            gap: 2px;
            align-items: center;
        }
        
        ::slotted(:first-child) {
            --unselected-button-shape-start-start: 16px;
            --unselected-button-shape-start-end: 4px;
            --unselected-button-shape-end-start: 16px;
            --unselected-button-shape-end-end: 4px;
        }
        
        ::slotted(*) {
            --unselected-button-shape: 4px;
            --selected-button-shape: 16px;
        }
        
        ::slotted(:last-child) {
            --unselected-button-shape-start-start: 4px;
            --unselected-button-shape-start-end: 16px;
            --unselected-button-shape-end-start: 4px;
            --unselected-button-shape-end-end: 16px;
        }
    `;
    
    protected render() {
        super.render();

        return html`
            <slot></slot>
        `;
    }
}
