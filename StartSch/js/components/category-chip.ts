import {html, LitElement} from 'lit';
import {customElement, property} from 'lit/decorators.js';
import {CategoryState, InterestIndex} from "../interest-index";
import {ModalPopup} from "./modal-popup";
import {SignalWatcher} from "@lit-labs/signals";

declare global {
    interface HTMLElementTagNameMap {
        'category-chip': CategoryChip;
    }
}

@customElement('category-chip')
export class CategoryChip extends SignalWatcher(LitElement) {
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
        
        const state = InterestIndex.getCategorySelectionState(category);
        const style = this.getStyle(state);

        if (!category.name) {
            return html`
                <grouped-button class="${style} bold" @mousedown="${this.mouseDownHandlerPage}">
                    ${category.page.name}
                </grouped-button>
            `;
        }

        return html`
            <grouped-button class="${style} thin" @mousedown="${this.mouseDownHandlerCategory}">
                ${category.name}
            </grouped-button>
        `;
    }
    
    private getStyle(state: CategoryState) {
        if (state.selected || state.includerSelected)
            return "";
        if (state.includedSelected)
            return "tonal";
        return "surface";
    }
}
