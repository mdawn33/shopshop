import { computed, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, map, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';


export interface Claim {
  type: string;
  value: string;
}

@Injectable({
  providedIn: 'root',
})
export class Auth {
  
  private userClaims = signal<Claim[] | null>(null);
  readonly currentUser = this.userClaims.asReadonly;

  readonly isAuthenticated = computed(() => this.userClaims() != null);

  constructor(private http: HttpClient) {}
  
  checkSession() {
    return this.http.get<Claim[]>(`${environment.apiGatewayUrl}/bff/user`).pipe(
      tap((claims) => this.userClaims.set(claims)),
      map(() => true),
      catchError(() => {
        this.userClaims.set(null);
        return of(false);
      })
    );

    // this.http.get('/bff/user').subscribe({
    //   next: (res: any) => {
    //     this.isAuthenticated.set(true);
    //     this.userClaims.set(res.claims);
    //   },
    //   error: () => {
    //     this.isAuthenticated.set(false);
    //     this.userClaims.set(null);
    //   }
    // });
  }

  /**
   * Helper method to scan the current user's claims array for a specific permission or role.
   */
  hasClaim(claimType: string, expectedValue: string): boolean {
    const claims = this.userClaims();
    if (!claims) return false;
    
    return claims.some(c => c.type === claimType && c.value === expectedValue);
  }

  login(returnUrl?: string) : void {
    const query = returnUrl ? `?returnUrl=${encodeURIComponent(returnUrl)}` : '';
    window.location.href = `${environment.apiGatewayUrl}/bff/login${query}`;
  }

  logout() : void {
    window.location.href = `${environment.apiGatewayUrl}/bff/logout`;
  }

  register() : void {
    window.location.href = `${environment.apiGatewayUrl}/bff/register`;
  }
}
