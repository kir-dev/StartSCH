import {LitElement, html} from "lit";
import {customElement} from "lit/decorators.js";

declare global {
    interface HTMLElementTagNameMap {
        'login-and-return-button': LoginAndReturnButton;
    }
}

@customElement('login-and-return-button')
export class LoginAndReturnButton extends LitElement {
    private getLoginHref(): string {
        const current = window.location?.href ?? '/';
        const encoded = encodeURIComponent(current);
        return `/authentication/login?returnUrl=${encoded}`;
    }

    render() {
        return html`
            <expressive-button class="small round" href="${this.getLoginHref()}">
                Bejelentkezés
            </expressive-button>
        `;
    }
}
