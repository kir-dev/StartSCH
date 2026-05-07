import {css, html, LitElement, PropertyValues} from "lit";
import {ref, Ref, createRef} from 'lit/directives/ref.js';
import {customElement, property} from "lit/decorators.js";
import {Calendar, EventInput} from "@fullcalendar/core";
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import huLocale from '@fullcalendar/core/locales/hu';

@customElement('calendar-view')
export class CalendarView extends LitElement {
    static styles = css`
        div {
            /* Sizing & Typography (Retained) */
            --fc-small-font-size: .85em;
            --fc-event-resizer-thickness: 8px;
            --fc-event-resizer-dot-total-width: 8px;
            --fc-event-resizer-dot-border-width: 1px;
            --fc-bg-event-opacity: 0.3;

            /* Base Backgrounds & Borders */
            --fc-page-bg-color: var(--md-sys-color-background);
            --fc-neutral-bg-color: var(--md-sys-color-surface-variant);
            --fc-neutral-text-color: var(--md-sys-color-on-surface-variant);
            --fc-border-color: var(--md-sys-color-outline-variant);

            /* Buttons (Mapped to MD3 Primary roles) */
            --fc-button-text-color: var(--md-sys-color-on-primary);
            --fc-button-bg-color: var(--md-sys-color-primary);
            --fc-button-border-color: var(--md-sys-color-primary);

            /* MD3 Button State Layers (Calculated dynamically using color-mix) */
            --fc-button-hover-bg-color: color-mix(in srgb, var(--md-sys-color-primary), var(--md-sys-color-on-primary) 8%);
            --fc-button-hover-border-color: color-mix(in srgb, var(--md-sys-color-primary), var(--md-sys-color-on-primary) 8%);
            --fc-button-active-bg-color: color-mix(in srgb, var(--md-sys-color-primary), var(--md-sys-color-on-primary) 12%);
            --fc-button-active-border-color: color-mix(in srgb, var(--md-sys-color-primary), var(--md-sys-color-on-primary) 12%);

            /* Events */
            --fc-event-bg-color: var(--md-sys-color-surface);
            --fc-event-border-color: var(--md-sys-color-outline-variant);
            --fc-event-text-color: var(--md-sys-color-on-surface);
            --fc-event-selected-overlay-color: color-mix(in srgb, var(--md-sys-color-on-surface) 25%, transparent);

            /* More Link (+x events) */
            --fc-more-link-bg-color: var(--md-sys-color-surface-container-highest);
            --fc-more-link-text-color: var(--md-sys-color-on-surface);

            /* Calendar Contextual Colors */
            --fc-non-business-color: color-mix(in srgb, var(--md-sys-color-on-surface) 4%, transparent);
            --fc-bg-event-color: var(--md-sys-color-secondary-container);
            --fc-highlight-color: color-mix(in srgb, var(--md-sys-color-primary) 10%, transparent);
            --fc-today-bg-color: color-mix(in srgb, var(--md-sys-color-primary) 5%, transparent);
            --fc-now-indicator-color: var(--md-sys-color-error);
        }
    `;

    calendarRootRef: Ref<HTMLDivElement> = createRef();
    calendar?: Calendar;

    @property({type: Array}) events: EventInput[] = [];

    render() {
        return html`
            <div ${ref(this.calendarRootRef)}></div>
        `;
    }

    firstUpdated() {
        this.calendar = new Calendar(
            this.calendarRootRef.value as HTMLElement,
            {
                plugins: [
                    dayGridPlugin,
                    timeGridPlugin,
                ],
                initialView: 'timeGridWeek',
                headerToolbar: {
                    left: 'prev,next today',
                    center: 'title',
                    right: 'dayGridMonth,timeGridWeek'
                },
                height: "100%",
                locale: huLocale,
                lazyFetching: false,
                datesSet: (dateInfo) => {
                    this.dispatchEvent(new CustomEvent('calendarrangechanged', {
                        detail: {
                            start: dateInfo.startStr,
                            end: dateInfo.endStr,
                        },
                        bubbles: true,
                        composed: true
                    }));
                },
                eventClick: (clickInfo) => {
                    this.dispatchEvent(new CustomEvent('calendareventclicked', {
                        detail: {
                            calendarId: clickInfo.event.extendedProps.calendarId,
                            eventId: clickInfo.event.id,
                        },
                        bubbles: true,
                        composed: true
                    }));
                },
                events: (_info, successCallback) => {
                    successCallback(this.events);
                },
            });
        this.calendar.render();
    }

    updated(_changedProperties: PropertyValues) {
        if (_changedProperties.has('events')) this.calendar!.refetchEvents();
    }
}
