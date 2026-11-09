import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, map } from 'rxjs';

import {
  AuthResponse,
  LoginRequest,
  LogoutOptions,
  RegisterRequest,
  UserResponse,
} from './auth.models';

/** Key under which the session (token and expiry) is kept in `localStorage`. */
export const SESSION_STORAGE_KEY = 'taskflow.session';

interface Session {
  accessToken: string;
  expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly session = signal<Session | null>(restoreSession());

  /** True while a session exists and its token has not expired. */
  isAuthenticated(): boolean {
    return this.accessToken() !== null;
  }

  /** The bearer token, or null when there is no unexpired session. */
  accessToken(): string | null {
    const session = this.session();
    return session && !isExpired(session) ? session.accessToken : null;
  }

  /** Signs in and stores the session. Errors are passed through unchanged. */
  login(request: LoginRequest): Observable<void> {
    return this.http.post<AuthResponse>('/api/auth/login', request).pipe(
      map(({ accessToken, expiresAt }) => {
        const session: Session = { accessToken, expiresAt };
        writeStorage(session);
        this.session.set(session);
      }),
    );
  }

  /** Creates the account. Does not sign in. */
  register(request: RegisterRequest): Observable<UserResponse> {
    return this.http.post<UserResponse>('/api/auth/register', request);
  }

  /** Clears the session and goes to the login page. Safe to call more than once. */
  logout(options?: LogoutOptions): void {
    if (this.session() === null) {
      return;
    }
    this.session.set(null);
    writeStorage(null);

    const queryParams = options?.returnUrl ? { returnUrl: options.returnUrl } : undefined;
    void this.router.navigate(['/login'], { queryParams });
  }
}

function restoreSession(): Session | null {
  const session = parseSession(readStorage());
  if (!session || isExpired(session)) {
    writeStorage(null);
    return null;
  }
  return session;
}

function parseSession(raw: string | null): Session | null {
  if (raw === null) {
    return null;
  }
  try {
    const value: unknown = JSON.parse(raw);
    if (typeof value !== 'object' || value === null) {
      return null;
    }
    const { accessToken, expiresAt } = value as Record<string, unknown>;
    if (typeof accessToken !== 'string' || typeof expiresAt !== 'string') {
      return null;
    }
    if (Number.isNaN(Date.parse(expiresAt))) {
      return null;
    }
    return { accessToken, expiresAt };
  } catch {
    return null;
  }
}

function isExpired(session: Session): boolean {
  return Date.parse(session.expiresAt) <= Date.now();
}

// Storage can be unavailable (privacy modes, quotas); the app then keeps the session in memory only.
function readStorage(): string | null {
  try {
    return localStorage.getItem(SESSION_STORAGE_KEY);
  } catch {
    return null;
  }
}

function writeStorage(session: Session | null): void {
  try {
    if (session) {
      localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
    } else {
      localStorage.removeItem(SESSION_STORAGE_KEY);
    }
  } catch {
    // Ignored: see readStorage.
  }
}
