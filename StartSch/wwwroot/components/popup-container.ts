import {customElement} from "lit/decorators.js";
import {css, html, LitElement} from "lit";

declare global {
    interface HTMLElementTagNameMap {
        'popup-container': PopupContainer;
    }
}

@customElement('popup-container')
export class PopupContainer extends LitElement {
    static styles = css`
        div {
            padding: 8px;
            background-color: var(--md-sys-color-surface-container-high);
            border-radius: 8px;
            box-shadow: var(--md-sys-shadow-2);
        }
    `;

    protected render() {
        return html`
            <div>
                <slot></slot>
            </div>
        `;
    }
}
