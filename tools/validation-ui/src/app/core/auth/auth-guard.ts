import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Session } from './session';

// Work item: TASK-087 (FEAT-020)
/** Sends a visitor without a token to the login page. */
export const authGuard: CanActivateFn = () =>
  inject(Session).token() ? true : inject(Router).createUrlTree(['/login']);
