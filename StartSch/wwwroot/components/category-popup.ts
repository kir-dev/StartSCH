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
        `
    ];
    
    @property({type: Number}) category: number = 0;
    
    protected render() {
        super.render();
        
        const category = InterestIndex.categories.get(this.category);
        if (!category) return;

        const includers = category.includerCategories;
        const included = category.includedCategories;

        return html`
            <header>
                <a href="/pages/${category.id}">
                    <h2>${category.name}</h2>
                    <md-icon>
                        open_in_new
                    </md-icon>
                </a>
            </header>
            <interest-toggles category="${category.id}"/>
            ${
                (includers.length > 0)
                    ? html`
                        <section>
                            <h3>
                                Gyűjtemények
                            </h3>
                            ${[...includers].map(category => html`
                                <category-chip category="${category.id}"/>
                            `)}
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
                            ${[...included].map(category => html`
                                <category-chip category="${category.id}"/>
                            `)}
                        </section>`
                    : nothing
            }
        `;
    }
}
