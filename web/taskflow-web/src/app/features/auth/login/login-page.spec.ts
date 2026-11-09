import { HttpErrorResponse } from '@angular/common/http';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { Observable, Subject, of, throwError } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { control, query, submitButton, typeInto } from '../../../../testing/dom';
import { LoginPage } from './login-page';

@Component({ template: '' })
class Blank {}

describe('LoginPage', () => {
  let login: ReturnType<typeof vi.fn<(request: unknown) => Observable<void>>>;
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    login = vi.fn(() => of(undefined));
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { login } },
        provideRouter([
          { path: 'login', component: LoginPage },
          { path: 'register', component: Blank },
          { path: 'tasks', component: Blank },
        ]),
      ],
    });
    harness = await RouterTestingHarness.create();
  });

  async function open(url = '/login'): Promise<HTMLElement> {
    await harness.navigateByUrl(url, LoginPage);
    return harness.routeNativeElement as HTMLElement;
  }

  async function fillAndSubmit(root: HTMLElement, email: string, password: string): Promise<void> {
    typeInto(control(root, 'email'), email);
    typeInto(control(root, 'password'), password);
    submitButton(root).click();
    await harness.fixture.whenStable();
  }

  const currentUrl = () => TestBed.inject(Router).url;

  it('links to the registration page', async () => {
    const root = await open();

    expect(query(root, 'a[href="/register"]')).toBeTruthy();
  });

  it('signs in with the entered credentials and goes to the tasks page', async () => {
    const root = await open();

    await fillAndSubmit(root, 'ana@example.com', 'Passw0rd1');

    expect(login).toHaveBeenCalledExactlyOnceWith({
      email: 'ana@example.com',
      password: 'Passw0rd1',
    });
    expect(currentUrl()).toBe('/tasks');
  });

  it('returns to the page the user asked for', async () => {
    const root = await open('/login?returnUrl=%2Ftasks%3Fstatus%3DPending');

    await fillAndSubmit(root, 'ana@example.com', 'Passw0rd1');

    expect(currentUrl()).toBe('/tasks?status=Pending');
  });

  it('ignores a return URL that leaves the app', async () => {
    const root = await open('/login?returnUrl=https%3A%2F%2Fevil.example');

    await fillAndSubmit(root, 'ana@example.com', 'Passw0rd1');

    expect(currentUrl()).toBe('/tasks');
  });

  it('does not call the API while the form is invalid, and shows the field errors', async () => {
    const root = await open();

    await fillAndSubmit(root, 'not-an-email', '');

    expect(login).not.toHaveBeenCalled();
    expect(root.querySelectorAll('mat-error').length).toBeGreaterThan(0);
  });

  it('disables the submit button while the request is in flight', async () => {
    const pending = new Subject<void>();
    login.mockReturnValue(pending);
    const root = await open();

    await fillAndSubmit(root, 'ana@example.com', 'Passw0rd1');
    expect(submitButton(root).disabled).toBe(true);

    pending.next();
    pending.complete();
    await harness.fixture.whenStable();
    expect(currentUrl()).toBe('/tasks');
  });

  it('shows the API message when the credentials are rejected and lets the user retry', async () => {
    login.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 401,
            url: '/api/auth/login',
            error: { status: 401, title: 'Unauthorized', detail: 'Invalid email or password.' },
          }),
      ),
    );
    const root = await open();

    await fillAndSubmit(root, 'ana@example.com', 'wrong-pass1');

    expect(query(root, '[role="alert"]').textContent).toContain('Invalid email or password.');
    expect(submitButton(root).disabled).toBe(false);
    expect(currentUrl()).toBe('/login');
  });
});
