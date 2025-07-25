import {customElement, property} from "lit/decorators.js";
import {css} from "lit";
import {MdFilledButton} from "@material/web/all";

declare global {
    interface HTMLElementTagNameMap {
        'toggle-button': ToggleButton;
    }
}

@customElement('toggle-button')
export class ToggleButton extends MdFilledButton {
    static styles = [
        ...MdFilledButton.styles,
        css`
            :host {
                min-width: 48px;
                --_container-height: 32px;
                --md-icon-size: 20px;
                --_icon-size: 20px;
                --_label-text-line-height: 8px;
                --_leading-space: 0;
                --_trailing-space: 0;
                
                transition: border-radius 200ms ease-in-out;

                --_container-shape-start-start: var(--container-shape-start-start, 4px);
                --_container-shape-start-end: var(--container-shape-start-end, 4px);
                --_container-shape-end-start: var(--container-shape-end-start, 4px);
                --_container-shape-end-end: var(--container-shape-end-end, 4px);
            }

            .background {
                transition: background-color 200ms ease-in-out;
            }

            .button {
                transition: color 200ms ease-in-out;
            }
            
            :host(:not([selected])) {
                --md-sys-color-primary: var(--md-sys-color-primary-container);
                --md-sys-color-on-primary: var(--md-sys-color-on-primary-container);
            }
            
            :host([implicitly-selected]) {
                --_container-shape-start-start: var(--round);
                --_container-shape-start-end: var(--round);
                --_container-shape-end-start: var(--round);
                --_container-shape-end-end: var(--round);
            }

            :host([selected]) {
                --_container-shape-start-start: var(--round);
                --_container-shape-start-end: var(--round);
                --_container-shape-end-start: var(--round);
                --_container-shape-end-end: var(--round);
            }
        `
    ]
    
    @property({type: Boolean, reflect: true, useDefault: true})
    selected: boolean = false;

    @property({
        type: Boolean,
        reflect: true,
        useDefault: true,
        attribute: 'implicitly-selected'
    })
    implicitlySelected: boolean = false;
}
