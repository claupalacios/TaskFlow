import { HttpErrorResponse } from '@angular/common/http';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { Observable, of, throwError } from 'rxjs';

import { UserResponse } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { control, query, submitButton, typeInto } from '../../../../testing/dom';
import { RegisterPage } from './register-page';

@Component({ template: '' })
class Blank {}

const USER: UserResponse = {
  id: 'u-1',
  email: 'new@example.com',
  createdAt: '2026-11-09T12:00:00+00:00',
};

function apiError(status: number, error: unknown): Observable<never> {
  return throwError(() => new HttpErrorResponse({ status, url: '/api/auth/register', error }));
}

describe('RegisterPage', () => {
  let register: ReturnType<typeof vi.fn<(request: unknown) => Observable<UserResponse>>>;
  let login: ReturnType<typeof vi.fn<(request: unknown) => Observable<void>>>;
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    register = vi.fn(() => of(USER));
    login = vi.fn(() => of(undefined));
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { register, login } },
        provideRouter([
          { path: 'register', component: RegisterPage },
          { path: 'login', component: Blank },
          { path: 'tasks', component: Blank },
        ]),
      ],
    });
    harness = await RouterTestingHarness.create();
  });

  async function open(): Promise<HTMLElement> {
    await harness.navigateByUrl('/register', RegisterPage);
    return harness.routeNativeElement as HTMLElement;
  }

  async function fillAndSubmit(root: HTMLElement, email: string, password: string): Promise<void> {
    typeInto(control(root, 'email'), email);
    typeInto(control(root, 'password'), password);
    submitButton(root).click();
    await harness.fixture.whenStable();
  }

  const currentUrl = () => TestBed.inject(Router).url;

  it('links to the login page', async () => {
    const root = await open();

    expect(query(root, 'a[href="/login"]')).toBeTruthy();
  });

  it('creates the account, signs in with the same credentials and opens the tasks page', async () => {
    const root = await open();

    await fillAndSubmit(root, 'new@example.com', 'Passw0rd1');

    const credentials = { email: 'new@example.com', password: 'Passw0rd1' };
    expect(register).toHaveBeenCalledExactlyOnceWith(credentials);
    expect(login).toHaveBeenCalledExactlyOnceWith(credentials);
    expect(currentUrl()).toBe('/tasks');
  });

  it.each([
    ['shorter than 8 characters', 'Pass1'],
    ['without a digit', 'Password'],
    ['without a letter', '12345678'],
    ['longer than 128 characters', `a1${'x'.repeat(127)}`],
  ])('does not call the API for a password %s', async (_rule, password) => {
    const root = await open();

    await fillAndSubmit(root, 'new@example.com', password);

    expect(register).not.toHaveBeenCalled();
    expect(root.querySelectorAll('mat-error').length).toBeGreaterThan(0);
  });

  it('does not call the API for an invalid email', async () => {
    const root = await open();

    await fillAndSubmit(root, 'not-an-email', 'Passw0rd1');

    expect(register).not.toHaveBeenCalled();
  });

  it('shows the API message when the email is already registered', async () => {
    register.mockReturnValue(
      apiError(409, { status: 409, title: 'Conflict', detail: 'Email is already registered.' }),
    );
    const root = await open();

    await fillAndSubmit(root, 'ana@example.com', 'Passw0rd1');

    expect(query(root, '[role="alert"]').textContent).toContain('Email is already registered.');
    expect(login).not.toHaveBeenCalled();
    expect(submitButton(root).disabled).toBe(false);
    expect(currentUrl()).toBe('/register');
  });

  it('shows ValidationProblemDetails field errors under the matching field', async () => {
    register.mockReturnValue(
      apiError(400, {
        status: 400,
        title: 'One or more validation errors occurred.',
        errors: { Email: ['The server rejected this email.'] },
      }),
    );
    const root = await open();

    await fillAndSubmit(root, 'new@example.com', 'Passw0rd1');

    const emailField = control(root, 'email').closest('mat-form-field');
    expect(emailField?.textContent).toContain('The server rejected this email.');
  });
});
