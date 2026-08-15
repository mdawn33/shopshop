import { Component, input, output, signal } from '@angular/core';
import { Navlink } from '../../core/models/navlink';

@Component({
  selector: 'app-header',
  imports: [],
  templateUrl: './header.html',
  styleUrl: './header.scss',
})
export class Header {

  /** Address shown in the "Enviar a" utility-bar control. */
  readonly deliveryLocation = input('');
  /** Number badge on the cart icon. */ 
  readonly cartCount = input(0);
  /** Links rendered in the quick-nav strip under the header. */
  readonly navLinks = input<Navlink[]>([]);
 
  /** Emits the trimmed query whenever the search form is submitted. */
  readonly search = output<string>();
 
  protected readonly searchQuery = signal('');
 
  onSubmit(event: Event): void {
    event.preventDefault();
    const query = this.searchQuery().trim();
    if (query) this.search.emit(query);
  }
}
