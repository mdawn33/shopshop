import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth';
import { errorInterceptor } from './core/interceptors/error';
import { loadingInterceptor } from './core/interceptors/loading';
import { Auth } from './core/services/auth';



// Handling Initial Shell Flicker with provideAppInitializer


export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor, loadingInterceptor])),
    // Modern Angular 19+ Initialization Strategy
    provideAppInitializer(() => {
      // Runs in the injection context, allowing direct service resolution
      const authService = inject(Auth);
      
      // Blocks bootstrap until the session handshake with the ASP.NET BFF resolves
      return authService.checkSession();
    })
  ]
};
