import {customElement, property} from "lit/decorators.js";
import {css, html} from "lit";
import {InterestContainer} from "./interest-container";
import {InterestIndex} from "../interest-index";

@customElement('category-popup')
export class CategoryPopup extends InterestContainer {
    static styles = css`
        :host {
            position: absolute;
            z-index: 200;
        }

        .surface {
            padding: 8px;
            background-color: var(--md-sys-color-tertiary-container);
            border-radius: 8px;
            box-shadow: var(--md-sys-shadow-2);
        }
        
        .interest-set {
            display: inline-block;
        }
    `;

    private _cleanup?: () => void;

    @property({type: Number}) category: number = 0;

    set cleanup(value: () => void) {
        this._cleanup = value;
    }

    connectedCallback() {
        super.connectedCallback();
        document.getElementById('popup-scrim')!.style.display = 'block';
    }

    disconnectedCallback() {
        super.disconnectedCallback();
        this._cleanup?.();
        if (document.querySelectorAll('page-popup').length === 0)
            document.getElementById('popup-scrim')!.style.display = '';
    }

    protected render() {
        super.render();

        const page = InterestIndex.categories.get(this.category)!.page;
        if (!page) return;

        return html`
            <div class="surface">
                ${page.categories.map(category => html`
                    ${category.page.name}
                    <div class="interest-set">
                        ${category.interests.map(interest => html`
                            <interest-toggle interestId="${interest.id}" interestName="${interest.name}" />
                        `)}
                    </div>
                `)}
            </div>
        `;
    }
}
