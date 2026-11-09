import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';

import { authGuard, guestGuard } from './auth.guards';
import { AuthService } from './auth.service';

describe('auth guards', () => {
  let authenticated: boolean;

  beforeEach(() => {
    authenticated = false;
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { isAuthenticated: () => authenticated } },
      ],
    });
  });

  function run(guard: typeof authGuard, url: string) {
    const state = { url } as RouterStateSnapshot;
    return TestBed.runInInjectionContext(() => guard({} as ActivatedRouteSnapshot, state));
  }

  function serialize(result: unknown): string {
    expect(result).toBeInstanceOf(UrlTree);
    return TestBed.inject(Router).serializeUrl(result as UrlTree);
  }

  describe('authGuard', () => {
    it('lets a signed-in user through', () => {
      authenticated = true;

      expect(run(authGuard, '/tasks')).toBe(true);
    });

    it('sends a signed-out user to login with the requested page as the return URL', () => {
      const tree = TestBed.inject(Router).parseUrl(
        serialize(run(authGuard, '/tasks?status=Pending')),
      );

      expect(tree.root.children['primary']?.segments.map((s) => s.path)).toEqual(['login']);
      expect(tree.queryParams).toEqual({ returnUrl: '/tasks?status=Pending' });
    });
  });

  describe('guestGuard', () => {
    it('lets a signed-out user open the page', () => {
      expect(run(guestGuard, '/login')).toBe(true);
    });

    it('sends a signed-in user to the tasks page', () => {
      authenticated = true;

      expect(serialize(run(guestGuard, '/login'))).toBe('/tasks');
    });
  });
});
