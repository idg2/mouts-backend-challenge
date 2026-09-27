import { eventsForCustomer, readOutbox, typesOf } from '../outbox-events';
import { prepare, saleBody } from '../fixtures';
import { Scenario, outcome } from '../scenario';

// Work item: TASK-092 (FEAT-020), TASK-094 (FEAT-020)
export const O7: Scenario = {
  id: 'O7',
  group: 'Outbox and events',
  title: 'Rejected asynchronous sale',
  description: 'An asynchronous sale of 21 units is accepted with 202, but the worker rejects it: no event appears and the read model keeps answering 404. Through the API a queued sale and a rejected one look the same; the trace panel shows the rejection.',
  flow: [
    { id: 'accepted', label: 'API accepted', keys: ['SAL-ASY-03'] },
    { id: 'worker', label: 'Worker stored it?', decision: true, keys: ['SAL-ASY-06', 'SAL-ASY-08'] },
    { id: 'outbox', label: 'Outbox: no event', keys: ['CMN-TXN-07'] },
    { id: 'read', label: 'Read model 404', keys: ['SAL-GET-03'] },
  ],
  async run(ctx) {
    const references = await prepare(ctx);
    const saleId = await ctx.step('POST /api/sales · 21 units, Prefer: respond-async', '202', async () => {
      const result = await ctx.api.post('/api/sales', saleBody(references, [{ productId: references.productId, quantity: 21 }]), { Prefer: 'respond-async' });
      return outcome(String(result.status), result.status === 202, result.body?.data?.id as string);
    }, 'accepted');
    await ctx.step('No event for the customer within 10 s', 'none', async () => {
      const poll = await ctx.poll(async () => eventsForCustomer(await readOutbox(ctx), references.customerId), (events) => events.length > 0);
      return poll.satisfied
        ? outcome(typesOf(poll.value), false, null)
        : outcome('none after 10 s (queued and rejected both answer 404; see the trace for the rejection)', true, null);
    }, 'worker');
    ctx.node('worker', 'done', 'no event in 10 s');
    ctx.node('outbox', 'done', 'none');
    await ctx.step('GET /api/sales/{id} (read model)', '404', async () => {
      const result = await ctx.api.get(`/api/sales/${saleId}`);
      return outcome(String(result.status), result.status === 404, null);
    }, 'read');
  },
};
