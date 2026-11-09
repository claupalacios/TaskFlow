import { HttpErrorResponse } from '@angular/common/http';

import { GENERIC_ERROR_MESSAGE, NETWORK_ERROR_MESSAGE, parseApiError } from './api-error';

function httpError(status: number, error: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, error, url: '/api/tasks' });
}

describe('parseApiError', () => {
  it('uses detail from problem details as the message', () => {
    const result = parseApiError(
      httpError(409, { status: 409, title: 'Conflict', detail: 'Email is already registered.' }),
    );

    expect(result).toEqual({
      status: 409,
      message: 'Email is already registered.',
      fieldErrors: {},
    });
  });

  it('falls back to title when there is no detail', () => {
    const result = parseApiError(httpError(404, { status: 404, title: 'Not found' }));

    expect(result.message).toBe('Not found');
  });

  it('maps ValidationProblemDetails errors to camelCase field names', () => {
    const result = parseApiError(
      httpError(400, {
        status: 400,
        title: 'One or more validation errors occurred.',
        errors: {
          Email: ['The Email field is required.'],
          '$.password': ['The JSON value could not be converted.'],
        },
      }),
    );

    expect(result.status).toBe(400);
    expect(result.message).toBe('One or more validation errors occurred.');
    expect(result.fieldErrors).toEqual({
      email: ['The Email field is required.'],
      password: ['The JSON value could not be converted.'],
    });
  });

  it('ignores error entries that are not lists of strings', () => {
    const result = parseApiError(
      httpError(400, { title: 'Bad request', errors: { email: 'not-a-list', title: [1, 2] } }),
    );

    expect(result.fieldErrors).toEqual({});
  });

  it('reports an unreachable server as a network error', () => {
    const result = parseApiError(
      new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') }),
    );

    expect(result).toEqual({ status: 0, message: NETWORK_ERROR_MESSAGE, fieldErrors: {} });
  });

  it('does not show a body that is not problem details', () => {
    const result = parseApiError(httpError(500, '<html>stack trace</html>'));

    expect(result).toEqual({ status: 500, message: GENERIC_ERROR_MESSAGE, fieldErrors: {} });
  });

  it('handles errors that are not HTTP errors', () => {
    const result = parseApiError(new Error('boom'));

    expect(result).toEqual({ status: 0, message: GENERIC_ERROR_MESSAGE, fieldErrors: {} });
  });
});
