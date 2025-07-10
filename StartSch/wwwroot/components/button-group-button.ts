import {customElement} from "lit/decorators.js";
import {css} from "lit";
import {MdFilledButton} from "@material/web/all";

declare global {
    interface HTMLElementTagNameMap {
        'button-group-button': ButtonGroupButton;
    }
}

@customElement('button-group-button')
export class ButtonGroupButton extends MdFilledButton {
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
            }

            .background {
                transition: background-color 200ms ease-in-out;
            }

            .button {
                transition: color 200ms ease-in-out;
            }
        `
    ]
}
