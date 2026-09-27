import { describe, expect, it } from 'vitest';
import { PENDING_OUTBOX_QUERY_PREFIX, TraceEntry, isRelayNoise } from './trace-filter';

// Work item: TASK-088 (FEAT-020)
function entry(key: string, values: Record<string, string> = {}): TraceEntry {
  return {
    cursor: 1, at: '', threadId: 1, key, sharedKey: null, title: '',
    values: Object.entries(values).map(([name, value]) => ({ name, value })), file: 'X.cs', line: 1,
  };
}

// Work item: TASK-088 (FEAT-020)
describe('isRelayNoise', () => {
  it.each(['SAL-RLY-02', 'SAL-RLY-03', 'SAL-RLY-04', 'SAL-RLY-05', 'SAL-RLY-06'])('hides the loop step %s', (key) => {
    expect(isRelayNoise(entry(key))).toBe(true);
  });

  it('keeps a relay failure', () => {
    expect(isRelayNoise(entry('SAL-RLY-07'))).toBe(false);
  });

  it('hides an empty dispatch read and keeps one that found rows', () => {
    expect(isRelayNoise(entry('SAL-DSP-01', { pending: '0' }))).toBe(true);
    expect(isRelayNoise(entry('SAL-DSP-01', { pending: '1' }))).toBe(false);
  });

  it('hides the pending-rows SQL and keeps other SQL', () => {
    expect(isRelayNoise(entry('CMN-PIP-11', { sql: `${PENDING_OUTBOX_QUERY_PREFIX}` }))).toBe(true);
    expect(isRelayNoise(entry('CMN-PIP-11', { sql: 'INSERT INTO "Sales" ("Id")' }))).toBe(false);
  });

  it('keeps business steps', () => {
    expect(isRelayNoise(entry('SAL-CRT-01'))).toBe(false);
  });
});
