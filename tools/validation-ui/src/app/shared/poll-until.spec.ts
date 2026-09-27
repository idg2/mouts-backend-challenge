import { describe, expect, it } from 'vitest';
import { PollClock, pollUntil } from './poll-until';

// Work item: TASK-088 (FEAT-020)
function fakeClock(): PollClock {
  let now = 0;
  return { now: () => now, sleep: async (ms) => { now += ms; } };
}

// Work item: TASK-088 (FEAT-020)
describe('pollUntil', () => {
  it('stops at the first satisfying value and counts attempts', async () => {
    let calls = 0;
    const result = await pollUntil(async () => ++calls, (value) => value === 3, { intervalMs: 250, timeoutMs: 10_000 }, fakeClock());
    expect(result).toEqual({ value: 3, attempts: 3, elapsedMs: 500, satisfied: true });
  });

  it('gives up at the timeout with the last value', async () => {
    const result = await pollUntil(async () => 'no', () => false, { intervalMs: 250, timeoutMs: 1_000 }, fakeClock());
    expect(result.satisfied).toBe(false);
    expect(result.value).toBe('no');
    expect(result.attempts).toBe(5);
    expect(result.elapsedMs).toBe(1_000);
  });
});
