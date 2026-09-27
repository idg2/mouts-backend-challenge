import { ApiClient, ApiResult } from '../core/api/api-client';
import { POLL, PollClock, PollResult, pollUntil, realClock } from '../shared/poll-until';
import {
  Cursors, NodeState, Outcome, ScenarioApi, ScenarioContext, ScenarioFailure, ScenarioRun, StepResult,
} from './scenario';
import { topicKeysOf } from './doc-keys';
import { ServerClock } from './server-clock';

// Work item: TASK-088 (FEAT-020), TASK-094 (FEAT-020)
/** Runs one scenario's steps against a mutable run and publishes a snapshot after every change. */
export class RunContext implements ScenarioContext {
  readonly api: ScenarioApi;
  private readonly serverClock = new ServerClock();
  private currentStep: StepResult | null = null;

  constructor(
    private readonly client: ApiClient,
    readonly cursors: Cursors,
    private readonly run: ScenarioRun,
    private readonly publish: () => void,
    private readonly clock: PollClock = realClock,
  ) {
    this.api = {
      get: (url) => this.call('GET', url),
      post: (url, body, headers) => this.call('POST', url, body, headers),
      put: (url, body) => this.call('PUT', url, body),
      delete: (url) => this.call('DELETE', url),
    };
  }

  async step<T>(label: string, expected: string, action: () => Promise<Outcome<T>>, nodeId?: string): Promise<T> {
    const nodeKeys = nodeId ? this.run.nodes.find((node) => node.id === nodeId)?.keys ?? [] : [];
    const step: StepResult = { keys: nodeKeys, label, expected, actual: '', status: 'running', exchanges: [] };
    this.run.steps.push(step);
    this.currentStep = step;
    if (nodeId) {
      this.node(nodeId, 'active');
    }
    this.publish();

    let result: Outcome<T>;
    try {
      result = await action();
    } catch (error) {
      step.actual = error instanceof ScenarioFailure
        ? error.message
        : `Unexpected error: ${error instanceof Error ? error.message : String(error)}`;
      step.status = 'fail';
      step.keys = nodeKeys.length > 0 ? nodeKeys : topicKeysOf(step.exchanges);
      this.currentStep = null;
      if (nodeId) {
        this.node(nodeId, 'fail', step.actual);
      }
      this.publish();
      throw new ScenarioFailure(step.actual);
    }

    step.actual = result.actual;
    step.status = result.pass ? 'pass' : 'fail';
    step.keys = nodeKeys.length > 0 ? nodeKeys : topicKeysOf(step.exchanges);
    this.currentStep = null;
    if (nodeId) {
      this.node(nodeId, result.pass ? 'done' : 'fail', result.pass ? result.actual : `expected ${expected}, got ${result.actual}`);
    }
    this.publish();
    if (!result.pass) {
      throw new ScenarioFailure(`${label}: expected ${expected}, got ${result.actual}`);
    }
    return result.value;
  }

  node(id: string, state: NodeState, detail = ''): void {
    const node = this.run.nodes.find((candidate) => candidate.id === id);
    if (!node) {
      throw new Error(`Unknown flow node ${id}`);
    }
    node.state = state;
    node.detail = detail;
    this.publish();
  }

  async poll<T>(probe: () => Promise<T>, done: (value: T) => boolean): Promise<PollResult<T>> {
    const exchanges = this.currentStep?.exchanges;
    const before = exchanges?.length ?? 0;
    const result = await pollUntil(probe, done, POLL, this.clock);
    if (exchanges && exchanges.length - before > 1) {
      exchanges.splice(before, exchanges.length - before - 1);
    }
    return result;
  }

  serverNow(): number {
    return this.serverClock.now();
  }

  sleep(ms: number): Promise<void> {
    return this.clock.sleep(ms);
  }

  private async call(method: string, url: string, body?: unknown, headers?: Record<string, string>): Promise<ApiResult> {
    const result = await this.client.send(method, url, body, headers);
    this.serverClock.observe(result.date);
    this.currentStep?.exchanges.push({
      method, url, requestBody: body ?? null, status: result.status, responseBody: result.body, ms: result.ms,
    });
    return result;
  }
}
