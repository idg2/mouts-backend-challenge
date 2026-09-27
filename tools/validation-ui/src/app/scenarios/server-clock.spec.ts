import { describe, expect, it, vi } from 'vitest';
import { ServerClock } from './server-clock';

// Work item: TASK-088 (FEAT-020)
describe('ServerClock', () => {
  it('uses the local clock before any Date header', () => {
    vi.spyOn(Date, 'now').mockReturnValue(1_000_000);
    expect(new ServerClock().now()).toBe(1_000_000);
    vi.restoreAllMocks();
  });

  it('applies the offset between the Date header and the local receive time', () => {
    const clock = new ServerClock();
    const serverTime = Date.parse('Sat, 26 Sep 2026 12:00:10 GMT');
    clock.observe('Sat, 26 Sep 2026 12:00:10 GMT', serverTime - 5_000);
    vi.spyOn(Date, 'now').mockReturnValue(serverTime - 5_000 + 1_500);
    expect(clock.now()).toBe(serverTime + 1_500);
    vi.restoreAllMocks();
  });

  it('ignores a missing or invalid header', () => {
    const clock = new ServerClock();
    clock.observe(null, 0);
    clock.observe('not a date', 0);
    vi.spyOn(Date, 'now').mockReturnValue(42);
    expect(clock.now()).toBe(42);
    vi.restoreAllMocks();
  });
});
