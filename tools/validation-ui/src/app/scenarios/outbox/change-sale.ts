import { Json } from '../../core/api/api-client';
import { References, itemById, prepare } from '../fixtures';
import { Scenario, ScenarioContext, outcome } from '../scenario';
import { OUTBOX_FLOW_TAIL, createSale, expectEvents, expectReadModel, expectRelayed } from './outbox-steps';

// Work item: TASK-092 (FEAT-020)
function put(ctx: ScenarioContext, label: string, saleId: string, references: References, body: object, check: (sale: Json) => boolean, describe: (sale: Json) => string, nodeId: string): Promise<Json> {
  return ctx.step(`PUT /api/sales/{id} · ${label}`, '200', async () => {
    const result = await ctx.api.put(`/api/sales/${saleId}`, { customerId: references.customerId, branchId: references.branchId, ...body });
    const sale = result.body?.data;
    return outcome(result.status === 200 ? `200 · ${describe(sale)}` : String(result.status), result.status === 200 && check(sale), sale);
  }, nodeId);
}

// Work item: TASK-092 (FEAT-020), TASK-094 (FEAT-020)
export const O3: Scenario = {
  id: 'O3',
  group: 'Outbox and events',
  title: 'Update a sale',
  description: 'A sale of 12 units (20%) is changed to 5 units: the update records SaleModified, and the read model ends with 5 units at 10%.',
  flow: [
    { id: 'sale', label: 'Sale stored', keys: ['SAL-CRT-10'] },
    { id: 'update', label: 'Changed to 5 units', keys: ['SAL-UPD-11'] },
    ...OUTBOX_FLOW_TAIL,
  ],
  async run(ctx) {
    const references = await prepare(ctx);
    const sale = await createSale(ctx, references, [{ productId: references.productId, quantity: 12 }], 'sale');
    const item = sale.items[0];
    await put(ctx, '5 units', sale.id, references,
      { isCancelled: false, items: [{ id: item.id, productId: references.productId, quantity: 5 }] },
      (updated) => Number(updated.items[0].discountPercentage) === 10,
      (updated) => `${updated.items[0].quantity} units at ${updated.items[0].discountPercentage}%`, 'update');
    await expectEvents(ctx, sale.id, ['SaleCreated', 'SaleModified'], 'outbox');
    await expectRelayed(ctx, sale.id, 'relay');
    await expectReadModel(ctx, sale.id, '5 units at 10%',
      (r) => r.status === 200 && r.body.data.items[0].quantity === 5 && Number(r.body.data.items[0].discountPercentage) === 10,
      (r) => (r.status === 200 ? `${r.body.data.items[0].quantity} units at ${r.body.data.items[0].discountPercentage}%` : String(r.status)), 'read');
  },
};

// Work item: TASK-092 (FEAT-020), TASK-094 (FEAT-020)
export const O4: Scenario = {
  id: 'O4',
  group: 'Outbox and events',
  title: 'Cancel an item',
  description: 'Lines of 2 and 3 units share the 4–9 tier (10%). Cancelling the 3-unit line records SaleModified then ItemCancelled, and reprices the other line to 0%.',
  flow: [
    { id: 'sale', label: 'Sale stored', keys: ['SAL-CRT-10'] },
    { id: 'cancel', label: 'Cancel the 3-unit line', keys: ['SAL-UPD-10', 'SAL-UPD-13'] },
    { id: 'reprice', label: 'Other line tier', decision: true, keys: ['SAL-UPD-20'] },
    ...OUTBOX_FLOW_TAIL,
  ],
  async run(ctx) {
    const references = await prepare(ctx);
    const lines = [{ productId: references.productId, quantity: 2 }, { productId: references.productId, quantity: 3 }];
    const sale = await createSale(ctx, references, lines, 'sale');
    const [kept, cancelled] = sale.items;
    const updated = await put(ctx, 'cancel the 3-unit line', sale.id, references,
      {
        isCancelled: false,
        items: [
          { id: kept.id, productId: references.productId, quantity: 2 },
          { id: cancelled.id, productId: references.productId, quantity: 3, isCancelled: true },
        ],
      },
      (result) => itemById(result, cancelled.id)?.isCancelled === true,
      (result) => `line cancelled: ${itemById(result, cancelled.id)?.isCancelled}`, 'cancel');
    await ctx.step('Discount of the other line', '0%', async () => {
      const percentage = itemById(updated, kept.id)?.discountPercentage;
      return outcome(`${percentage}%`, Number(percentage) === 0, null);
    }, 'reprice');
    ctx.node('reprice', 'done', 'below 4 = 0%');
    await expectEvents(ctx, sale.id, ['SaleCreated', 'SaleModified', 'ItemCancelled'], 'outbox');
    await expectRelayed(ctx, sale.id, 'relay');
    await expectReadModel(ctx, sale.id, 'line cancelled, other at 0%',
      (r) => r.status === 200 && itemById(r.body.data, cancelled.id)?.isCancelled === true
        && Number(itemById(r.body.data, kept.id)?.discountPercentage) === 0,
      (r) => (r.status === 200 ? `cancelled ${itemById(r.body.data, cancelled.id)?.isCancelled}, other at ${itemById(r.body.data, kept.id)?.discountPercentage}%` : String(r.status)),
      'read');
  },
};

// Work item: TASK-092 (FEAT-020), TASK-094 (FEAT-020)
export const O5: Scenario = {
  id: 'O5',
  group: 'Outbox and events',
  title: 'Cancel a sale',
  description: 'Cancelling the whole sale records SaleModified then SaleCancelled, without an ItemCancelled per item, and the read model shows the sale cancelled.',
  flow: [
    { id: 'sale', label: 'Sale stored', keys: ['SAL-CRT-10'] },
    { id: 'cancel', label: 'Sale cancelled', keys: ['SAL-UPD-14', 'SAL-UPD-15'] },
    ...OUTBOX_FLOW_TAIL,
  ],
  async run(ctx) {
    const references = await prepare(ctx);
    const sale = await createSale(ctx, references, [{ productId: references.productId, quantity: 12 }], 'sale');
    await put(ctx, 'isCancelled: true', sale.id, references,
      { isCancelled: true, items: [{ id: sale.items[0].id, productId: references.productId, quantity: 12 }] },
      (updated) => updated.isCancelled === true,
      (updated) => `isCancelled ${updated.isCancelled}`, 'cancel');
    await expectEvents(ctx, sale.id, ['SaleCreated', 'SaleModified', 'SaleCancelled'], 'outbox');
    await expectRelayed(ctx, sale.id, 'relay');
    await expectReadModel(ctx, sale.id, 'isCancelled true',
      (r) => r.status === 200 && r.body.data.isCancelled === true,
      (r) => (r.status === 200 ? `isCancelled ${r.body.data.isCancelled}` : String(r.status)), 'read');
  },
};
