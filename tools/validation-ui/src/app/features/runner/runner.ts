import { Injectable, InjectionToken, computed, inject, signal } from '@angular/core';
import { ApiClient } from '../../core/api/api-client';
import { Diagnostics } from '../../core/diagnostics/diagnostics';
import { RunContext } from '../../scenarios/run-context';
import { POLL, pollUntil } from '../../shared/poll-until';
import { RunStatus, Scenario, ScenarioFailure, ScenarioRun, idleRun } from '../../scenarios/scenario';
import { TraceEntry, isRelayNoise } from './trace-filter';

// Work item: TASK-088 (FEAT-020)
/** The scenarios the runner offers, in display order. */
export const SCENARIOS = new InjectionToken<readonly Scenario[]>('SCENARIOS');

// Work item: TASK-088 (FEAT-020), TASK-094 (FEAT-020)
/** Runs scenarios strictly one at a time, so their cursor windows never overlap, and keeps each result. */
@Injectable({ providedIn: 'root' })
export class Runner {
  private readonly api = inject(ApiClient);
  private readonly diagnostics = inject(Diagnostics);

  readonly scenarios = inject(SCENARIOS);
  private readonly runsValue = signal<Record<string, ScenarioRun>>(
    Object.fromEntries(this.scenarios.map((scenario) => [scenario.id, idleRun(scenario)])),
  );
  private readonly busyValue = signal(false);
  private previousOutboxCursor: number | null = null;

  readonly runs = this.runsValue.asReadonly();
  readonly busy = this.busyValue.asReadonly();
  readonly counts = computed(() => {
    const counts: Record<RunStatus, number> = { idle: 0, running: 0, pass: 0, fail: 0 };
    for (const run of Object.values(this.runsValue())) {
      counts[run.status]++;
    }
    return counts;
  });

  async run(id: string): Promise<void> {
    const scenario = this.scenarios.find((candidate) => candidate.id === id);
    if (!scenario || this.busyValue()) {
      return;
    }
    this.busyValue.set(true);
    try {
      await this.execute(scenario);
    } finally {
      this.busyValue.set(false);
    }
  }

  async runAll(): Promise<void> {
    if (this.busyValue()) {
      return;
    }
    this.busyValue.set(true);
    try {
      for (const scenario of this.scenarios) {
        await this.execute(scenario);
      }
    } finally {
      this.busyValue.set(false);
    }
  }

  private async execute(scenario: Scenario): Promise<void> {
    const run: ScenarioRun = { ...idleRun(scenario), status: 'running' };
    const publish = () => this.runsValue.update((runs) => ({ ...runs, [scenario.id]: snapshot(run) }));
    const started = Date.now();
    const traceEnabled = this.diagnostics.traceEnabled();
    let traceCursor: number | null = null;
    publish();

    try {
      await this.drainPreviousEvents();
      const outbox = await this.head('/api/diagnostics/outbox');
      this.previousOutboxCursor = outbox;
      traceCursor = traceEnabled ? await this.head('/api/diagnostics/trace') : 0;
      await scenario.run(new RunContext(this.api, { outbox, trace: traceCursor }, run, publish));
      run.status = 'pass';
    } catch (error) {
      run.status = 'fail';
      if (!(error instanceof ScenarioFailure)) {
        run.steps.push({
          keys: [], label: 'Scenario', expected: 'no error', status: 'fail', exchanges: [],
          actual: `Unexpected error: ${error instanceof Error ? error.message : String(error)}`,
        });
      }
    }

    if (traceEnabled && traceCursor !== null) {
      run.trace = await this.readTrace(traceCursor);
    }
    run.elapsedMs = Date.now() - started;
    publish();
  }

  // Work item: TASK-088 (FEAT-020)
  /**
   * Waits, up to the poll timeout, until the relay has sent every event recorded since the previous scenario started,
   * so its dispatch and projection steps do not land in the next scenario's trace window.
   */
  private async drainPreviousEvents(): Promise<void> {
    const after = this.previousOutboxCursor;
    if (after === null) {
      return;
    }
    await pollUntil(
      async () => {
        const result = await this.api.send('GET', `/api/diagnostics/outbox?after=${after}`);
        return (result.status === 200 ? result.body.data.items : []) as { processedAt: string | null }[];
      },
      (items) => items.every((item) => item.processedAt !== null),
      POLL,
    );
  }

  private async head(url: string): Promise<number> {
    const result = await this.api.send('GET', url);
    if (result.status !== 200) {
      throw new Error(`GET ${url} answered ${result.status}`);
    }
    return result.body.data.head as number;
  }

  private async readTrace(after: number): Promise<TraceEntry[]> {
    try {
      const result = await this.api.send('GET', `/api/diagnostics/trace?after=${after}`);
      const items = (result.status === 200 ? result.body.data.items : []) as TraceEntry[];
      return items.filter((entry) => !isRelayNoise(entry));
    } catch {
      return [];
    }
  }
}

// Work item: TASK-088 (FEAT-020)
function snapshot(run: ScenarioRun): ScenarioRun {
  return {
    ...run,
    nodes: run.nodes.map((node) => ({ ...node })),
    steps: run.steps.map((step) => ({ ...step, exchanges: [...step.exchanges] })),
    trace: [...run.trace],
  };
}
