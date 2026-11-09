import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';

import { parseApiError } from '../../../core/api/api-error';
import { applyFieldErrors } from '../../../core/api/form-errors';
import { AuthService } from '../../../core/auth/auth.service';
import { DEFAULT_RETURN_URL } from '../../../core/auth/return-url';

/** Mirrors the API's PasswordPolicy and the User email limit. */
export const PASSWORD_MIN_LENGTH = 8;
export const PASSWORD_MAX_LENGTH = 128;
export const EMAIL_MAX_LENGTH = 256;

/** At least one letter and one digit, in any script (like `char.IsLetter` / `char.IsDigit`). */
function letterAndDigit(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value;
  if (!value) {
    return null;
  }
  return /\p{L}/u.test(value) && /\p{Nd}/u.test(value) ? null : { letterAndDigit: true };
}

@Component({
  selector: 'app-register-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  templateUrl: './register-page.html',
  styleUrl: '../auth-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly passwordMinLength = PASSWORD_MIN_LENGTH;
  protected readonly passwordMaxLength = PASSWORD_MAX_LENGTH;
  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(EMAIL_MAX_LENGTH)]],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(PASSWORD_MIN_LENGTH),
        Validators.maxLength(PASSWORD_MAX_LENGTH),
        letterAndDigit,
      ],
    ],
  });
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.submitting()) {
      return;
    }

    const credentials = this.form.getRawValue();
    this.submitting.set(true);
    this.error.set(null);
    this.auth
      .register(credentials)
      .pipe(switchMap(() => this.auth.login(credentials)))
      .subscribe({
        complete: () => void this.router.navigateByUrl(DEFAULT_RETURN_URL),
        error: (e: unknown) => {
          const apiError = parseApiError(e);
          applyFieldErrors(this.form, apiError.fieldErrors);
          this.error.set(apiError.message);
          this.submitting.set(false);
        },
      });
  }
}
