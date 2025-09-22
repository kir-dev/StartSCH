import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement} from "lit";
import {Category, InterestIndex, Page} from "../interest-index";

@customElement('category-list')
export class CategoryList extends LitElement {
    static styles = css`
        :host {
            display: inline flex;
            flex-wrap: wrap;
            gap: 4px;
        }

        :host([expanded]) {
            div {
                gap: 8px;
            }
        }

        button-group {
            --round: 14px;
        }
    `;

    @property({type: Array, attribute: 'categories'})
    categoryIds!: number[];

    @property({type: Boolean, reflect: true, useDefault: true})
    expanded: boolean = false;

    // soft-disable page link if the default category is not in categoryIds
    @property({type: Boolean, attribute: 'do-not-auto-link-to-page'})
    doNotAutoLinkToPage: boolean = false;

    protected render() {
        const pages = new Map<Page, Category[]>;
        this.categoryIds
            .map(c => InterestIndex.categories.get(c)!)
            .filter(c => c)
            .forEach(category => {
                const page = category!.page;

                let categories: Category[];
                if (pages.has(page)) {
                    categories = pages.get(page)!;
                } else {
                    categories = [];
                    pages.set(page, categories);
                }

                if (category.name)
                    categories.push(category);
            });

        return html`
            ${
                [...pages.entries()]
                    .sort((a, b) => a[0].name.localeCompare(b[0].name))
                    .map(([page, categories]) => {
                        const defaultCategory = page.categories.find(c => !c.name)!;
                        const disablePageLink = this.doNotAutoLinkToPage && !categories.includes(defaultCategory);

                        if (categories.length > 1 && !this.expanded) {
                            return html`
                                <button-group>
                                    <category-chip ?soft-disabled="${disablePageLink}"
                                                   category="${defaultCategory.id}"></category-chip>
                                    <grouped-button class="tonal" @click="${() => this.expanded = true}">...
                                    </grouped-button>
                                </button-group>
                            `;
                        }

                        return html`
                            <button-group>
                                <category-chip ?soft-disabled="${disablePageLink}"
                                               category="${defaultCategory.id}"></category-chip>
                                ${
                                    categories
                                        .sort((a, b) => a.name!.localeCompare(b.name!))
                                        .map(c => html`
                                            <category-chip category="${c.id}"></category-chip>
                                        `)
                                }
                            </button-group>
                        `;
                    })
            }
        `;
    }
}
