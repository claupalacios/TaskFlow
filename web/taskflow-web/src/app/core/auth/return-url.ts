/** Where users land after signing in when there is no usable return URL. */
export const DEFAULT_RETURN_URL = '/tasks';

const AUTH_PATHS = ['/login', '/register'];

/**
 * Returns `raw` only when it is a path inside this app that is not an auth page;
 * otherwise `DEFAULT_RETURN_URL`. Prevents open redirects and login loops.
 */
export function safeReturnUrl(raw: string | null | undefined): string {
  if (!raw || !raw.startsWith('/') || raw.startsWith('//') || raw.includes('\\')) {
    return DEFAULT_RETURN_URL;
  }

  const path = raw.split(/[?#]/, 1)[0];
  const isAuthPage = AUTH_PATHS.some((p) => path === p || path.startsWith(`${p}/`));
  return isAuthPage ? DEFAULT_RETURN_URL : raw;
}
