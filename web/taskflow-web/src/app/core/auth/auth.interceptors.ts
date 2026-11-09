import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { AuthService } from './auth.service';

const API_PREFIX = '/api/';
const CREDENTIAL_ENDPOINTS = ['/api/auth/login', '/api/auth/register'];

/** Relative URLs only: the token never leaves this app's origin. */
const isApiRequest = (url: string) => url.startsWith(API_PREFIX);

/** Adds `Authorization: Bearer <token>` to requests for this app's own API. */
export const authTokenInterceptor: HttpInterceptorFn = (req, next) => {
  const token = isApiRequest(req.url) ? inject(AuthService).accessToken() : null;
  if (!token) {
    return next(req);
  }
  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};

/** Ends the session when the API answers 401, except for the login and register calls. */
export const authErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        isApiRequest(req.url) &&
        !CREDENTIAL_ENDPOINTS.includes(req.url)
      ) {
        auth.logout({ returnUrl: router.url });
      }
      return throwError(() => error);
    }),
  );
};
