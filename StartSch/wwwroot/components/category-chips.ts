import {customElement, property} from "lit/decorators.js";
import {html, LitElement} from "lit";

@customElement('category-chips')
export class CategoryChips extends LitElement {
    @property({type: Array}) categories: number[] = [];

    protected render() {
        return html`
            ${this.categories.map(c => html`
                <category-chip category="${c}"/>
            `)}
        `;
    }
}
