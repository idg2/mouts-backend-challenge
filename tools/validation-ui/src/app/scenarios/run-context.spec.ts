import { describe, expect, it, vi } from 'vitest';
import { ApiClient } from '../core/api/api-client';
import { PollClock } from '../shared/poll-until';
import { RunContext } from './run-context';
import { Scenario, ScenarioFailure, idleRun, outcome } from './scenario';

// Work item: TASK-088 (FEAT-020)
const scenario: Scenario = {
  id: 'T1', group: 'Discount rules', title: 't', description: 'd',
  flow: [{ id: 'a', label: 'A' }, { id: 'b', label: 'B', decision: true, keys: ['SAL-CRT-15'] }],
  run: async () => undefined,
};

// Work item: TASK-088 (FEAT-020)
function context(send = vi.fn().mockResolvedValue({ status: 200, body: { data: 1 }, date: null, ms: 3 })) {
  const run = idleRun(scenario);
  const publish = vi.fn();
  let now = 0;
  const clock: PollClock = { now: () => now, sleep: async (ms) => { now += ms; } };
  const ctx = new RunContext({ send } as unknown as ApiClient, { outbox: 5, trace: 7 }, run, publish, clock);
  return { ctx, run, publish, send };
}

// Work item: TASK-088 (FEAT-020)
describe('RunContext', () => {
  it('records a passing step, its exchanges, and marks its node done with the actual value', async () => {
    const { ctx, run, publish } = context();
    const value = await ctx.step('Call', '200', async () => {
      const result = await ctx.api.get('/api/x');
      return outcome(String(result.status), result.status === 200, result.body.data);
    }, 'a');

    expect(value).toBe(1);
    expect(run.steps[0]).toMatchObject({ label: 'Call', expected: '200', actual: '200', status: 'pass' });
    expect(run.steps[0].exchanges).toEqual([
      { method: 'GET', url: '/api/x', requestBody: null, status: 200, responseBody: { data: 1 }, ms: 3 },
    ]);
    expect(run.nodes[0]).toMatchObject({ state: 'done', detail: '200' });
    expect(publish).toHaveBeenCalled();
  });

  it('fails a diverging step, marks its node red, and throws', async () => {
    const { ctx, run } = context();
    await expect(ctx.step('Discount', '20%', async () => outcome('10%', false, null), 'b')).rejects.toBeInstanceOf(ScenarioFailure);
    expect(run.steps[0]).toMatchObject({ actual: '10%', status: 'fail' });
    expect(run.nodes[1]).toMatchObject({ state: 'fail', detail: 'expected 20%, got 10%' });
    expect(run.nodes[0].state).toBe('pending');
  });

  it('turns an unexpected error inside a step into a failed step', async () => {
    const { ctx, run } = context();
    await expect(ctx.step('Boom', 'ok', async () => { throw new TypeError('bad'); })).rejects.toBeInstanceOf(ScenarioFailure);
    expect(run.steps[0]).toMatchObject({ status: 'fail', actual: 'Unexpected error: bad' });
  });

  it('keeps only the last exchange of a poll', async () => {
    const send = vi.fn()
      .mockResolvedValueOnce({ status: 404, body: null, date: null, ms: 1 })
      .mockResolvedValueOnce({ status: 404, body: null, date: null, ms: 1 })
      .mockResolvedValueOnce({ status: 200, body: {}, date: null, ms: 1 });
    const { ctx, run } = context(send);
    await ctx.step('Poll', '200', async () => {
      const result = await ctx.poll(() => ctx.api.get('/api/sales/1'), (r) => r.status === 200);
      return outcome(`${result.value.status} after ${result.attempts} attempts`, result.satisfied, null);
    });
    expect(run.steps[0].exchanges.map((e) => e.status)).toEqual([200]);
    expect(run.steps[0].actual).toBe('200 after 3 attempts');
  });

  it('names a step by its node keys, or else by the topic keys of its calls', async () => {
    const { ctx, run } = context();
    await ctx.step('Rule', 'yes', async () => outcome('yes', true, null), 'b');
    await ctx.step('Create', '201', async () => {
      await ctx.api.post('/api/customers', {});
      await ctx.api.post('/api/sales', {});
      return outcome('201', true, null);
    }, 'a');
    await ctx.step('Read', '200', async () => {
      await ctx.api.get('/api/diagnostics/outbox?after=1');
      return outcome('200', true, null);
    });

    expect(run.steps.map((step) => step.keys)).toEqual([['SAL-CRT-15'], ['CUS-CRT', 'SAL-CRT'], []]);
  });

  it('exposes the start cursors', () => {
    expect(context().ctx.cursors).toEqual({ outbox: 5, trace: 7 });
  });
});
