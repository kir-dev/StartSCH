import {LitElement} from "lit";
import {customElement, property} from "lit/decorators.js";

const DEBOUNCE_MS = 300;

@customElement('debounced-textarea')
export class DebouncedTextarea extends LitElement {

    @property()
    value: string = "";

    private _textarea: HTMLTextAreaElement;
    private _debounceTimer: ReturnType<typeof setTimeout> | null = null;

    constructor() {
        super();
        this._textarea = document.createElement('textarea');
        this._textarea.addEventListener('input', () => {
            if (this._debounceTimer !== null)
                clearTimeout(this._debounceTimer);
            this._debounceTimer = setTimeout(() => {
                this._debounceTimer = null;
                this.value = this._textarea.value;
                this.dispatchEvent(new Event('change', {bubbles: true}));
            }, DEBOUNCE_MS);
        });
    }

    // Skip Lit's shadow DOM entirely — render directly into the host element.
    protected createRenderRoot() {
        return this;
    }

    connectedCallback() {
        super.connectedCallback();
        // Transfer style/class from host to textarea and append it.
        this._textarea.style.cssText = this.style.cssText;
        this.style.cssText = '';
        this.appendChild(this._textarea);
    }

    // When Blazor pushes a new value attribute, sync it to the textarea
    // (but only if the textarea isn't the source of the change).
    updated(changed: Map<string, unknown>) {
        if (changed.has('value') && this._textarea.value !== this.value) {
            this._textarea.value = this.value ?? '';
        }
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
