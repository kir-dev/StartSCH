import {customElement, property} from "lit/decorators.js";
import {css, html} from "lit";
import {InterestContainer} from "./interest-container";
import {InterestIndex} from "../interest-index";

declare global {
    interface HTMLElementTagNameMap {
        'page-popup': PagePopup;
    }
}

@customElement('page-popup')
export class PagePopup extends InterestContainer {
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
    `;

    private _cleanup?: () => void;

    @property({type: Number}) page: number = 0;

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

        const page = InterestIndex.pages.get(this.page);
        if (!page) return;

        return html`
            <div class="surface">
                ${page.categories.map(category => html`
                    <interest-toggles category="${category.id}"/>
                `)}
            </div>
        `;
    }
}
