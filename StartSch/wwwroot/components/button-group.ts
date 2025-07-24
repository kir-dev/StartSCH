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
            --container-shape-end-start: 14px;
            --container-shape-start-start: 14px;
        }
        
        ::slotted(:last-child) {
            --container-shape-start-end: 14px;
            --container-shape-end-end: 14px;
        }
    `;
    
    protected render() {
        super.render();

        return html`
            <slot></slot>
        `;
    }
}
