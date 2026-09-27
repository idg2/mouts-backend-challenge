import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { Session } from './session';

// Work item: TASK-087 (FEAT-020)
/** Adds the bearer token; a 401 on a request that carried one ends the session and returns to the login page. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(Session);
  const router = inject(Router);
  const token = session.token();
  const sent = token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
  return next(sent).pipe(
    catchError((error: unknown) => {
      if (token && error instanceof HttpErrorResponse && error.status === 401) {
        session.clear();
        void router.navigate(['/login']);
      }
      return throwError(() => error);
    }),
  );
};
