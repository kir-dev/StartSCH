import {autoUpdate, computePosition, flip, offset, shift, size} from "@floating-ui/dom";
import {css} from "lit";

export class ModalPopup {
    static styles = css`
        :host {
            padding: 8px;
            background-color: var(--md-sys-color-surface-container-high);
            border-radius: 8px;
            box-shadow: var(--md-sys-shadow-2);
            overflow: auto;
        }
    `;
    
    static create<K extends keyof HTMLElementTagNameMap>(
        reference: HTMLElement,
        elementTagName: K,
        options?: (element: HTMLElementTagNameMap[K]) => void
    ) {
        const scrim = document.createElement("div");
        scrim.classList.add("modal-popup-scrim");
        document.body.append(scrim);
        
        const element = document.createElement(elementTagName);
        options?.(element);
        element.classList.add("modal-popup");
        document.body.append(element);
        
        const floating = element;
        const cleanup = autoUpdate(
            reference,
            floating,
            () => {
                computePosition(reference, floating, {
                    placement: "bottom",
                    middleware: [
                        offset(8),
                        flip({padding: 96}),
                        shift({padding: 16}),
                        size({
                            padding: 16,
                            apply({availableWidth, availableHeight}) {
                                Object.assign(floating.style, {
                                    maxWidth: `min(700px, ${Math.max(0, availableWidth)}px)`,
                                    maxHeight: `min(50vh, ${Math.max(0, availableHeight)}px)`,
                                });
                            },
                        })
                    ]
                }).then(({x, y}) => {
                    Object.assign(floating.style, {
                        left: `${x}px`,
                        top: `${y}px`,
                    });
                });
            },
        );
        
        scrim.addEventListener("mousedown", e => {
            if (e.button != 0)
                return;
            
            cleanup();
            scrim.remove();
            element.remove();
        });
    }
}
