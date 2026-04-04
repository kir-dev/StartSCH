import {css, html, LitElement} from "lit";
import {ref, Ref, createRef} from 'lit/directives/ref.js';
import {customElement} from "lit/decorators.js";
import {Calendar, CalendarOptions} from "@fullcalendar/core";
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
            --fc-event-bg-color: var(--md-sys-color-primary);
            --fc-event-border-color: var(--md-sys-color-primary);
            --fc-event-text-color: var(--md-sys-color-on-primary);
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

    opts: CalendarOptions = {
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
        locale: huLocale,
        events: (info, _successCallback, _failureCallback) => {
            const event = new CustomEvent('fullcalendargetevents', {
                detail: info,
                bubbles: true,
                composed: true,
            });
            this.dispatchEvent(event);
            _successCallback([]);
        }
    };

    calendarRootRef: Ref<HTMLDivElement> = createRef();

    firstUpdated() {
        const cal = new Calendar(this.calendarRootRef.value as HTMLElement, this.opts);
        cal.render();
    }

    protected render() {
        return html`
            <div ${ref(this.calendarRootRef)}></div>
        `;
    }
}
