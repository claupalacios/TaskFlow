import { DEFAULT_RETURN_URL, safeReturnUrl } from './return-url';

describe('safeReturnUrl', () => {
  it.each(['/tasks', '/tasks?status=Pending', '/tasks/42'])('keeps the in-app path %s', (url) => {
    expect(safeReturnUrl(url)).toBe(url);
  });

  it.each([null, undefined, ''])('falls back to the default for %s', (url) => {
    expect(safeReturnUrl(url)).toBe(DEFAULT_RETURN_URL);
  });

  it.each([
    'https://evil.example/phish',
    '//evil.example',
    '/\\evil.example',
    'javascript:alert(1)',
    'tasks',
  ])('rejects %s, which is not a path inside the app', (url) => {
    expect(safeReturnUrl(url)).toBe(DEFAULT_RETURN_URL);
  });

  it.each(['/login', '/login?returnUrl=%2Ftasks', '/register'])(
    'rejects the auth page %s so sign-in cannot loop',
    (url) => {
      expect(safeReturnUrl(url)).toBe(DEFAULT_RETURN_URL);
    },
  );
});
