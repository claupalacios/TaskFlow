import { CanActivateFn } from '@angular/router';

import { notImplemented } from '../not-implemented';

/** Lets signed-in users through; sends everyone else to `/login?returnUrl=…`. */
export const authGuard: CanActivateFn = (route, state) => notImplemented(route, state);

/** Keeps signed-in users away from the login and register pages. */
export const guestGuard: CanActivateFn = (route, state) => notImplemented(route, state);
