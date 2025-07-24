import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement, nothing} from "lit";
import {ModalPopup} from "./modal-popup";
import {InterestIndex} from "../interest-index";

declare global {
    interface HTMLElementTagNameMap {
        'category-popup': CategoryPopup;
    }
}

@customElement('category-popup')
export class CategoryPopup extends LitElement {
    static styles = [
        ModalPopup.styles,
        css`
            h2 {
                display: inline flex;
                margin: 0;
            }
        `
    ];
    
    @property({type: Number}) category: number = 0;
    
    protected render() {
        super.render();
        
        const category = InterestIndex.categories.get(this.category);
        if (!category) return;

        const page = category.page;
        const subCategories = category.includedCategories.filter(c => c.page === page);
        const included = category.includedCategories.filter(c => c.page !== page);
        const includers = category.includerCategories.filter(c => c.page !== page);

        return html`
            <header>
                <h2>${category.name}</h2>
            </header>
            <interest-toggles category="${category.id}"></interest-toggles>

            ${
                (subCategories.length > 0)
                    ? html`
                        <section>
                            <h3>
                                Alkategóriák
                            </h3>
                            <div style="display: flex; gap: 8px; flex-wrap: wrap">
                                ${
                                    subCategories.map(category => html`
                                        <category-chip category="${category.id}"></category-chip>
                                    `)
                                }
                            </div>
                        </section>`
                    : nothing
            }
            
            ${
                (includers.length > 0)
                    ? html`
                        <section>
                            <h3>
                                Gyűjtemények
                            </h3>
                            <div style="display: flex; gap: 8px; flex-wrap: wrap">
                                <category-list .categoryIds="${[...includers].map(c => c.id)}"></category-list>
                            </div>
                        </section>`
                    : nothing
            }
            
            ${
                (included.length > 0)
                    ? html`
                        <section>
                            <h3>
                                Részkategóriák
                            </h3>
                            <div style="display: flex; gap: 8px; flex-wrap: wrap">
                                <category-list .categoryIds="${[...included].map(c => c.id)}"></category-list>
                            </div>
                        </section>`
                    : nothing
            }
        `;
    }
}
