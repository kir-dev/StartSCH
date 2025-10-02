import {css} from "lit";
import {customElement} from "lit/decorators.js";
import {Button as MdButton} from "@material/web/button/internal/button";
import {MdFilledButton} from "@material/web/button/filled-button";

declare global {
    interface HTMLElementTagNameMap {
        'expressive-button': ExpressiveButton;
    }
}

// Measurements https://m3.material.io/components/buttons/specs#a69008b5-4efe-43ec-9d23-cbece83b08e6

@customElement('expressive-button')
export class ExpressiveButton extends MdButton {
    static styles = [
        ...MdFilledButton.styles,
        css`
            :host {
                min-width: 48px;
                transition: border-radius var(--md-sys-motion-spring-fast-spatial-easing);
            }

            .background {
                transition: background-color 200ms ease-in-out;
            }

            .button {
                transition: color 200ms ease-in-out;
            }

            .button {
                min-width: calc(48px - var(--_leading-space) - var(--_trailing-space));
            }

            .touch {
                height: 32px;
            }
            
            .label {
                display: flex;
            }
            
            :host(.extra-small) {
                --_height: 32;
                --_leading-space: 10px;
                --_trailing-space: 10px;
                --md-icon-size: 20px;
                --_icon-size: 20px;
                
                min-width: 48px;
                --_container-height: 32px;
            }

            :host(.extra-small.square) {
                /* intentionally different from the Material spec, so that the difference is more visible between
                 selected/unselected states */
                border-radius: 10px;
            }

            :host(.extra-small.round) {
                border-radius: 16px;
            }

            :host(.extra-small:active) {
                /* intentionally different from the Material spec */
                border-radius: 6px;
            }

            :host(.small) {
                --_height: 40;
                --_leading-space: 16px;
                --_trailing-space: 16px;
                --md-icon-size: 20px;
                --_icon-size: 20px;

                min-width: 48px;
                --_container-height: 40px;
            }

            :host(.small.square) {
                /* intentionally different from the Material spec, so that the difference is more visible between
                 selected/unselected states */
                border-radius: 12px;
            }

            :host(.small.round) {
                border-radius: 20px;
            }

            :host(.small:active) {
                border-radius: 8px;
            }
            
            :host(.filled) {
                // default for MdFilledButton
            }

            :host(.tonal) {
                --md-sys-color-primary: var(--md-sys-color-primary-container);
                --md-sys-color-on-primary: var(--md-sys-color-on-primary-container);
            }
            
            :host(.neutral) {
                --md-sys-color-primary: var(--md-sys-color-surface-container-highest);
                --md-sys-color-on-primary: var(--md-sys-color-on-surface);
            }

            :host(.thin) {
                font-weight: 400;
            }

            :host(.bold) {
                font-weight: 800;
            }
        `,
    ];
}
