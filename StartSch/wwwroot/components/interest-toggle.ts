import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement} from "lit";

@customElement('interest-toggle')
export class InterestToggle extends LitElement {
    static interestToIcon: Record<string, string> = {
        'ShowEventsInCategory': 'home',
        'ShowPostsForEvent': 'home',
        'ShowPostsInCategory': 'home',
        'EmailWhenOrderingStartedInCategory': 'mail',
        'EmailWhenPostPublishedForEvent': 'mail',
        'EmailWhenPostPublishedInCategory': 'mail',
        'PushWhenOrderingStartedInCategory': 'send_to_mobile',
        'PushWhenPostPublishedForEvent': 'send_to_mobile',
        'PushWhenPostPublishedInCategory': 'send_to_mobile',
    };
    
    static styles = css`
        :root {
            background-color: blue;
        }
    `;
    
    @property({type: Boolean, reflect: true, useDefault: true}) toggled: boolean = false;
    @property({type: Number}) interestId: number = 0;
    @property() interestName: string = "";
    
    handleClick() {
        console.log(this.interestId);
    }

    protected render() {
        super.render();
        return html`
            <md-icon-button @click="${this.handleClick}">
                <md-icon>
                    ${InterestToggle.interestToIcon[this.interestName]}
                </md-icon>
            </md-icon-button>
        `;
    }
}
