import {customElement, property} from "lit/decorators.js";
import {html, LitElement, nothing, PropertyValues} from "lit";
import {Interest, InterestIndex, InterestSelectionState} from "../interest-index";
import {SignalWatcher} from "@lit-labs/signals";
import tippy, {createSingleton} from "tippy.js";
import * as PushSubscriptions from "../push-subscriptions";

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
        // {
        //     icon: 'home',
        //     interests: [
        //         {
        //             type: InterestType.ShowPostsInCategory,
        //             icon: 'chat',
        //             description: "Posztok megjelenítése a főoldalon"
        //         },
        //         {
        //             type: InterestType.ShowEventsInCategory,
        //             icon: 'event',
        //             description: "Események megjelenítése a főoldalon"
        //         },
        //     ],
        // },
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
        const interest = InterestIndex.interests.get(interestId)!;
        let selected = InterestIndex.subscriptions.has(interestId);
        selected = !selected;

        if (selected) {
            const registerDeviceForPush =
                interest.name.includes('Push')
                && !PushSubscriptions.hasRegisteredDevices.get()
                && !PushSubscriptions.noPushOnThisDevice.get()
                && !PushSubscriptions.pushInterestsFollowed.get()
                && PushSubscriptions.permissionState.get() === "default";

            InterestIndex.subscriptions.add(interestId);
            const followInterest = fetch(`/api/interests/${interestId}/subscriptions`, {
                method: 'PUT',
            });

            if (registerDeviceForPush) {
                const registerPush = PushSubscriptions.registerDevice();
                await Promise.all([followInterest, registerPush]);
            } else
                await followInterest;
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
        const loggedIn = window.isAuthenticated;

        return html`
            ${
                !loggedIn && html`
                    <div style="display: flex; flex-direction: column; gap: 4px; margin: 16px 0">
                        Érdeklődési körök követéséhez jelentkezz be:
                        <login-and-return-button style="margin: 8px"></login-and-return-button>
                    </div>
                ` || nothing
            }

            ${
                PushSubscriptions.suggestSubscribing.get() &&
                (
                    PushSubscriptions.permissionState.get() === "denied"
                        ? html`
                            <div style="margin: 8px; font-size: 14px; color: var(--md-sys-color-on-surface-variant)">
                                Letiltottad az oldal számára az értesítések küldését. Ha szeretnél értesítéseket kapni ezen
                                az eszközön, engedélyezd ezt a böngésződ beállításiban.
                            </div>
                        `
                        : html`
                            <div style="margin: 12px; font-size: 14px; color: var(--md-sys-color-on-surface-variant)">
                                Szeretnél értesítéseket kapni ezen az eszközön?
                                <div style="display: flex; gap: 4px; margin-top: 8px">
                                    <expressive-button
                                        class="extra-small filled round"
                                        @click="${PushSubscriptions.registerDevice}">
                                        Bekapcsolás
                                    </expressive-button>
                                    <expressive-button
                                        class="extra-small tonal round"
                                        @click="${() => PushSubscriptions.noPushOnThisDevice.set(true)}">
                                        Inkább másik eszközön
                                    </expressive-button>
                                </div>
                            </div>
                        `
                ) || nothing
            }

            <div class="toggles" style="display: flex; flex-direction: column; gap: 8px">
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
                                <md-icon style="color: var(--md-sys-color-on-surface-variant)">${interestGroup.icon}
                                </md-icon>
                                ${interestsInGroup.map((tuple) => {
                                    if (!tuple)
                                        return undefined;
                                    const [description, interest] = tuple;
                                    const icon = description.icon;
                                    const state = InterestIndex.getInterestSelectionState(interest).get();
                                    return html`
                                        <expressive-button
                                            ?soft-disabled="${!loggedIn}"
                                            class="extra-small ${
                                                state === InterestSelectionState.Selected
                                                    ? 'square filled'
                                                    : state === InterestSelectionState.IncluderSelected
                                                        ? 'round filled'
                                                        : 'round neutral'
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
                this.renderRoot.querySelectorAll(".toggles expressive-button"),
                {
                    content(element) {
                        return (element as Element & { description: string }).description;
                    },
                },
            ),
        );
    }
}
