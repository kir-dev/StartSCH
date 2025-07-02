import {customElement, property} from "lit/decorators.js";
import {html, LitElement} from "lit";

declare global {
    interface HTMLElementTagNameMap {
        'button-group': ButtonGroup;
    }
}

@customElement('button-group')
export class ButtonGroup extends LitElement {


    protected render() {
        super.render();

        return html`
            <div>
                <button-group-button selected implicitlyselected>
                    <md-icon>
                        send_to_mobile
                    </md-icon>
                </button-group-button>
            </div>
        `;
    }
}
