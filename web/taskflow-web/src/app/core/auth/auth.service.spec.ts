import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationStart, Router, provideRouter } from '@angular/router';
import { filter } from 'rxjs';

import { AuthService, SESSION_STORAGE_KEY } from './auth.service';

@Component({ template: '' })
class Blank {}

const NOW = new Date('2026-11-09T12:00:00Z');
const IN_ONE_HOUR = '2026-11-09T13:00:00+00:00';
const AN_HOUR_AGO = '2026-11-09T11:00:00+00:00';
const CREDENTIALS = { email: 'ana@example.com', password: 'Passw0rd1' };

describe('AuthService', () => {
  let http: HttpTestingController;
  let navigations: string[];

  function setUp(stored?: string): AuthService {
    if (stored !== undefined) {
      localStorage.setItem(SESSION_STORAGE_KEY, stored);
    }
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'login', component: Blank }]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    navigations = [];
    TestBed.inject(Router)
      .events.pipe(filter((e): e is NavigationStart => e instanceof NavigationStart))
      .subscribe((e) => navigations.push(e.url));
    return TestBed.inject(AuthService);
  }

  function signIn(auth: AuthService): void {
    auth.login(CREDENTIALS).subscribe();
    http.expectOne('/api/auth/login').flush({ accessToken: 'token-1', expiresAt: IN_ONE_HOUR });
  }

  const settle = () => TestBed.inject(ApplicationRef).whenStable();

  beforeEach(() => {
    localStorage.clear();
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(NOW);
  });

  afterEach(() => {
    http?.verify();
    vi.useRealTimers();
    localStorage.clear();
  });

  it('starts signed out when nothing is stored', () => {
    const auth = setUp();

    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.accessToken()).toBeNull();
  });

  it('posts the credentials to /api/auth/login and keeps the session', () => {
    const auth = setUp();
    let completed = false;

    auth.login(CREDENTIALS).subscribe({ complete: () => (completed = true) });
    const req = http.expectOne('/api/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(CREDENTIALS);
    req.flush({ accessToken: 'token-1', expiresAt: IN_ONE_HOUR });

    expect(completed).toBe(true);
    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.accessToken()).toBe('token-1');
    expect(JSON.parse(localStorage.getItem(SESSION_STORAGE_KEY) ?? 'null')).toEqual({
      accessToken: 'token-1',
      expiresAt: IN_ONE_HOUR,
    });
  });

  it('passes a failed login through and stays signed out', () => {
    const auth = setUp();
    let status: number | undefined;

    auth
      .login({ ...CREDENTIALS, password: 'wrong' })
      .subscribe({ error: (e) => (status = e.status) });
    http
      .expectOne('/api/auth/login')
      .flush(
        { title: 'Unauthorized', detail: 'Invalid email or password.' },
        { status: 401, statusText: 'Unauthorized' },
      );

    expect(status).toBe(401);
    expect(auth.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it('registers with a POST to /api/auth/register without signing in', () => {
    const auth = setUp();
    let email: string | undefined;

    auth.register(CREDENTIALS).subscribe((user) => (email = user.email));
    const req = http.expectOne('/api/auth/register');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(CREDENTIALS);
    req.flush(
      { id: 'u-1', email: CREDENTIALS.email, createdAt: NOW.toISOString() },
      { status: 201, statusText: 'Created' },
    );

    expect(email).toBe(CREDENTIALS.email);
    expect(auth.isAuthenticated()).toBe(false);
  });

  it('restores an unexpired session from storage', () => {
    const auth = setUp(JSON.stringify({ accessToken: 'stored', expiresAt: IN_ONE_HOUR }));

    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.accessToken()).toBe('stored');
  });

  it('discards an expired stored session', () => {
    const auth = setUp(JSON.stringify({ accessToken: 'stored', expiresAt: AN_HOUR_AGO }));

    expect(auth.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it.each([
    'not json',
    '{"accessToken":42}',
    'null',
    '{"accessToken":"t","expiresAt":"not a date"}',
  ])('discards a malformed stored session: %s', (stored) => {
    const auth = setUp(stored);

    expect(auth.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
  });

  it('stops returning the token once it expires', () => {
    const auth = setUp();
    signIn(auth);

    vi.setSystemTime(new Date('2026-11-09T13:00:01Z'));

    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.accessToken()).toBeNull();
  });

  it('logout clears the session and goes to the login page', async () => {
    const auth = setUp();
    signIn(auth);

    auth.logout();
    await settle();

    expect(auth.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(SESSION_STORAGE_KEY)).toBeNull();
    expect(navigations).toEqual(['/login']);
  });

  it('logout passes the return URL to the login page', async () => {
    const auth = setUp();
    signIn(auth);

    auth.logout({ returnUrl: '/tasks?status=Pending' });
    await settle();

    expect(navigations).toHaveLength(1);
    const url = TestBed.inject(Router).parseUrl(navigations[0]);
    expect(url.root.children['primary']?.segments.map((s) => s.path)).toEqual(['login']);
    expect(url.queryParams).toEqual({ returnUrl: '/tasks?status=Pending' });
  });

  it('logout is idempotent: repeated calls navigate once', async () => {
    const auth = setUp();
    signIn(auth);

    auth.logout({ returnUrl: '/tasks' });
    auth.logout({ returnUrl: '/tasks' });
    await settle();
    auth.logout();
    await settle();

    expect(navigations).toHaveLength(1);
  });
});
