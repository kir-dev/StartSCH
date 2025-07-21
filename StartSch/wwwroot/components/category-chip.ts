import {css, html, LitElement} from 'lit';
import {customElement, property} from 'lit/decorators.js';
import {InterestIndex} from "../interest-index";
import {ModalPopup} from "./modal-popup";
import {MdFilledTonalButton} from "@material/web/all";

declare global {
    interface HTMLElementTagNameMap {
        'category-chip': CategoryChip;
    }
}

@customElement('category-chip')
export class CategoryChip extends LitElement {
    static styles = css`
        button {
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

    @property({type: Number}) category: number = 0;

    private mouseDownHandlerCategory(e: MouseEvent) {
        if (e.button !== 0)
            return;
        e.preventDefault();

        ModalPopup.create(this, "category-popup", p => p.category = this.category);
    }

    private mouseDownHandlerPage(e: MouseEvent) {
        if (e.button !== 0)
            return;
        e.preventDefault();

        const category = InterestIndex.categories.get(this.category)!;
        ModalPopup.create(this, "page-popup", p => p.page = category.page.id);
    }

    private clickHandler(e: MouseEvent) {
        e.preventDefault();
    }

    protected render() {
        super.render();

        const category = InterestIndex.categories.get(this.category);
        if (!category) return;

        if (category.name) {
            return html`
                <button @mousedown="${this.mouseDownHandlerCategory}" @click="${this.clickHandler}">
                    <md-ripple></md-ripple>
                    ${category.name}
                </button>
            `;
        }

        return html`
            <button @mousedown="${this.mouseDownHandlerPage}" @click="${this.clickHandler}">
                <md-ripple></md-ripple>
                ${category.page.name}
            </button>
        `;
    }
}
