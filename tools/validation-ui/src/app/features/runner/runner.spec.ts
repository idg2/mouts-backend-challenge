import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';
import { ApiClient } from '../../core/api/api-client';
import { Diagnostics } from '../../core/diagnostics/diagnostics';
import { Scenario, outcome } from '../../scenarios/scenario';
import { Runner, SCENARIOS } from './runner';

// Work item: TASK-088 (FEAT-020)
function setup(scenarios: Scenario[], traceEnabled = false) {
  const send = vi.fn(async (_method: string, url: string) => {
    if (url.startsWith('/api/diagnostics/trace?after=')) {
      return { status: 200, body: { data: { head: 9, items: [
        { cursor: 8, key: 'SAL-RLY-02', values: [] }, { cursor: 9, key: 'SAL-CRT-01', values: [] },
      ] } }, date: null, ms: 1 };
    }
    return { status: 200, body: { data: { head: 3, items: [] } }, date: null, ms: 1 };
  });
  TestBed.configureTestingModule({
    providers: [
      { provide: SCENARIOS, useValue: scenarios },
      { provide: ApiClient, useValue: { send } },
      { provide: Diagnostics, useValue: { traceEnabled: signal(traceEnabled), mode: signal('debug') } },
    ],
  });
  return { runner: TestBed.inject(Runner), send };
}

// Work item: TASK-088 (FEAT-020)
function scenario(id: string, run: Scenario['run']): Scenario {
  return { id, group: 'Discount rules', title: id, description: id, flow: [{ id: 'a', label: 'A' }, { id: 'b', label: 'B' }], run };
}

// Work item: TASK-088 (FEAT-020)
describe('Runner', () => {
  it('stops a scenario at its first divergence and leaves later nodes pending', async () => {
    const { runner } = setup([scenario('X1', async (ctx) => {
      await ctx.step('first', '1', async () => outcome('2', false, null), 'a');
      ctx.node('b', 'done');
    })]);

    await runner.run('X1');

    const run = runner.runs()['X1'];
    expect(run.status).toBe('fail');
    expect(run.nodes.map((n) => n.state)).toEqual(['fail', 'pending']);
    expect(runner.busy()).toBe(false);
  });

  it('runs all scenarios one after another', async () => {
    const order: string[] = [];
    const slow = (id: string) => scenario(id, async () => {
      order.push(`${id}:start`);
      await new Promise((resolve) => setTimeout(resolve, 5));
      order.push(`${id}:end`);
    });
    const { runner } = setup([slow('A1'), slow('A2')]);

    await runner.runAll();

    expect(order).toEqual(['A1:start', 'A1:end', 'A2:start', 'A2:end']);
    expect(runner.counts()).toMatchObject({ pass: 2, fail: 0 });
  });

  it('records an error thrown outside a step as a failed step', async () => {
    const { runner } = setup([scenario('E1', async () => { throw new Error('boom'); })]);
    await runner.run('E1');
    expect(runner.runs()['E1'].steps.at(-1)).toMatchObject({ status: 'fail', actual: 'Unexpected error: boom' });
  });

  it('waits for the events of the previous scenario to be processed before starting the next', async () => {
    const order: string[] = [];
    let outboxReads = 0;
    const send = vi.fn(async (_method: string, url: string) => {
      if (url.startsWith('/api/diagnostics/outbox?after=')) {
        outboxReads++;
        order.push(`drain:${outboxReads}`);
        const processedAt = outboxReads >= 2 ? '2026-09-26T12:00:00Z' : null;
        return { status: 200, body: { data: { head: 4, items: [{ sequence: 4, processedAt }] } }, date: null, ms: 1 };
      }
      return { status: 200, body: { data: { head: 3, items: [] } }, date: null, ms: 1 };
    });
    TestBed.configureTestingModule({
      providers: [
        { provide: SCENARIOS, useValue: [scenario('A1', async () => { order.push('A1'); }), scenario('A2', async () => { order.push('A2'); })] },
        { provide: ApiClient, useValue: { send } },
        { provide: Diagnostics, useValue: { traceEnabled: signal(false), mode: signal('debug') } },
      ],
    });
    const runner = TestBed.inject(Runner);

    await runner.runAll();

    expect(order).toEqual(['A1', 'drain:1', 'drain:2', 'A2']);
    expect(send).toHaveBeenCalledWith('GET', '/api/diagnostics/outbox?after=3');
  });

  it('reads the trace after the start cursor and hides relay noise', async () => {
    const { runner, send } = setup([scenario('T1', async () => undefined)], true);
    await runner.run('T1');
    expect(send).toHaveBeenCalledWith('GET', '/api/diagnostics/trace?after=3');
    expect(runner.runs()['T1'].trace.map((e) => e.key)).toEqual(['SAL-CRT-01']);
  });
});
