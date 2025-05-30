import {LitElement} from "lit";
import {property} from "lit/decorators.js";
import {autoUpdate, computePosition, flip, offset, shift} from "@floating-ui/dom";
import {PagePopup} from "./page-popup";

export class InterestContainer extends LitElement {
    private popup?: PagePopup;

    @property({type: Boolean, useDefault: true, reflect: true})
    hasPopup: boolean = false;

    showPagePopup(pageId: number) {
        this.hasPopup = true;
        
        // can't use new PagePopup() here as that causes a circular reference between modules that esbuild can't handle.
        // this way, even though we import PagePopup, esbuild ignores it, as we only use it as a type
        this.popup = document.createElement('page-popup');
        this.popup.page = pageId;
        document.body.append(this.popup);

        this.popup.cleanup = autoUpdate(
            this,
            this.popup,
            () => {
                computePosition(this, this.popup!, {
                    placement: "top",
                    middleware: [offset(8), flip({padding: 96}), shift({padding: 16})]
                }).then(({x, y}) => {
                    Object.assign(this.popup!.style, {
                        left: `${x}px`,
                        top: `${y}px`,
                    });
                });
            },
        );
    }

    private removePopup() {
        this.popup?.remove();
        this.popup = undefined;
        this.hasPopup = false;
    }

    protected render() {
        if (!this.hasPopup) this.removePopup();
    }
}

const scrim = document.getElementById('popup-scrim')!;
scrim.addEventListener('mousedown', e => {
    if (e.button !== 0) return;
    document
        .querySelectorAll('page-chip, page-popup')
        .forEach(popup => (popup as InterestContainer).hasPopup = false)
});
