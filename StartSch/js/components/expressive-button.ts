import {css} from "lit";
import {customElement, property} from "lit/decorators.js";
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

                --_container-shape-start-start: var(--shape-start-start, var(--shape, 0));
                --_container-shape-start-end: var(--shape-start-end, var(--shape, 0));
                --_container-shape-end-start: var(--shape-end-start, var(--shape, 0));
                --_container-shape-end-end: var(--shape-end-end, var(--shape, 0));
            }

            /* As the :host is rendered before Lit has initialized, it receives a border-radius of 0, which then
               would animate to the actual radius. Disable the transition until the browser has finished the first
               render after Lit has loaded. */
            :host(:not([no-animate-border-radius])) {
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
                height: var(--_container-height);
            }

            .label {
                display: flex;
            }

            :host(.extra-small) {
                --_container-height: 32px;
                --_leading-space: 10px;
                --_trailing-space: 10px;
                --_with-leading-icon-leading-space: 10px;
                --_with-leading-icon-trailing-space: 10px;
                --_with-trailing-icon-leading-space: 10px;
                --_with-trailing-icon-trailing-space: 10px;
                --md-icon-size: 20px;
                --_icon-size: 20px;
                gap: 4px;
            }

            :host(.extra-small.square) {
                /* intentionally different from the Material spec, so that the difference is more visible between
                 selected/unselected states */
                --shape: 10px;
            }

            :host(.extra-small.round) {
                --shape: 16px;
            }

            :host(:not(.standard).extra-small:active) {
                /* intentionally different from the Material spec */
                --shape: 6px;
            }

            :host(.small) {
                --_container-height: 40px;
                --_leading-space: 16px;
                --_trailing-space: 16px;
                --_with-leading-icon-leading-space: 16px;
                --_with-leading-icon-trailing-space: 16px;
                --_with-trailing-icon-leading-space: 16px;
                --_with-trailing-icon-trailing-space: 16px;
                --md-icon-size: 20px;
                --_icon-size: 20px;
                gap: 8px;
            }

            :host(.small.square) {
                /* intentionally different from the Material spec, so that the difference is more visible between
                 selected/unselected states */
                --shape: 12px;
            }

            :host(.small.round) {
                --shape: 20px;
            }

            :host(:not(.standard).small:active) {
                --shape: 8px;
            }
            
            :host(.medium) {
                --_container-height: 56px;
                --_leading-space: 24px;
                --_trailing-space: 24px;
                --_with-leading-icon-leading-space: 24px;
                --_with-leading-icon-trailing-space: 24px;
                --_with-trailing-icon-leading-space: 24px;
                --_with-trailing-icon-trailing-space: 24px;
                --md-icon-size: 24px;
                --_icon-size: 24px;
                gap: 8px;
            }
            
            :host(.medium.square) {
                --shape: 16px;
            }
            
            :host(.medium.round) {
                --shape: 28px;
            }
            
            :host(:not(.standard).medium:active) {
                --shape: 12px;
            }

            :host(.filled) {
                // default for MdFilledButton
            }

            :host(.tonal) {
                --md-sys-color-primary: var(--md-sys-color-primary-container);
                --md-sys-color-on-primary: var(--md-sys-color-on-primary-container);
            }

            :host(.text) {
                // from @material/web/button/internal/text-styles.css
                --_disabled-label-text-color: var(--md-sys-color-on-surface);
                --_focus-label-text-color: var(--md-sys-color-primary);
                --_hover-label-text-color: var(--md-sys-color-primary);
                --_hover-state-layer-color: var(--md-sys-color-primary);
                --_label-text-color: var(--md-sys-color-primary);
                --_pressed-label-text-color: var(--md-sys-color-primary);
                --_pressed-state-layer-color: var(--md-sys-color-primary);
                --_focus-icon-color: var(--md-sys-color-primary);
                --_hover-icon-color: var(--md-sys-color-primary);
                --_icon-color: var(--md-sys-color-primary);
                --_pressed-icon-color: var(--md-sys-color-primary);
                --_container-color: none;
                --_disabled-container-color: none;
                --_disabled-container-opacity: 0
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
            
            :host(.round-right) {
                --_container-shape-start-start: 0;
                --_container-shape-start-end: var(--shape);
                --_container-shape-end-start: 0;
                --_container-shape-end-end: var(--shape);
            }
        `,
    ];
    
    @property({type: Boolean, attribute: "no-animate-border-radius", reflect: true, useDefault: true})
    noAnimateBorderRadius: boolean = false;

    connectedCallback() {
        super.connectedCallback();
        this.noAnimateBorderRadius = true;
        setTimeout(() => this.noAnimateBorderRadius = false);
    }
}
