import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { ApiClient } from './api-client';

// Work item: TASK-087 (FEAT-020)
describe('ApiClient', () => {
  let client: ApiClient;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    client = TestBed.inject(ApiClient);
    http = TestBed.inject(HttpTestingController);
  });

  it('resolves a success with status, body, and Date header', async () => {
    const pending = client.send('POST', '/api/sales', { a: 1 }, { Prefer: 'respond-async' });
    const request = http.expectOne('/api/sales');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ a: 1 });
    expect(request.request.headers.get('Prefer')).toBe('respond-async');
    request.flush({ data: { id: 'x' } }, { status: 202, statusText: 'Accepted', headers: { Date: 'Sat, 26 Sep 2026 12:00:00 GMT' } });

    const result = await pending;
    expect(result.status).toBe(202);
    expect(result.body).toEqual({ data: { id: 'x' } });
    expect(result.date).toBe('Sat, 26 Sep 2026 12:00:00 GMT');
  });

  it('resolves an HTTP error with its status and body instead of rejecting', async () => {
    const pending = client.send('POST', '/api/sales', {});
    http.expectOne('/api/sales').flush({ type: 'ValidationError', error: 'QuantityLimitExceeded' }, { status: 400, statusText: 'Bad Request' });

    const result = await pending;
    expect(result.status).toBe(400);
    expect(result.body.error).toBe('QuantityLimitExceeded');
  });

  it('rejects a network failure', async () => {
    const pending = client.send('GET', '/api/diagnostics');
    http.expectOne('/api/diagnostics').error(new ProgressEvent('error'));
    await expect(pending).rejects.toBeTruthy();
  });
});
