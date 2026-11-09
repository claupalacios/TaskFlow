import { notImplemented } from '../not-implemented';

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
  return notImplemented(error);
}
