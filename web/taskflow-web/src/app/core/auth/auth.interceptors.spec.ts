import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { authErrorInterceptor, authTokenInterceptor } from './auth.interceptors';
import { AuthService } from './auth.service';

describe('authTokenInterceptor', () => {
  let token: string | null;
  let http: HttpClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    token = null;
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authTokenInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { accessToken: () => token } },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('adds the bearer token to API requests', () => {
    token = 'token-1';

    http.get('/api/tasks').subscribe();

    expect(controller.expectOne('/api/tasks').request.headers.get('Authorization')).toBe(
      'Bearer token-1',
    );
  });

  it('sends no Authorization header when signed out', () => {
    http.get('/api/tasks').subscribe();

    expect(controller.expectOne('/api/tasks').request.headers.has('Authorization')).toBe(false);
  });

  it('never sends the token to another origin', () => {
    token = 'token-1';

    http.get('https://other.example/api/tasks').subscribe();

    expect(
      controller.expectOne('https://other.example/api/tasks').request.headers.has('Authorization'),
    ).toBe(false);
  });
});

describe('authErrorInterceptor', () => {
  let logout: ReturnType<typeof vi.fn>;
  let http: HttpClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    logout = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { logout } },
        { provide: Router, useValue: { url: '/tasks?status=Pending' } },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  function fail(url: string, status: number): number | undefined {
    let received: number | undefined;
    http.get(url).subscribe({ error: (e) => (received = e.status) });
    controller.expectOne(url).flush({ title: 'Error' }, { status, statusText: 'Error' });
    return received;
  }

  it('ends the session on a 401 from the API and keeps the current page as the return URL', () => {
    const status = fail('/api/tasks', 401);

    expect(logout).toHaveBeenCalledExactlyOnceWith({ returnUrl: '/tasks?status=Pending' });
    expect(status).toBe(401);
  });

  it.each(['/api/auth/login', '/api/auth/register'])(
    'leaves a 401 from %s to the form, without logging out',
    (url) => {
      const status = fail(url, 401);

      expect(logout).not.toHaveBeenCalled();
      expect(status).toBe(401);
    },
  );

  it.each([400, 403, 404, 500])('does not log out on a %s', (code) => {
    const status = fail('/api/tasks', code);

    expect(logout).not.toHaveBeenCalled();
    expect(status).toBe(code);
  });
});
