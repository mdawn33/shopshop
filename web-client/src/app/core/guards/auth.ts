import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { Auth } from "../services/auth";
import { map } from "rxjs";


export const authGuard: CanActivateFn = (route, state) => {

    return true;
    
    // const auth = inject(Auth);

    // if (auth.isAuthenticated()) {
    //   console.log("User is authenticated");
    //     return true;
    // }

    // return auth.checkSession().pipe(
    //     map((isLoggedIn) => {
    //       console.log("User is logged in: ", isLoggedIn);
    //         if(isLoggedIn) {
    //             return true;
    //         } else {
    //           console.log("calling login endpoint ...");
    //             auth.login(state.url);
    //             return false;
    //         }
    //     })
    // );


    // return router.createUrlTree(['/auth/login'], {
    //     queryParams: { returnUrl: encodeURIComponent(state.url) }
    // })


}


/**
 * Ensures the authenticated user possesses a designated claim configuration.
 * @param claimType The type key string (e.g., 'role', 'permissions')
 * @param allowedValue The expected content string value (e.g., 'Admin')
 */
export const claimGuard = (claimType: string, allowedValue: string): CanActivateFn => {
  return (route, state) => {
    const authService = inject(Auth);
    const router = inject(Router);

    // Lambda helper executing validation logic against active route
    const checkAccess = () => {
      if (authService.hasClaim(claimType, allowedValue)) {
        return true;
      }
      router.navigate(['/unauthorized']); // Route to safe landing pad
      return false;
    };

    if (authService.isAuthenticated()) {
      return checkAccess();
    }

    return authService.checkSession().pipe(
      map((isLoggedIn) => {
        if (!isLoggedIn) {
          authService.login(state.url);
          return false;
        }
        return checkAccess();
      })
    );
  };
};