// Work item: TASK-088 (FEAT-020)
/** Time source of a poll; tests pass a fake one. */
export interface PollClock {
  now(): number;
  sleep(ms: number): Promise<void>;
}

// Work item: TASK-088 (FEAT-020)
export const realClock: PollClock = {
  now: () => Date.now(),
  sleep: (ms) => new Promise((resolve) => setTimeout(resolve, ms)),
};

// Work item: TASK-088 (FEAT-020)
export interface PollOptions {
  intervalMs: number;
  timeoutMs: number;
}

// Work item: TASK-088 (FEAT-020)
/** Every scenario poll: every 250 ms, up to 10 s. A test expectation, not environment configuration. */
export const POLL: PollOptions = { intervalMs: 250, timeoutMs: 10_000 };

// Work item: TASK-088 (FEAT-020)
export interface PollResult<T> {
  value: T;
  attempts: number;
  elapsedMs: number;
  satisfied: boolean;
}

// Work item: TASK-088 (FEAT-020)
/** Calls `probe` until `done` holds or the timeout would pass; never throws on a timeout. */
export async function pollUntil<T>(
  probe: () => Promise<T>,
  done: (value: T) => boolean,
  options: PollOptions,
  clock: PollClock = realClock,
): Promise<PollResult<T>> {
  const started = clock.now();
  let attempts = 0;
  for (;;) {
    attempts++;
    const value = await probe();
    const elapsedMs = clock.now() - started;
    if (done(value)) {
      return { value, attempts, elapsedMs, satisfied: true };
    }
    if (elapsedMs + options.intervalMs > options.timeoutMs) {
      return { value, attempts, elapsedMs, satisfied: false };
    }
    await clock.sleep(options.intervalMs);
  }
}
