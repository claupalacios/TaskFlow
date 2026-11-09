import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/auth/auth.guards';

// Pages are lazy so the first load carries only the shell, the guards and the auth session.
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tasks' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login-page').then((m) => m.LoginPage),
    canActivate: [guestGuard],
    title: 'Sign in · TaskFlow',
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register-page').then((m) => m.RegisterPage),
    canActivate: [guestGuard],
    title: 'Create account · TaskFlow',
  },
  {
    path: 'tasks',
    loadComponent: () => import('./features/tasks/tasks-page').then((m) => m.TasksPage),
    canActivate: [authGuard],
    title: 'My tasks · TaskFlow',
  },
  { path: '**', redirectTo: 'tasks' },
];
