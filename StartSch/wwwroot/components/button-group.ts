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
        }
    `;
    
    protected render() {
        super.render();

        return html`
            <selectable-button selected>
                <md-icon>
                    send_to_mobile
                </md-icon>
            </selectable-button>
            <selectable-button implicitly-selected>
                <md-icon>
                    send_to_mobile
                </md-icon>
            </selectable-button>
            <selectable-button>
                <md-icon>
                    send_to_mobile
                </md-icon>
            </selectable-button>
        `;
    }
}
