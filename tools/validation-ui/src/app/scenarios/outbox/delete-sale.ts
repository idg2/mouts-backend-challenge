import { prepare } from '../fixtures';
import { Scenario, outcome } from '../scenario';
import { OUTBOX_FLOW_TAIL, createSale, expectEvents, expectReadModel, expectRelayed } from './outbox-steps';

// Work item: TASK-092 (FEAT-020), TASK-094 (FEAT-020)
export const O6: Scenario = {
  id: 'O6',
  group: 'Outbox and events',
  title: 'Delete a sale',
  description: 'The sale is first seen in the read model; deleting it records SaleDeleted, and the read model then answers 404.',
  flow: [
    { id: 'sale', label: 'Sale stored', keys: ['SAL-CRT-10'] },
    { id: 'projected', label: 'In the read model', keys: ['SAL-PRJ-01'] },
    { id: 'delete', label: 'Sale deleted', keys: ['SAL-DEL-02', 'SAL-DEL-04'] },
    ...OUTBOX_FLOW_TAIL,
  ],
  async run(ctx) {
    const references = await prepare(ctx);
    const sale = await createSale(ctx, references, [{ productId: references.productId, quantity: 12 }], 'sale');
    // A 404 before the create is projected would pass the final check falsely, so the sale must be seen first.
    await expectReadModel(ctx, sale.id, '200', (r) => r.status === 200, (r) => String(r.status), 'projected');
    await ctx.step('DELETE /api/sales/{id}', '200', async () => {
      const result = await ctx.api.delete(`/api/sales/${sale.id}`);
      return outcome(String(result.status), result.status === 200, null);
    }, 'delete');
    await expectEvents(ctx, sale.id, ['SaleCreated', 'SaleDeleted'], 'outbox');
    await expectRelayed(ctx, sale.id, 'relay');
    await expectReadModel(ctx, sale.id, '404', (r) => r.status === 404, (r) => String(r.status), 'read');
  },
};
