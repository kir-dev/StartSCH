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

    protected render() {
        super.render();

        const category = InterestIndex.categories.get(this.category);
        if (!category) return;

        if (category.name) {
            return html`
                <grouped-button class="tonal" @mousedown="${this.mouseDownHandlerCategory}">
                    ${category.name}
                </grouped-button>
            `;
        }

        return html`
            <grouped-button @mousedown="${this.mouseDownHandlerPage}">
                ${category.page.name}
            </grouped-button>
        `;
    }
}
