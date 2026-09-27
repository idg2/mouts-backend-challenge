import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';
import { ApiClient } from '../api/api-client';
import { Diagnostics } from './diagnostics';

// Work item: TASK-087 (FEAT-020)
describe('Diagnostics.probe', () => {
  function setup(send: ApiClient['send']): Diagnostics {
    TestBed.configureTestingModule({ providers: [{ provide: ApiClient, useValue: { send } }] });
    return TestBed.inject(Diagnostics);
  }

  it('reads a 200 as Debug with the trace setting', async () => {
    const diagnostics = setup(vi.fn().mockResolvedValue({ status: 200, body: { data: { traceEnabled: true } }, date: null, ms: 1 }));
    await diagnostics.probe();
    expect(diagnostics.mode()).toBe('debug');
    expect(diagnostics.traceEnabled()).toBe(true);
  });

  it('reads a 404 as Release', async () => {
    const diagnostics = setup(vi.fn().mockResolvedValue({ status: 404, body: null, date: null, ms: 1 }));
    await diagnostics.probe();
    expect(diagnostics.mode()).toBe('release');
  });

  it.each([502, 500, 504])('reads %i as unreachable', async (status) => {
    const diagnostics = setup(vi.fn().mockResolvedValue({ status, body: null, date: null, ms: 1 }));
    await diagnostics.probe();
    expect(diagnostics.mode()).toBe('unreachable');
  });

  it('reads a network failure as unreachable', async () => {
    const diagnostics = setup(vi.fn().mockRejectedValue(new Error('offline')));
    await diagnostics.probe();
    expect(diagnostics.mode()).toBe('unreachable');
  });
});
