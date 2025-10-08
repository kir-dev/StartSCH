import {Signal} from "signal-polyfill";

export class ExternalSignal<T> {
    private readonly signal: Signal.State<T>;
    private readonly getValue: () => T;

    constructor(getValue: () => T) {
        this.getValue = getValue;
        this.signal = new Signal.State(getValue());
    }
    
    get() {
        return this.signal.get();
    }

    refresh() {
        this.signal.set(this.getValue());
    }
}
