import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { authInterceptor } from './auth-interceptor';
import { Session } from './session';

// Work item: TASK-087 (FEAT-020)
describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let session: Session;
  let router: Router;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    session = TestBed.inject(Session);
    router = TestBed.inject(Router);
  });

  it('adds the bearer token when signed in', async () => {
    session.start('abc', 'admin@example.com');
    const pending = firstValueFrom(http.get('/api/sales'));
    const request = backend.expectOne('/api/sales');
    expect(request.request.headers.get('Authorization')).toBe('Bearer abc');
    request.flush({});
    await pending;
  });

  it('sends no header when signed out', async () => {
    const pending = firstValueFrom(http.get('/api/diagnostics'));
    const request = backend.expectOne('/api/diagnostics');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
    await pending;
  });

  it('clears the session and goes to login on a 401 of a signed-in request', async () => {
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    session.start('expired', 'admin@example.com');
    const pending = firstValueFrom(http.get('/api/sales'));
    backend.expectOne('/api/sales').flush({}, { status: 401, statusText: 'Unauthorized' });

    await expect(pending).rejects.toBeTruthy();
    expect(session.token()).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });
});
