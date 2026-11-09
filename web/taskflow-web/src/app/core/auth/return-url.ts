import { notImplemented } from '../not-implemented';

/** Where users land after signing in when there is no usable return URL. */
export const DEFAULT_RETURN_URL = '/tasks';

/**
 * Returns `raw` only when it is a path inside this app that is not an auth page;
 * otherwise `DEFAULT_RETURN_URL`. Prevents open redirects and login loops.
 */
export function safeReturnUrl(raw: string | null | undefined): string {
  return notImplemented(raw);
}
