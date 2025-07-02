import {customElement, property} from "lit/decorators.js";
import {html, LitElement} from "lit";

declare global {
    interface HTMLElementTagNameMap {
        'button-group-button': ButtonGroupButton;
    }
}

@customElement('button-group-button')
export class ButtonGroupButton extends LitElement {
    @property({type: Boolean}) selected: boolean = false;
    @property({type: Boolean}) implicitlySelected: boolean = false;
    
    protected render() {
        super.render();
        
        return html`
            <div>
                ${this.selected}
            </div>
            <div>
                ${this.implicitlySelected}
            </div>
            <div>
                <slot></slot>
            </div>
        `;
    }
}
