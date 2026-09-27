import { ApiResult } from '../core/api/api-client';
import { TraceEntry } from '../features/runner/trace-filter';
import { PollResult } from '../shared/poll-until';

// Work item: TASK-088 (FEAT-020)
export type NodeState = 'pending' | 'active' | 'done' | 'fail';

// Work item: TASK-088 (FEAT-020), TASK-094 (FEAT-020)
/**
 * A node of a scenario's flat flow; a decision node shows the answer the run proved in its detail. `keys` are the
 * documented step keys the node stands for, as in docs/ and the trace.
 */
export interface FlowNodeDef {
  id: string;
  label: string;
  decision?: boolean;
  keys?: string[];
}

// Work item: TASK-088 (FEAT-020)
export interface FlowNode extends FlowNodeDef {
  state: NodeState;
  detail: string;
}

// Work item: TASK-088 (FEAT-020)
export interface HttpExchange {
  method: string;
  url: string;
  requestBody: unknown;
  status: number;
  responseBody: unknown;
  ms: number;
}

// Work item: TASK-088 (FEAT-020)
export type StepStatus = 'running' | 'pass' | 'fail';

// Work item: TASK-088 (FEAT-020), TASK-094 (FEAT-020)
/** One checked step; `keys` name it in docs/: its node's keys, or else the topic keys of its calls. */
export interface StepResult {
  keys: string[];
  label: string;
  expected: string;
  actual: string;
  status: StepStatus;
  exchanges: HttpExchange[];
}

// Work item: TASK-088 (FEAT-020)
/** What a step's action found: the text shown as Actual, whether it matches, and a value for the scenario. */
export interface Outcome<T> {
  actual: string;
  pass: boolean;
  value: T;
}

// Work item: TASK-088 (FEAT-020)
export function outcome<T>(actual: string, pass: boolean, value: T): Outcome<T> {
  return { actual, pass, value };
}

// Work item: TASK-088 (FEAT-020)
/** Ends a scenario at a divergence; the message is shown as the step's Actual. */
export class ScenarioFailure extends Error {}

// Work item: TASK-088 (FEAT-020)
/** The outbox sequence and trace cursor read when the scenario started. */
export interface Cursors {
  outbox: number;
  trace: number;
}

// Work item: TASK-088 (FEAT-020)
/** HTTP calls of a scenario; each call is recorded in the current step. */
export interface ScenarioApi {
  get(url: string): Promise<ApiResult>;
  post(url: string, body: unknown, headers?: Record<string, string>): Promise<ApiResult>;
  put(url: string, body: unknown): Promise<ApiResult>;
  delete(url: string): Promise<ApiResult>;
}

// Work item: TASK-088 (FEAT-020)
export interface ScenarioContext {
  readonly api: ScenarioApi;
  readonly cursors: Cursors;
  /** Runs one checked step; marks `nodeId` active, then done (detail = actual) or failed; throws on a mismatch. */
  step<T>(label: string, expected: string, action: () => Promise<Outcome<T>>, nodeId?: string): Promise<T>;
  node(id: string, state: NodeState, detail?: string): void;
  /** Polls every 250 ms up to 10 s, keeping only the last HTTP exchange in the step. */
  poll<T>(probe: () => Promise<T>, done: (value: T) => boolean): Promise<PollResult<T>>;
  /** The API's clock, from the Date header of the latest response. */
  serverNow(): number;
  sleep(ms: number): Promise<void>;
}

// Work item: TASK-088 (FEAT-020)
export type ScenarioGroup = 'Discount rules' | 'Discount policies' | 'Outbox and events';

// Work item: TASK-088 (FEAT-020)
export interface Scenario {
  id: string;
  group: ScenarioGroup;
  title: string;
  description: string;
  flow: FlowNodeDef[];
  run(ctx: ScenarioContext): Promise<void>;
}

// Work item: TASK-088 (FEAT-020)
export type RunStatus = 'idle' | 'running' | 'pass' | 'fail';

// Work item: TASK-088 (FEAT-020)
export interface ScenarioRun {
  status: RunStatus;
  nodes: FlowNode[];
  steps: StepResult[];
  trace: TraceEntry[];
  elapsedMs: number | null;
}

// Work item: TASK-088 (FEAT-020)
export const STATUS_SYMBOL: Record<RunStatus, string> = { idle: '·', running: '⏳', pass: '✅', fail: '❌' };

// Work item: TASK-088 (FEAT-020)
export function idleRun(scenario: Scenario): ScenarioRun {
  return {
    status: 'idle',
    nodes: scenario.flow.map((node) => ({ ...node, state: 'pending', detail: '' })),
    steps: [],
    trace: [],
    elapsedMs: null,
  };
}
