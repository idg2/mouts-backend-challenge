// Work item: TASK-088 (FEAT-020)
/** One event from GET /api/diagnostics/trace. */
export interface TraceEntry {
  cursor: number;
  at: string;
  threadId: number;
  key: string | null;
  sharedKey: string | null;
  title: string;
  values: { name: string; value: string }[];
  file: string;
  line: number;
}

// Work item: TASK-088 (FEAT-020)
/** First line of the relay's pending-rows query (CMN-PIP-11 `sql`), confirmed on the live API. */
export const PENDING_OUTBOX_QUERY_PREFIX = 'SELECT o."Id", o."OccurredAt", o."Payload", o."ProcessedAt", o."Sequence", o."Type"';

// Work item: TASK-088 (FEAT-020)
const RELAY_LOOP_KEYS = new Set(['SAL-RLY-02', 'SAL-RLY-03', 'SAL-RLY-04', 'SAL-RLY-05', 'SAL-RLY-06']);

// Work item: TASK-088 (FEAT-020)
/** True for the relay's idle cycle steps: the loop itself, an empty pending read, and its SQL. */
export function isRelayNoise(entry: TraceEntry): boolean {
  if (entry.key !== null && RELAY_LOOP_KEYS.has(entry.key)) {
    return true;
  }
  const value = (name: string) => entry.values.find((candidate) => candidate.name === name)?.value;
  if (entry.key === 'SAL-DSP-01') {
    return value('pending') === '0';
  }
  if (entry.key === 'CMN-PIP-11') {
    return value('sql')?.startsWith(PENDING_OUTBOX_QUERY_PREFIX) ?? false;
  }
  return false;
}
