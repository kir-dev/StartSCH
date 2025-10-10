import {css, html, LitElement} from "lit";

export class LoadingBar extends LitElement {
    static styles = css`
        .bar {
            height: 4px;
            width: 100%;
            background: linear-gradient(90deg, #0000 33%, var(--md-sys-color-primary) 50%, #0000 66%);
            background-size: 300% 100%;
            animation: l1 1s infinite linear;
        }

        @keyframes l1 {
            0% {
                background-position: right
            }
        }
    `;
    
    protected render() {
        return html`
            <div style="height: 48px; display: flex; align-items: end">
                <div class="bar"></div>
            </div>
        `;
    }
}
