import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement} from "lit";

@customElement('selectable-button')
export class SelectableButton extends LitElement {

    static styles = css`
        button-group-button {
            transition: border-radius 200ms ease-in-out;
            --md-filled-button-container-shape: 4px;
            
            --_container-color: var(--md-sys-color-primary-container);
            --_content-color: var(--md-sys-color-on-primary-container);

            --md-filled-button-label-text-color: var(--_content-color);
            --md-filled-button-hover-label-text-color: var(--_content-color);
            --md-filled-button-focus-label-text-color: var(--_content-color);
            --md-filled-button-active-label-text-color: var(--_content-color);
            --md-filled-button-pressed-label-text-color: var(--_content-color);
            --md-ripple-pressed-color: var(--_content-color);
            --md-ripple-hover-color: var(--_content-color);
        }

        :host([implicitly-selected]) button-group-button {
            --md-filled-button-container-shape: 16px;
        }
        
        :host([selected]) button-group-button {
            --md-filled-button-container-shape: 16px;
            
            --_container-color: var(--md-sys-color-primary);
            --_content-color: var(--md-sys-color-on-primary);
        }
    `;

    @property({type: Boolean, reflect: true, useDefault: true}) selected: boolean = false;
    @property({
        type: Boolean,
        reflect: true,
        useDefault: true,
        attribute: 'implicitly-selected'
    }) implicitlySelected: boolean = false;

    private toggle(e: Event) {
        this.selected = !this.selected;
    }

    protected render() {
        super.render();

        return html`
            <button-group-button @click="${this.toggle}">
                <slot name="icon" slot="icon"></slot>
                <slot></slot>
            </button-group-button>
        `;
    }
}
