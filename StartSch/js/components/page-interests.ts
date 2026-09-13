import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement, nothing} from "lit";
import {InterestIndex} from "../interest-index";

@customElement('page-interests')
export class PageInterests extends LitElement {
    static styles = css`
        h2 {
            margin: 12px 0 4px 0;

            font-family: var(--md-sys-typescale-label-large-font);
            font-size: var(--md-sys-typescale-label-large-size);
            line-height: var(--md-sys-typescale-label-large-line-height);
            font-weight: var(--md-sys-typescale-label-large-weight);
            letter-spacing: var(--md-sys-typescale-label-large-tracking);
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
                            <h2>
                                Kategóriák
                            </h2>
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
                            <h2>
                                Gyűjtemények
                            </h2>
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
                            <h2>
                                Aloldalak
                            </h2>
                            <div style="display: flex; gap: 8px; flex-wrap: wrap">
                                <category-list .categoryIds="${[...includedCategories].map(c => c.id)}"></category-list>
                            </div>
                        </section>`
                    : nothing
            }
        `;
    }
}
