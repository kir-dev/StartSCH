// https://learn.microsoft.com/en-us/aspnet/core/blazor/components/event-handling#custom-event-arguments
export function afterWebStarted(blazor) {
    blazor.registerCustomEventType('calendarrangechanged', {
        browserEventName: 'calendarrangechanged',
        createEventArgs: event => event.detail,
    });
    blazor.registerCustomEventType('calendareventclicked', {
        browserEventName: 'calendareventclicked',
        createEventArgs: event => event.detail,
    });
}
