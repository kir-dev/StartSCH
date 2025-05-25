import {LitElement, css, html} from 'lit';
import {customElement, property, state} from 'lit/decorators.js';
import * as InterestIndex from "../interest-index";
import {autoUpdate, computePosition, flip, shift} from "@floating-ui/dom";

@customElement('page-chip')
export class PageChip extends LitElement {
    static styles = css`
        span {
            padding: 1px 8px;
            display: inline;
            background: var(--md-sys-color-tertiary-container);
            color: var(--md-sys-color-on-tertiary-container);
            border-radius: 8px;
            font-weight: bold;
            font-variation-settings: "wdth" 0;
            transition: background 0.2s ease-in-out, color 0.2s ease-in-out;

            &:hover {
                outline: 1px solid var(--md-sys-color-on-tertiary-container);
                cursor: pointer;
            }
        }
    `;

    @property({type: Number}) page: number = 0;
    @property({reflect: true, type: Boolean}) open: boolean = false;

    private _clickHandler(e: Event) {
        this.setAttribute("x-data", "");
        this.open = !this.open;
        const el = document.createElement("page-chip");
        el.style.position = "absolute";
        el.style.top = "100px";
        el.style.left = "100px";
        el.textContent = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        el.page = 12;
        document.body.append(el);

        const ref = this;
        function updatePosition() {
            computePosition(ref, el, {
                placement: "top",
                middleware: [flip({padding: 96}), shift({padding: 16})]
            }).then(({x, y}) => {
                Object.assign(el.style, {
                    left: `${x}px`,
                    top: `${y}px`,
                });
            });
        }

        const cleanup = autoUpdate(
            ref,
            el,
            updatePosition,
        );
    }

    render() {
        const name = InterestIndex.pages.get(this.page)?.name;
        return html`
            <span @click="${this._clickHandler}">${name}</span>
        `;
    }
}
