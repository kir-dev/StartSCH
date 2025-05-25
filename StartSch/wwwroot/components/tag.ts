import {LitElement, css, html} from 'lit';
import {customElement, property} from 'lit/decorators.js';

import Alpine from 'alpinejs'

Alpine.start();

@customElement('category-tag')
export class SimpleGreeting extends LitElement {
    // Define scoped styles right with your component, in plain CSS
    static styles = css`
        :host {
            color: blue;
        }
    `;

    // Declare reactive properties
    @property()
    accessor name: string = 'World';

    // Render the UI as a function of component state
    render() {
        return html`
            <p>Hello, ${this.name}!</p>
        `;
    }
}
