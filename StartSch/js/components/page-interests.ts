import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement, nothing} from "lit";
import {InterestIndex} from "../interest-index";

@customElement('page-interests')
export class PageInterests extends LitElement {
    static styles = css`
        h3 {
            margin: 12px 0 4px 0;
        }
    `;
    
    @property({type: Number}) page: number = 0;

    protected render() {
        const page = InterestIndex.pages.get(this.page);
        if (!page) return;

        const defaultCategory = page.categories.find(c => !c.name)!;

        const topLevelCategories = defaultCategory.includedCategories
            .filter(c => c.page === page);
        const includedCategories = defaultCategory.includedCategories
            .filter(c => c.page !== page);

        return html`
            <interest-toggles category="${defaultCategory.id}"></interest-toggles>
            
            <slot></slot>

            ${
                (topLevelCategories.length > 0)
                    ? html`
                        <section>
                            <h3>
                                Kategóriák
                            </h3>
                            <div style="display: flex; gap: 8px; flex-wrap: wrap">
                                ${
                                    topLevelCategories.map(category => html`
                                        <category-chip category="${category.id}"></category-chip>
                                    `)
                                }
                            </div>
                        </section>`
                    : nothing
            }

            ${
                (defaultCategory.includerCategories.length > 0)
                    ? html`
                        <section>
                            <h3>
                                Gyűjtemények
                            </h3>
                            <div style="display: flex; gap: 8px; flex-wrap: wrap">
                                <category-list
                                    .categoryIds="${[...defaultCategory.includerCategories].map(c => c.id)}"></category-list>
                            </div>
                        </section>`
                    : nothing
            }

            ${
                (includedCategories.length > 0)
                    ? html`
                        <section>
                            <h3>
                                Aloldalak
                            </h3>
                            <div style="display: flex; gap: 8px; flex-wrap: wrap">
                                <category-list .categoryIds="${[...includedCategories].map(c => c.id)}"></category-list>
                            </div>
                        </section>`
                    : nothing
            }
        `;
    }
}
