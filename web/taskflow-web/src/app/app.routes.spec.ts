import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of } from 'rxjs';

import { routes } from './app.routes';
import { AuthService } from './core/auth/auth.service';

describe('app routes', () => {
  let authenticated: boolean;

  // The pages are lazy. Load their modules once up front, so the first navigation in a spec
  // measures routing rather than the cold compile of a page and its Material imports.
  beforeAll(() =>
    Promise.all([
      import('./features/auth/login/login-page'),
      import('./features/auth/register/register-page'),
      import('./features/tasks/tasks-page'),
    ]),
  );

  beforeEach(() => {
    authenticated = false;
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        // The tasks page loads its list; these specs only check where navigation ends.
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: () => authenticated,
            login: () => of(undefined),
            logout: vi.fn(),
          },
        },
      ],
    });
  });

  async function visit(url: string): Promise<{ path: string; returnUrl: string | undefined }> {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url);
    const router = TestBed.inject(Router);
    const tree = router.parseUrl(router.url);
    const path = '/' + (tree.root.children['primary']?.segments.map((s) => s.path).join('/') ?? '');
    return { path, returnUrl: tree.queryParams['returnUrl'] };
  }

  it.each(['/', '/tasks', '/no-such-page'])(
    'sends a signed-out visitor of %s to login, returning to /tasks',
    async (url) => {
      expect(await visit(url)).toEqual({ path: '/login', returnUrl: '/tasks' });
    },
  );

  it.each(['/login', '/register'])('lets a signed-out visitor open %s', async (url) => {
    expect(await visit(url)).toEqual({ path: url, returnUrl: undefined });
  });

  it.each(['/', '/login', '/register'])(
    'sends a signed-in visitor of %s to /tasks',
    async (url) => {
      authenticated = true;

      expect(await visit(url)).toEqual({ path: '/tasks', returnUrl: undefined });
    },
  );
});
