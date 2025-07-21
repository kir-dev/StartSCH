import {css, html, LitElement} from 'lit';
import {customElement, property} from 'lit/decorators.js';
import {InterestIndex} from "../interest-index";
import {ModalPopup} from "./modal-popup";

declare global {
    interface HTMLElementTagNameMap {
        'page-chip': PageChip;
    }
}

@customElement('page-chip')
export class PageChip extends LitElement {
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
    `;

    @property({type: Number}) page: number = 0;

    private mouseDownHandler(e: MouseEvent) {
        if (e.button !== 0)
            return;
        e.preventDefault();
        
        ModalPopup.create(this, "page-popup", p => p.page = this.page);
    }
    
    private clickHandler(e: MouseEvent) {
        e.preventDefault();
    }

    protected render() {
        super.render();
        
        const name = InterestIndex.pages.get(this.page)!.name;
        return html`
            <a href="/pages/${this.page}" @mousedown="${this.mouseDownHandler}" @click="${this.clickHandler}">
                <md-ripple></md-ripple>
                ${name}
            </a>
        `;
    }
}
