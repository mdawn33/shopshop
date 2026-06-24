import { Component, signal, output } from '@angular/core';

@Component({
  selector: 'app-footer',
  imports: [],
  templateUrl: './footer.html',
  styleUrl: './footer.scss',
})
export class Footer {

  /** Emits the typed address when the newsletter form is submitted. */
  readonly subscribe = output<string>();
 
  protected readonly email = signal('');
 
  onSubmit(event: Event): void {
    event.preventDefault();
    const value = this.email().trim();
    if (value) {
      this.subscribe.emit(value);
      this.email.set('');
    }
  }
}
