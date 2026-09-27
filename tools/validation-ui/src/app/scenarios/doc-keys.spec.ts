import { describe, expect, it } from 'vitest';
import { HttpExchange } from './scenario';
import { isTraced, topicKeysOf } from './doc-keys';

// Work item: TASK-094 (FEAT-020)
function call(method: string, url: string): HttpExchange {
  return { method, url, requestBody: null, status: 200, responseBody: null, ms: 1 };
}

// Work item: TASK-094 (FEAT-020)
describe('topicKeysOf', () => {
  it('maps each API call to the topic key of its documented flow, once, in call order', () => {
    const keys = topicKeysOf([
      call('POST', '/api/customers'), call('POST', '/api/branches'), call('POST', '/api/products'),
      call('POST', '/api/sales'), call('PUT', '/api/sales/9f1'), call('GET', '/api/sales/9f1'), call('GET', '/api/sales/9f1'),
      call('DELETE', '/api/sales/9f1'), call('POST', '/api/discount-policies'), call('POST', '/api/discount-policies/disable'),
    ]);
    expect(keys).toEqual(['CUS-CRT', 'BRN-CRT', 'PRD-CRT', 'SAL-CRT', 'SAL-UPD', 'SAL-GET', 'SAL-DEL', 'DSC-CRT', 'DSC-DIS']);
  });

  it('gives diagnostics calls no key', () => {
    expect(topicKeysOf([call('GET', '/api/diagnostics/outbox?after=3')])).toEqual([]);
  });
});

// Work item: TASK-094 (FEAT-020)
describe('isTraced', () => {
  const traced = new Set(['SAL-CRT-15', 'CMN-PIP-01']);

  it('matches a step key exactly and a topic key by any of its steps', () => {
    expect(isTraced('SAL-CRT-15', traced)).toBe(true);
    expect(isTraced('SAL-CRT-16', traced)).toBe(false);
    expect(isTraced('SAL-CRT', traced)).toBe(true);
    expect(isTraced('SAL-UPD', traced)).toBe(false);
  });
});
