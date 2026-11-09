import { HttpInterceptorFn } from '@angular/common/http';

import { notImplemented } from '../not-implemented';

/** Adds `Authorization: Bearer <token>` to requests for this app's own API. */
export const authTokenInterceptor: HttpInterceptorFn = (req, next) => notImplemented(req, next);

/** Ends the session when the API answers 401, except for the login and register calls. */
export const authErrorInterceptor: HttpInterceptorFn = (req, next) => notImplemented(req, next);
