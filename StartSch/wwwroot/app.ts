import "./interest-index";
import "./push-notifications";
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

import "@material/web/chips/suggestion-chip.js";
import "@material/web/icon/icon.js";
import "@material/web/iconbutton/icon-button.js";
import "./components/interest-container"; // needed as esbuild ignores type-only imports
import "./components/interest-toggle";
import "./components/page-chip";
import "./components/page-popup";
import {InterestToggle} from "./components/interest-toggle";
import {PageChip} from "./components/page-chip";
import {PagePopup} from "./components/page-popup";

declare global {
    interface HTMLElementTagNameMap {
        "page-chip": PageChip;
        "page-popup": PagePopup;
        "interest-toggle": InterestToggle;
    }
}
