import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { notImplemented } from '../not-implemented';
import { LoginRequest, LogoutOptions, RegisterRequest, UserResponse } from './auth.models';

/** Key under which the session (token and expiry) is kept in `localStorage`. */
export const SESSION_STORAGE_KEY = 'taskflow.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  /** True while a session exists and its token has not expired. */
  isAuthenticated(): boolean {
    return notImplemented();
  }

  /** The bearer token, or null when there is no unexpired session. */
  accessToken(): string | null {
    return notImplemented();
  }

  /** Signs in and stores the session. Errors are passed through unchanged. */
  login(request: LoginRequest): Observable<void> {
    return notImplemented(request);
  }

  /** Creates the account. Does not sign in. */
  register(request: RegisterRequest): Observable<UserResponse> {
    return notImplemented(request);
  }

  /** Clears the session and goes to the login page. Safe to call more than once. */
  logout(options?: LogoutOptions): void {
    notImplemented(options);
  }
}
