import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement} from "lit";
import {InterestIndex} from "../interest-index";

declare global {
    interface HTMLElementTagNameMap {
        'interest-toggles': InterestToggles;
    }
}

enum InterestType {
    ShowEventsInCategory = "ShowEventsInCategory",
    ShowPostsForEvent = "ShowPostsForEvent",
    ShowPostsInCategory = "ShowPostsInCategory",
    EmailWhenOrderingStartedInCategory = "EmailWhenOrderingStartedInCategory",
    EmailWhenPostPublishedForEvent = "EmailWhenPostPublishedForEvent",
    EmailWhenPostPublishedInCategory = "EmailWhenPostPublishedInCategory",
    PushWhenOrderingStartedInCategory = "PushWhenOrderingStartedInCategory",
    PushWhenPostPublishedForEvent = "PushWhenPostPublishedForEvent",
    PushWhenPostPublishedInCategory = "PushWhenPostPublishedInCategory",
}

@customElement('interest-toggles')
export class InterestToggles extends LitElement {
    static styles = css`
        md-icon {
            opacity: .5;
        }
        
        hr {
            width: 1px;
            height: 24px;
            border: none;
            background-color: var(--md-sys-color-outline);
            margin: 0 8px;
            display: inline-block;
        }
    `;
    
    @property({type: Number}) category: number = 0;
    
    protected render(): unknown {
        const category = InterestIndex.categories.get(this.category);
        if (!category)
            return;
        const interests = category.interests;

        const renderInterestType = (interestType: string, icon: string) => {
            const interest = interests.find(i => i.name == interestType);
            if (!interest)
                return;
            return html`
                <interest-toggle interestId="${interest.id}" icon="${icon}" />
            `;
        };

        return html`
            <md-icon>home</md-icon>
            ${renderInterestType(InterestType.ShowPostsInCategory, 'chat')}
            ${renderInterestType(InterestType.ShowEventsInCategory, 'event')}
            <hr>
            <md-icon>notifications</md-icon>
            ${renderInterestType(InterestType.PushWhenPostPublishedInCategory, 'send_to_mobile')}
            ${renderInterestType(InterestType.EmailWhenPostPublishedInCategory, 'mail')}
            <hr>
            <md-icon>restaurant</md-icon>
            ${renderInterestType(InterestType.PushWhenOrderingStartedInCategory, 'send_to_mobile')}
            ${renderInterestType(InterestType.EmailWhenOrderingStartedInCategory, 'mail')}
        `;
    }
}
