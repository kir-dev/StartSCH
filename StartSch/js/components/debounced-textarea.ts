import {css, html, LitElement} from "lit";
import {customElement, property} from "lit/decorators.js";

const DEBOUNCE_MS = 300;

@customElement('debounced-textarea')
export class DebouncedTextarea extends LitElement {
    static styles = css`
        :host { display: contents }
        textarea { width: 100%; box-sizing: border-box }
    `;

    @property()
    value: string = "";

    private _debounceTimer: ReturnType<typeof setTimeout> | null = null;

    protected render() {
        return html`
            <textarea
                .value="${this.value}"
                style="${this.getAttribute('style') ?? ''}"
                class="${this.getAttribute('class') ?? ''}"
                @input="${this._onInput}"
            ></textarea>
        `;
    }

    private _onInput(e: Event) {
        const newValue = (e.target as HTMLTextAreaElement).value;

        if (this._debounceTimer !== null)
            clearTimeout(this._debounceTimer);

        this._debounceTimer = setTimeout(() => {
            this._debounceTimer = null;
            this.value = newValue;
            this.dispatchEvent(new CustomEvent('change', {bubbles: true, composed: true}));
        }, DEBOUNCE_MS);
    }

    disconnectedCallback() {
        super.disconnectedCallback();
        if (this._debounceTimer !== null) {
            clearTimeout(this._debounceTimer);
            this._debounceTimer = null;
        }
    }
}

declare global {
    interface HTMLElementTagNameMap {
        'debounced-textarea': DebouncedTextarea;
    }
}
