import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement, nothing} from "lit";
import {InterestIndex} from "../interest-index";
import {ModalPopup} from "./modal-popup";

declare global {
    interface HTMLElementTagNameMap {
        'page-popup': PagePopup;
    }
}

@customElement('page-popup')
export class PagePopup extends LitElement {
    static styles = [
        ModalPopup.styles,
        css`
            header > a {
                display: flex;
                align-items: center;
                justify-content: space-between;
                gap: 16px;
                text-decoration: none;
                color: var(--md-sys-color-on-tertiary-container);
                font-weight: bold;
                font-variation-settings: "wdth" 0;
                margin-bottom: 8px;

                h2 {
                    display: inline flex;
                    margin: 0;
                }
            }
        `
    ];

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
            <header>
                <a href="/pages/${page.id}">
                    <h2>${page.name}</h2>
                    <md-icon>
                        open_in_full
                    </md-icon>
                </a>
            </header>
            
            <page-interests page="${this.page}"></page-interests>
        `;
    }
}
