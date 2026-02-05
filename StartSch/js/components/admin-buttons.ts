import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement} from "lit";
import {Category, InterestIndex, Page} from "../interest-index";

@customElement('admin-buttons')
export class AdminButtons extends LitElement {
    static styles = css`
        :host {
            display: inline flex;
            flex-wrap: wrap;
            gap: 4px;
        }

        div {
            display: flex;
            gap: 2px;
        }
    `;

    @property({type: Array, attribute: 'categories'})
    categoryIds!: number[];

    protected render() {
        const pages = new Set(
            this.categoryIds
                .map(c => InterestIndex.categories.get(c)!)
                .filter(c => c)
                .map(c => c.page)
        );
        const administeredPages = window.administeredPageIds;
        const canAdminister = administeredPages.intersection(pages).size > 0;
        
        if (!canAdminister)
            return null;

        return html`
            <md-icon-button></md-icon-button>
        `;
    }
}
