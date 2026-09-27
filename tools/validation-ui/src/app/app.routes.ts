import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth-guard';
import { LoginPage } from './features/login/login-page';
import { MatrixPage } from './features/matrix/matrix-page';
import { RunnerPage } from './features/runner/runner-page';

// Work item: TASK-089 (FEAT-020), TASK-095 (FEAT-020)
export const routes: Routes = [
  { path: 'login', component: LoginPage },
  { path: '', component: RunnerPage, canActivate: [authGuard] },
  { path: 'matrix', component: MatrixPage, canActivate: [authGuard] },
  { path: '**', redirectTo: '' },
];
