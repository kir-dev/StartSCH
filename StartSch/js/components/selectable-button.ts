import {html, LitElement} from "lit";
import {customElement, property} from "lit/decorators.js";

@customElement('selectable-button')
export class SelectableButton extends LitElement {
    @property({type: Boolean, reflect: true, useDefault: true})
    selected: boolean = false;

    @property({
        type: Boolean,
        reflect: true,
        useDefault: true,
        attribute: 'implicitly-selected'
    })
    implicitlySelected: boolean = false;

    protected render() {
        const borderClass = this.selected
            ? "round"
            : "square";
        const colorClass = (this.selected || this.implicitlySelected)
            ? "filled"
            : "neutral";

        return html`
            <expressive-button class="extra-small ${borderClass} ${colorClass}">
                <slot></slot>
            </expressive-button>
        `;
    }
}
