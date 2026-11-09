import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { DEFAULT_RETURN_URL } from './return-url';
import { AuthService } from './auth.service';

/** Lets signed-in users through; sends everyone else to `/login?returnUrl=…`. */
export const authGuard: CanActivateFn = (_route, state) =>
  inject(AuthService).isAuthenticated() ||
  inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });

/** Keeps signed-in users away from the login and register pages. */
export const guestGuard: CanActivateFn = () =>
  !inject(AuthService).isAuthenticated() || inject(Router).parseUrl(DEFAULT_RETURN_URL);
