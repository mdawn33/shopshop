import { Component, input, output, signal } from '@angular/core';
import { Category } from '../../core/models/category';

@Component({
  selector: 'app-sidebar',
  imports: [],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
})
export class Sidebar {

  readonly categories = input<Category[]>([]);
  readonly activeCategory = input('All');
  readonly categorySelected = output<string>({});

  protected readonly isOpen = signal(false);

  select(categoryId: string) : void {
    this.categorySelected.emit(categoryId);
    this.close();
  }

  toggle() {
    this.isOpen.update((open) => !open);

  }

  close() {
    this.isOpen.set(false);
  }
}
