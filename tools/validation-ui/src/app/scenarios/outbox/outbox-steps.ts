import { ApiResult, Json } from '../../core/api/api-client';
import { References, SaleLine, saleBody } from '../fixtures';
import { OutboxEntry, eventsForSale, inSequenceOrder, readOutbox, typesOf } from '../outbox-events';
import { FlowNodeDef, ScenarioContext, outcome } from '../scenario';

// Work item: TASK-092 (FEAT-020), TASK-094 (FEAT-020)
/** The last three nodes of every outbox scenario. */
export const OUTBOX_FLOW_TAIL: FlowNodeDef[] = [
  { id: 'outbox', label: 'Outbox recorded', keys: ['SAL-OBW-05'] },
  { id: 'relay', label: 'Relay sent?', decision: true, keys: ['SAL-DSP-04', 'SAL-DSP-05'] },
  { id: 'read', label: 'Read model', keys: ['SAL-PRJ-01'] },
];

// Work item: TASK-092 (FEAT-020)
export function createSale(ctx: ScenarioContext, references: References, lines: SaleLine[], nodeId: string): Promise<Json> {
  const units = lines.map((line) => line.quantity).join(' + ');
  return ctx.step(`POST /api/sales · ${units} units`, '201', async () => {
    const result = await ctx.api.post('/api/sales', saleBody(references, lines));
    return outcome(String(result.status), result.status === 201, result.body?.data);
  }, nodeId);
}

// Work item: TASK-092 (FEAT-020)
/** Polls the outbox until the sale's events after the start cursor are exactly `types`, in sequence order. */
export function expectEvents(ctx: ScenarioContext, saleId: string, types: string[], nodeId: string): Promise<OutboxEntry[]> {
  const expected = types.join(', ');
  return ctx.step('Outbox events for the sale', expected, async () => {
    const poll = await ctx.poll(async () => eventsForSale(await readOutbox(ctx), saleId), (events) => typesOf(events) === expected);
    const events = poll.value;
    const sequences = events.map((event) => `#${event.sequence}`).join(' ');
    return outcome(`${typesOf(events)}${sequences ? ` (${sequences})` : ''}`, poll.satisfied && inSequenceOrder(events), events);
  }, nodeId);
}

// Work item: TASK-092 (FEAT-020)
/** Polls until every event of the sale has processedAt, and reports the relay lag. */
export async function expectRelayed(ctx: ScenarioContext, saleId: string, nodeId: string): Promise<void> {
  await ctx.step('Relay sent every event', 'processedAt set', async () => {
    const poll = await ctx.poll(
      async () => eventsForSale(await readOutbox(ctx), saleId),
      (events) => events.length > 0 && events.every((event) => event.processedAt !== null),
    );
    if (!poll.satisfied) {
      return outcome(`pending after ${(poll.elapsedMs / 1000).toFixed(1)} s`, false, null);
    }
    const lag = Math.max(...poll.value.map((event) => Date.parse(event.processedAt!) - Date.parse(event.occurredAt)));
    return outcome(`yes · +${lag} ms`, true, null);
  }, nodeId);
}

// Work item: TASK-092 (FEAT-020)
/** Polls GET /api/sales/{id} until a predicate on the answer holds; a bare 200 is never enough. */
export async function expectReadModel(
  ctx: ScenarioContext,
  saleId: string,
  expected: string,
  holds: (result: ApiResult) => boolean,
  describe: (result: ApiResult) => string,
  nodeId: string,
): Promise<void> {
  await ctx.step('GET /api/sales/{id} (read model)', expected, async () => {
    const poll = await ctx.poll(() => ctx.api.get(`/api/sales/${saleId}`), holds);
    const seconds = (poll.elapsedMs / 1000).toFixed(1);
    return outcome(`${describe(poll.value)} · ${poll.attempts} attempts, ${seconds} s`, poll.satisfied, null);
  }, nodeId);
  ctx.node(nodeId, 'done', expected);
}
