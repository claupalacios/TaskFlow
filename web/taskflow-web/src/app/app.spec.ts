import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { AuthService } from './core/auth/auth.service';

describe('App', () => {
  let authenticated: boolean;
  let logout: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    authenticated = false;
    logout = vi.fn();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { isAuthenticated: () => authenticated, logout } },
      ],
    }).compileComponents();
  });

  async function render(): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function logoutButton(root: HTMLElement): HTMLButtonElement | undefined {
    return Array.from(root.querySelectorAll<HTMLButtonElement>('mat-toolbar button')).find((b) =>
      b.textContent?.includes('Log out'),
    );
  }

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance).toBeTruthy();
  });

  it('shows the app name in the toolbar', async () => {
    const root = await render();

    expect(root.querySelector('mat-toolbar')?.textContent).toContain('TaskFlow');
  });

  it('has a main region that hosts the routed pages', async () => {
    const root = await render();

    expect(root.querySelector('main')?.querySelector('router-outlet')).not.toBeNull();
  });

  it('hides the log out button when signed out', async () => {
    const root = await render();

    expect(logoutButton(root)).toBeUndefined();
  });

  it('logs out from the toolbar when signed in', async () => {
    authenticated = true;
    const root = await render();

    logoutButton(root)?.click();

    expect(logout).toHaveBeenCalledExactlyOnceWith();
  });
});
