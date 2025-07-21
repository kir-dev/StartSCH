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
            :host {
                position: absolute;
                z-index: 200;
            }

            header > a {
                display: flex;
                align-items: center;
                justify-content: space-between;
                gap: 16px;
                text-decoration: none;
                color: var(--md-sys-color-on-tertiary-container);
                font-family: "Roboto Serif", serif;
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
        super.render();

        const page = InterestIndex.pages.get(this.page);
        if (!page) return;

        const defaultCategory = page.categories.find(c => !c.name);

        const includers = new Set(page.categories.flatMap(c => c.includerCategories));
        const included = new Set(page.categories.flatMap(c => c.includedCategories));

        return html`
            <header>
                <a href="/pages/${page.id}">
                    <h2>${page.name}</h2>
                    <md-icon>
                        open_in_new
                    </md-icon>
                </a>
            </header>
            ${defaultCategory && html`
                <interest-toggles category="${defaultCategory.id}"/>
            `}
            ${page.categories.filter(c => c.name).map(category => html`
                <interest-toggles category="${category.id}"/>
            `)}
            ${
                (includers.size > 0)
                    ? html`
                        <section>
                            <h3>
                                Gyűjtemények
                            </h3>
                            <div style="display: flex; gap: 8px">
                                ${[...includers].map(category => html`
                                    <category-chip category="${category.id}"/>
                                `)}
                            </div>
                        </section>`
                    : nothing
            }
            ${
                (included.size > 0)
                    ? html`
                        <section>
                            <h3>
                                Aloldalak
                            </h3>
                            <div style="display: flex; gap: 8px; flex-wrap: wrap">
                                ${[...included].map(category => html`
                                    <category-chip category="${category.id}"/>
                                `)}
                            </div>
                        </section>`
                    : nothing
            }
        `;
    }
}
