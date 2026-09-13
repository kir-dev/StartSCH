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
                margin-bottom: 8px;

                h2 {
                    display: inline flex;
                    margin: 0;
                    
                    font-family: var(--md-sys-typescale-headline-small-font);
                    font-size: var(--md-sys-typescale-headline-small-size);
                    line-height: var(--md-sys-typescale-headline-small-line-height);
                    font-weight: var(--md-sys-typescale-headline-small-weight);
                    letter-spacing: var(--md-sys-typescale-headline-small-tracking);
                }
            }
        `
    ];

    @property({type: Number}) page: number = 0;

    protected render() {
        const page = InterestIndex.pages.get(this.page);
        if (!page) return;
        
        return html`
            <header>
                <a href="/pages/${page.id}">
                    <h2 class="typescale-title-small">${page.name}</h2>
                    <md-icon>
                        arrow_forward
                    </md-icon>
                </a>
            </header>
            
            <page-interests page="${this.page}"></page-interests>
        `;
    }
}
