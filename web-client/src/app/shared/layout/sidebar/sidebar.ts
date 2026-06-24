import { Component, signal, input, output } from '@angular/core';
import { Category } from '../../../core/models/category';

@Component({
  selector: 'app-sidebar',
  imports: [],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
})
export class Sidebar {

  // TODO: Why readonly???
  readonly categories = input<Category[]>([]);
  readonly activeCategory = input('todas');

  // Emits the category selected
  readonly categorySelected = output<string>();

  protected readonly isOpen = signal(false);

  toggle() : void {
    this.isOpen.update((open) => !open);
  }

  close() : void {
    this.isOpen.set(false);
  }
  
  select(id: string): void {
    this.categorySelected.emit(id);
    this.close();
  }
}
