import {css, html, LitElement} from "lit";
import {customElement} from "lit/decorators.js";
import {Button as MdButton} from "@material/web/button/internal/button";
import {styles as filledStyles} from '@material/web/button/internal/filled-styles';
import {styles as sharedElevationStyles} from '@material/web/button/internal/shared-elevation-styles';
import {styles as sharedStyles} from '@material/web/button/internal/shared-styles';
import {MdFilledButton} from "@material/web/button/filled-button";

// Measurements https://m3.material.io/components/buttons/specs#a69008b5-4efe-43ec-9d23-cbece83b08e6

@customElement('expressive-button')
export class ExpressiveButton extends MdButton {
    static styles = [
        ...MdFilledButton.styles,
        css`
            :host {
                min-width: 48px;
                transition: border-radius 200ms ease-in-out;
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
                border-radius: 12px;
            }

            :host(.extra-small.round) {
                border-radius: 16px;
            }

            :host(.extra-small:active) {
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
