import { prepare, saleBody } from '../fixtures';
import { Scenario, outcome } from '../scenario';
import { OUTBOX_FLOW_TAIL, createSale, expectEvents, expectReadModel, expectRelayed } from './outbox-steps';

// Work item: TASK-092 (FEAT-020), TASK-094 (FEAT-020)
export const O1: Scenario = {
  id: 'O1',
  group: 'Outbox and events',
  title: 'Synchronous sale',
  description: 'A sale of 12 units is stored at 20% and records SaleCreated in the outbox in the same transaction; the relay sends it to the queue and the projection writes the sale to the read model.',
  flow: [{ id: 'sale', label: 'Sale stored', keys: ['SAL-CRT-10', 'SAL-CRT-12'] }, ...OUTBOX_FLOW_TAIL],
  async run(ctx) {
    const references = await prepare(ctx);
    const sale = await createSale(ctx, references, [{ productId: references.productId, quantity: 12 }], 'sale');
    ctx.node('sale', 'done', `sale ${sale.saleNumber}`);
    await expectEvents(ctx, sale.id, ['SaleCreated'], 'outbox');
    await expectRelayed(ctx, sale.id, 'relay');
    await expectReadModel(ctx, sale.id, '200 at 20%',
      (r) => r.status === 200 && Number(r.body?.data?.items?.[0]?.discountPercentage) === 20,
      (r) => (r.status === 200 ? `200 at ${r.body.data.items[0].discountPercentage}%` : String(r.status)), 'read');
  },
};

// Work item: TASK-092 (FEAT-020), TASK-094 (FEAT-020)
export const O2: Scenario = {
  id: 'O2',
  group: 'Outbox and events',
  title: 'Asynchronous sale',
  description: 'With Prefer: respond-async the API answers 202 at once; a worker stores the sale from the queue, which records SaleCreated, and the relay and the projection take it to the read model.',
  flow: [
    { id: 'accepted', label: 'API accepted', keys: ['SAL-ASY-03'] },
    { id: 'worker', label: 'Worker stored the sale', keys: ['SAL-ASY-06'] },
    ...OUTBOX_FLOW_TAIL,
  ],
  async run(ctx) {
    const references = await prepare(ctx);
    const saleId = await ctx.step('POST /api/sales · 12 units, Prefer: respond-async', '202', async () => {
      const result = await ctx.api.post('/api/sales', saleBody(references, [{ productId: references.productId, quantity: 12 }]), { Prefer: 'respond-async' });
      return outcome(String(result.status), result.status === 202, result.body?.data?.id as string);
    }, 'accepted');
    await expectEvents(ctx, saleId, ['SaleCreated'], 'worker');
    ctx.node('worker', 'done', 'SaleCreated recorded');
    ctx.node('outbox', 'done', 'SaleCreated');
    await expectRelayed(ctx, saleId, 'relay');
    await expectReadModel(ctx, saleId, '200', (r) => r.status === 200, (r) => String(r.status), 'read');
  },
};
