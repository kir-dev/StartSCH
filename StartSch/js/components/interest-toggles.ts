import {customElement, property} from "lit/decorators.js";
import {css, html, LitElement, PropertyValues} from "lit";
import {Interest, InterestIndex, InterestSelectionState} from "../interest-index";
import {SignalWatcher} from "@lit-labs/signals";
import tippy, {createSingleton} from "tippy.js";

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

interface InterestDescription {
    type: InterestType,
    icon: string,
    description: string
}

interface InterestDescriptionGroup {
    icon: string
    interests: InterestDescription[]
}

@customElement('interest-toggles')
export class InterestToggles extends SignalWatcher(LitElement) {
    static interestGroups: InterestDescriptionGroup[] = [
        {
            icon: 'home',
            interests: [
                {
                    type: InterestType.ShowPostsInCategory,
                    icon: 'chat',
                    description: "Posztok megjelenítése a főoldalon"
                },
                {
                    type: InterestType.ShowEventsInCategory,
                    icon: 'event',
                    description: "Események megjelenítése a főoldalon"
                },
            ],
        },
        {
            icon: 'chat_add_on',
            interests: [
                {
                    type: InterestType.PushWhenPostPublishedInCategory,
                    icon: 'mobile_chat',
                    description: "Push értesítés új posztokról"
                },
                {
                    type: InterestType.EmailWhenPostPublishedInCategory,
                    icon: 'mail',
                    description: "Email új posztokról"
                },
            ],
        },
        {
            icon: 'shopping_cart',
            interests: [
                {
                    type: InterestType.PushWhenOrderingStartedInCategory,
                    icon: 'mobile_chat',
                    description: "Push értesítés rendelés kezdetekor"
                },
                {
                    type: InterestType.EmailWhenOrderingStartedInCategory,
                    icon: 'mail',
                    description: "Email rendelés kezdetekor"
                },
            ],
        },
    ];

    @property({type: Number}) category: number = 0;

    async handleToggled(e: Event) {
        const button = e.target as (EventTarget & { interestId: number });
        const interestId = button.interestId;
        let selected = InterestIndex.subscriptions.has(interestId);
        selected = !selected;

        if (selected) {
            InterestIndex.subscriptions.add(interestId);
            await fetch(`/api/interests/${interestId}/subscriptions`, {
                method: 'PUT',
            });
        } else {
            InterestIndex.subscriptions.delete(interestId);
            await fetch(`/api/interests/${interestId}/subscriptions`, {
                method: 'DELETE',
            });
        }
    }

    protected render() {
        const category = InterestIndex.categories.get(this.category);
        if (!category)
            return;
        const categoryInterests = category.interests;

        return html`
            <div style="display: flex; flex-direction: column; gap: 8px">
                ${
                    InterestToggles.interestGroups.map(interestGroup => {
                        const interestsInGroup = interestGroup.interests
                            .map(interestType => {
                                const interest =
                                    categoryInterests.find(i => i.name == interestType.type);
                                if (!interest)
                                    return undefined;
                                return [
                                    interestType,
                                    interest,
                                ] as [description: InterestDescription, interest: Interest];
                            });
                        if (interestsInGroup.every(i => !i))
                            return null;
                        return html`
                            <div style="display: flex; gap: 2px; align-items: center">
                                <md-icon>${interestGroup.icon}</md-icon>
                                ${interestsInGroup.map((tuple) => {
                                    if (!tuple)
                                        return undefined;
                                    const [description, interest] = tuple;
                                    const icon = description.icon;
                                    const state = InterestIndex.getInterestSelectionState(interest).get();
                                    return html`
                                        <expressive-button
                                            class="extra-small ${
                                                state === InterestSelectionState.Selected
                                                    ? 'round filled'
                                                    : state === InterestSelectionState.IncluderSelected
                                                        ? 'square filled'
                                                        : 'square neutral'
                                            }"
                                            @click="${this.handleToggled}"
                                            .interestId="${interest.id}"
                                            .description="${description.description}">
                                            <md-icon>
                                                ${icon}
                                            </md-icon>
                                        </expressive-button>
                                    `;
                                })}
                            </div>
                        `;
                    })
                }
            </div>
        `;
    }

    protected firstUpdated(_changedProperties: PropertyValues) {
        createSingleton(
            tippy(
                this.renderRoot.querySelectorAll("expressive-button"),
                {
                    content(element) {
                        return (element as Element & { description: string }).description;
                    },
                },
            ),
        );
    }
}
