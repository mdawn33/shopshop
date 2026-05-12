import { computed, Injectable, signal } from '@angular/core';
import { User } from '../models/user';

@Injectable({
  providedIn: 'root',
})
export class Auth {

  
  private readonly user = signal<User | null>(null);
  private readonly _token = signal<string | null>(null); 

  readonly currentUser = this.user.asReadonly();

  readonly isAuthenticated = computed(() => this.currentUser() !== null);

  readonly token = this._token.asReadonly();


  constructor() {
    // temporary user
    const defaultUser: User = { id: '1', email: 'abc@123.com', displayName: '' };

    this.user.set(defaultUser);
  }


  login() : Promise<never> {
    console.warn('Not implemented');
    return Promise.reject(new Error('Not implemented'));
  }

  logout() : Promise<never> {
    console.warn('Not implemented');
    return Promise.reject(new Error('Not implemented'));
  }

  register() : Promise<never> {
    console.warn('Not implemented');
    return Promise.reject(new Error('Not implemented'));
  }
}
