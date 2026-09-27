import { DEFAULT_POLICY_ID, expectCreatedRecorded, policyOutcome, prepare, saleBody } from '../fixtures';
import { Scenario, outcome } from '../scenario';

// Work item: TASK-090 (FEAT-020), TASK-094 (FEAT-020)
export const D8: Scenario = {
  id: 'D8',
  group: 'Discount rules',
  title: '12 units asking 5% → 5%',
  description: 'A client may ask for less than the tier allows: 12 units have a 20% ceiling, and asking 5% gets exactly 5%.',
  flow: [
    { id: 'item', label: 'Item', keys: ['SAL-CRT-01'] },
    { id: 'policy', label: 'Policy', decision: true, keys: ['SAL-CRT-14'] },
    { id: 'tier', label: 'Ceiling', decision: true, keys: ['SAL-CRT-16'] },
    { id: 'requested', label: 'Requested ≤ ceiling?', decision: true, keys: ['SAL-CRT-15'] },
    { id: 'stored', label: 'Sale stored', keys: ['SAL-CRT-10', 'SAL-CRT-12'] },
    { id: 'outbox', label: 'Outbox: SaleCreated', keys: ['SAL-CRT-11'] },
  ],
  async run(ctx) {
    const references = await prepare(ctx);
    ctx.node('item', 'done', '12 units asking 5%');
    const sale = await ctx.step('POST /api/sales · 12 units asking 5%', '201', async () => {
      const result = await ctx.api.post('/api/sales', saleBody(references, [{ productId: references.productId, quantity: 12, discountPercentage: 5 }]));
      return outcome(String(result.status), result.status === 201, result.body?.data);
    }, 'requested');
    const item = sale.items[0];
    await ctx.step('Discount policy', 'default', async () => policyOutcome(item.discountPolicyId, DEFAULT_POLICY_ID, 'default'), 'policy');
    await ctx.step('Ceiling of the tier', '20%', async () =>
      outcome(`${item.discountCeilingPercentage}%`, Number(item.discountCeilingPercentage) === 20, null), 'tier');
    await ctx.step('Item discount', '5%', async () =>
      outcome(`${item.discountPercentage}%`, Number(item.discountPercentage) === 5, null), 'requested');
    ctx.node('requested', 'done', 'yes: 5% applied');
    ctx.node('stored', 'done', `sale ${sale.saleNumber}`);
    await expectCreatedRecorded(ctx, sale.id, 'outbox');
  },
};
