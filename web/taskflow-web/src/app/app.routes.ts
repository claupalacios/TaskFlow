import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './core/auth/auth.guards';
import { LoginPage } from './features/auth/login/login-page';
import { RegisterPage } from './features/auth/register/register-page';
import { TasksPage } from './features/tasks/tasks-page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tasks' },
  { path: 'login', component: LoginPage, canActivate: [guestGuard], title: 'Sign in · TaskFlow' },
  {
    path: 'register',
    component: RegisterPage,
    canActivate: [guestGuard],
    title: 'Create account · TaskFlow',
  },
  { path: 'tasks', component: TasksPage, canActivate: [authGuard], title: 'My tasks · TaskFlow' },
  { path: '**', redirectTo: 'tasks' },
];
