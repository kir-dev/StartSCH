import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement} from "lit";
import {InterestIndex} from "../interest-index";
import {SelectableButton} from "./selectable-button";

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
        div {
            display: flex;
            flex-direction: column;
            gap: 4px;
        }
    `;
    
    @property({type: Number}) category: number = 0;
    
    async handleToggled(e: Event) {
        const button = e.target as (SelectableButton & { interestId: number });
        const interestId = button.interestId;
        let selected = InterestIndex.subscriptions.has(interestId);
        selected = !selected;
        
        if (selected) {
            InterestIndex.subscriptions.add(interestId);
            this.requestUpdate();
            await fetch(`/api/interests/${interestId}/subscriptions`, {
                method: 'PUT',
            });
        }
        else {
            InterestIndex.subscriptions.delete(interestId);
            this.requestUpdate();
            await fetch(`/api/interests/${interestId}/subscriptions`, {
                method: 'DELETE',
            });
        }
    }

    static interestGroups = [
        {
            icon: 'home',
            interests: [
                {name: InterestType.ShowPostsInCategory, icon: 'chat'},
                {name: InterestType.ShowEventsInCategory, icon: 'event'}
            ]
        },
        {
            icon: 'chat_add_on',
            interests: [
                {name: InterestType.PushWhenPostPublishedInCategory, icon: 'mobile_chat'},
                {name: InterestType.EmailWhenPostPublishedInCategory, icon: 'mail'}
            ]
        },
        {
            icon: 'shopping_cart',
            interests: [
                {name: InterestType.PushWhenOrderingStartedInCategory, icon: 'mobile_chat'},
                {name: InterestType.EmailWhenOrderingStartedInCategory, icon: 'mail'}
            ]
        },
    ];
    
    protected render() {
        const category = InterestIndex.categories.get(this.category);
        if (!category)
            return;
        const categoryInterests = category.interests;

        return html`
            <div>
                ${
                    InterestToggles.interestGroups.map(interestGroup => {
                        const groupInterests = interestGroup.interests
                            .map(interestType => categoryInterests
                                .find(i => i.name == interestType.name))
                        if (groupInterests.every(i => i == undefined))
                            return null;
                        return html`
                            <button-group>
                                <md-icon>${interestGroup.icon}</md-icon>
                                ${groupInterests.map((interest, index) => {
                                    if (interest == undefined)
                                        return null;
                                    const icon = interestGroup.interests[index].icon;
                                    return html`
                                        <selectable-button
                                            @click="${this.handleToggled}"
                                            ?selected="${InterestIndex.subscriptions.has(interest.id)}"
                                            .interestId="${interest.id}">
                                            <md-icon>
                                                ${icon}
                                            </md-icon>
                                        </selectable-button>
                                    `;
                                })}
                            </button-group>
                        `;
                    })
                }
            </div>
        `;
    }
}
