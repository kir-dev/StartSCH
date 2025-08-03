import {customElement} from "lit/decorators.js";
import {Button} from "@material/web/button/internal/button";
import {css, html} from "lit";
import {styles as filledStyles} from '@material/web/button/internal/filled-styles';
import {styles as sharedElevationStyles} from '@material/web/button/internal/shared-elevation-styles';
import {styles as sharedStyles} from '@material/web/button/internal/shared-styles';

@customElement('grouped-button')
export class GroupedButton extends Button {
    static styles = [
        sharedStyles,
        sharedElevationStyles,
        filledStyles,
        css`
            :host {
                --_container-height: 28px;
                --_leading-space: 10px;
                --_trailing-space: 10px;
                
                --_container-shape-start-start: var(--container-shape-start-start, 4px);
                --_container-shape-start-end: var(--container-shape-start-end, 4px);
                --_container-shape-end-start: var(--container-shape-end-start, 4px);
                --_container-shape-end-end: var(--container-shape-end-end, 4px);
            }

            :host(.tonal) {
                --md-sys-color-primary: var(--md-sys-color-primary-container);
                --md-sys-color-on-primary: var(--md-sys-color-on-primary-container);
            }
            
            .button {
                min-width: calc(48px - var(--_leading-space) - var(--_trailing-space));
            }
            
            .touch {
                height: 32px;
            }
        `,
    ];
}
