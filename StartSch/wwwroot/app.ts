import "./interest-index";
import "./push-notifications";
import {initializeInterestIndex} from "./interest-index";
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
import "@material/web/iconbutton/filled-icon-button.js";
import "@material/web/iconbutton/filled-tonal-icon-button.js";
import "@material/web/iconbutton/outlined-icon-button.js";
import "@material/web/ripple/ripple.js";
import "./components/category-chip"; // needed as esbuild ignores type-only imports
import "./components/category-chips";
import "./components/category-popup";
import "./components/interest-container";
import "./components/interest-toggle";
import "./components/interest-toggles";
import "./components/page-chip";
import "./components/page-popup";
import {CategoryChips} from "./components/category-chips";
import {CategoryChip} from "./components/category-chip";
import {CategoryPopup} from "./components/category-popup";
import {InterestToggle} from "./components/interest-toggle";
import {InterestToggles} from "./components/interest-toggles";
import {PageChip} from "./components/page-chip";
import {PagePopup} from "./components/page-popup";

declare global {
    interface HTMLElementTagNameMap {
        "page-chip": PageChip
        "page-popup": PagePopup
        "category-chip": CategoryChip
        "category-chips": CategoryChips
        "category-popup": CategoryPopup
        "interest-toggle": InterestToggle
        "interest-toggles": InterestToggles
    }
}
