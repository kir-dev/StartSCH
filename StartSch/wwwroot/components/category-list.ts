import {customElement, property} from "lit/decorators.js";
import {html, LitElement} from "lit";
import {Category, InterestIndex, Page} from "../interest-index";

@customElement('category-list')
export class CategoryList extends LitElement {
    @property({type: Array, attribute: 'categories'})
    categoryIds!: number[];

    @property({type: Boolean})
    expanded: boolean = false;

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
                    .map(([page, categories]) => html`
                        ${page.name}
                        ${
                            categories.length > 1 && !this.expanded
                                ? html`<span @click="${() => this.expanded = true}">...</span>`
                                : categories
                                    .sort((a, b) => a.name!.localeCompare(b.name!))
                                    .map(c => c.name)
                        }
                    `)
            }
        `;
    }
}
