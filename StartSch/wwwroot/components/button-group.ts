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
            --container-shape-end-start: var(--round);
            --container-shape-start-start: var(--round);
        }
        
        ::slotted(:last-child) {
            --container-shape-start-end: var(--round);
            --container-shape-end-end: var(--round);
        }
    `;
    
    protected render() {
        super.render();

        return html`
            <slot></slot>
        `;
    }
}
