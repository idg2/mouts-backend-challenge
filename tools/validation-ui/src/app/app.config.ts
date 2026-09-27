import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth-interceptor';
import { AppConfigStore } from './core/config/app-config';
import { SCENARIOS } from './features/runner/runner';
import { CATALOG } from './scenarios/catalog';

// Work item: TASK-089 (FEAT-020)
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAppInitializer(() => inject(AppConfigStore).load()),
    { provide: SCENARIOS, useValue: CATALOG },
  ],
};
