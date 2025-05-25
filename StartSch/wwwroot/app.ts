import "./push-notifications";
import "./interest-index";
import {initialize as initializeInterestIndex} from "./interest-index";
import Alpine from 'alpinejs'

declare namespace startSch {
    let interestIndexJson: string
}

Alpine.data('chip', (id: number) => ({
    attrs: {
        ["x-on:mouseover"]() {
            console.log(id);
        },
        ["href"]: "/groups/" + id
    }
}));
Alpine.start();

initializeInterestIndex(startSch.interestIndexJson)


import "./components/page-chip";
import {PageChip} from "./components/page-chip";

declare global {
    interface HTMLElementTagNameMap {
        "page-chip": PageChip;
    }
}
