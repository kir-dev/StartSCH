import {css, html} from 'lit';
import {customElement, property} from 'lit/decorators.js';
import * as InterestIndex from "../interest-index";
import {InterestContainer} from "./interest-container";

@customElement('page-chip')
export class PageChip extends InterestContainer {
    static styles = css`
        a {
            padding: 1px 8px;
            display: inline-block;
            background: var(--md-sys-color-tertiary-container);
            color: var(--md-sys-color-on-tertiary-container);
            border-radius: 8px;
            font-family: "Roboto Serif", serif;
            font-weight: bold;
            font-variation-settings: "wdth" 0;
            cursor: pointer;
            position: relative;
            border: none;
            text-decoration: none;
            line-height: 1.2;

            &:hover {
                box-shadow: var(--md-sys-shadow-2);
            }
        }
        
        :host {
            position: relative;
        }
        
        :host([haspopup]) {
            z-index: 200;
            
            a {
                box-shadow: var(--md-sys-shadow-2);
            }
        }
    `;

    @property({type: Number}) page: number = 0;

    private mouseDownHandler(e: MouseEvent) {
        if (e.button !== 0)
            return;
        e.preventDefault();
        
        if (this.hasPopup) {
            this.hasPopup = false;
            return;
        }
        this.showPopup(this.page);
    }
    
    private clickHandler(e: MouseEvent) {
        e.preventDefault();
    }

    protected render() {
        super.render();
        
        const name = InterestIndex.pages.get(this.page)?.name;
        return html`
            <a href="/pages/${this.page}" @mousedown="${this.mouseDownHandler}" @click="${this.clickHandler}">
                <md-ripple></md-ripple>
                ${name}
            </a>
        `;
    }
}
