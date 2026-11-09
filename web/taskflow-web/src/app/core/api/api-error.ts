import { HttpErrorResponse } from '@angular/common/http';

/** An API failure reduced to what the UI shows. */
export interface ApiError {
  /** HTTP status; 0 when the server could not be reached or the error was not an HTTP error. */
  status: number;
  /** Message for the whole form. */
  message: string;
  /** Per-field messages from `ValidationProblemDetails.errors`, keyed by camelCase field name. */
  fieldErrors: Record<string, string[]>;
}

export const NETWORK_ERROR_MESSAGE =
  'Cannot reach the server. Check your connection and try again.';
export const GENERIC_ERROR_MESSAGE = 'Something went wrong. Please try again.';

/** Turns an `HttpErrorResponse` carrying RFC 9457 problem details into an `ApiError`. */
export function parseApiError(error: unknown): ApiError {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: 0, message: GENERIC_ERROR_MESSAGE, fieldErrors: {} };
  }
  if (error.status === 0) {
    return { status: 0, message: NETWORK_ERROR_MESSAGE, fieldErrors: {} };
  }

  const body: unknown = error.error;
  if (!isRecord(body)) {
    return { status: error.status, message: GENERIC_ERROR_MESSAGE, fieldErrors: {} };
  }

  return {
    status: error.status,
    message:
      nonEmptyString(body['detail']) ?? nonEmptyString(body['title']) ?? GENERIC_ERROR_MESSAGE,
    fieldErrors: parseFieldErrors(body['errors']),
  };
}

function parseFieldErrors(errors: unknown): Record<string, string[]> {
  const result: Record<string, string[]> = {};
  if (!isRecord(errors)) {
    return result;
  }

  for (const [key, value] of Object.entries(errors)) {
    if (!Array.isArray(value) || !value.every((m) => typeof m === 'string')) {
      continue;
    }
    const field = toFieldName(key);
    result[field] = [...(result[field] ?? []), ...value];
  }
  return result;
}

/** `Email` → `email`, `$.password` → `password` (System.Text.Json reports JSON paths). */
function toFieldName(key: string): string {
  const name = key.startsWith('$.') ? key.slice(2) : key;
  return name.charAt(0).toLowerCase() + name.slice(1);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function nonEmptyString(value: unknown): string | undefined {
  return typeof value === 'string' && value.length > 0 ? value : undefined;
}
