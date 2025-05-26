import {css, html} from 'lit';
import {customElement, property} from 'lit/decorators.js';
import * as InterestIndex from "../interest-index";
import {InterestContainer} from "./interest-container";

@customElement('page-chip')
export class PageChip extends InterestContainer {
    static styles = css`
        span {
            padding: 1px 8px;
            display: inline;
            background: var(--md-sys-color-tertiary-container);
            color: var(--md-sys-color-on-tertiary-container);
            border-radius: 8px;
            font-weight: bold;
            font-variation-settings: "wdth" 0;
            //transition: background 0.2s ease-in-out, color 0.2s ease-in-out;

            &:hover {
                cursor: pointer;
                box-shadow: var(--md-sys-shadow-2);
            }
        }
    `;

    @property({type: Number}) page: number = 0;

    private _clickHandler(e: Event) {
        if (this.hasPopup) {
            this.hasPopup = false;
            return;
        }
        this.showPopup(this.page);
    }

    protected render() {
        super.render();
        
        const name = InterestIndex.pages.get(this.page)?.name;
        this.style.zIndex = this.hasPopup ? "200" : "";
        return html`
            <span @click="${this._clickHandler}">${name}</span>
        `;
    }
}
