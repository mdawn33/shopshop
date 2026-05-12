import { computed, Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class Loading {

  private readonly counter = signal(0);
  readonly isLoading = computed(() => this.counter() > 0);

  increment() : void {
    this.counter.update(n => n + 1);
  }

  decrement() : void {
    this.counter.update(n => n - 1);
  }
}
