import { AbstractControl } from '@angular/forms';

/**
 * Shows server-side field errors on the matching controls as a `server` error.
 * The error goes away on the next edit, when the control's own validators run again.
 */
export function applyFieldErrors(
  form: AbstractControl,
  fieldErrors: Record<string, string[]>,
): void {
  for (const [field, messages] of Object.entries(fieldErrors)) {
    const control = form.get(field);
    if (control && messages.length > 0) {
      control.setErrors({ ...control.errors, server: messages.join(' ') });
      control.markAsTouched();
    }
  }
}
