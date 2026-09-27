import { DEFAULT_POLICY_ID, expectCreatedRecorded, policyOutcome, prepare, saleBody } from '../fixtures';
import { Scenario, outcome } from '../scenario';

// Work item: TASK-090 (FEAT-020), TASK-094 (FEAT-020)
export const D7: Scenario = {
  id: 'D7',
  group: 'Discount rules',
  title: 'Same product on two lines (2 + 3) → 10% on both',
  description: 'The tier comes from the total of a product across lines: 2 + 3 = 5 units, so both lines get the 4–9 tier, 10%.',
  flow: [
    { id: 'item', label: 'Items', keys: ['SAL-CRT-01'] },
    { id: 'policy', label: 'Policy', decision: true, keys: ['SAL-CRT-14'] },
    { id: 'max', label: 'Over 20?', decision: true, keys: ['SAL-CRT-15'] },
    { id: 'tier', label: 'Tier', decision: true, keys: ['SAL-CRT-16'] },
    { id: 'stored', label: 'Sale stored', keys: ['SAL-CRT-10', 'SAL-CRT-12'] },
    { id: 'outbox', label: 'Outbox: SaleCreated', keys: ['SAL-CRT-11'] },
  ],
  async run(ctx) {
    const references = await prepare(ctx);
    ctx.node('item', 'done', '2 + 3 units');
    const lines = [{ productId: references.productId, quantity: 2 }, { productId: references.productId, quantity: 3 }];
    const sale = await ctx.step('POST /api/sales · 2 + 3 units', '201', async () => {
      const result = await ctx.api.post('/api/sales', saleBody(references, lines));
      return outcome(String(result.status), result.status === 201, result.body?.data);
    }, 'max');
    ctx.node('max', 'done', 'no: total 5');
    await ctx.step('Discount policy', 'default', async () => policyOutcome(sale.items[0].discountPolicyId, DEFAULT_POLICY_ID, 'default'), 'policy');
    await ctx.step('Both lines discounted', '10%, 10%', async () => {
      const actual = sale.items.map((item: { discountPercentage: number }) => `${item.discountPercentage}%`).join(', ');
      return outcome(actual, actual === '10%, 10%', null);
    }, 'tier');
    ctx.node('tier', 'done', 'total 5: 4–9 = 10%');
    ctx.node('stored', 'done', `sale ${sale.saleNumber}`);
    await expectCreatedRecorded(ctx, sale.id, 'outbox');
  },
};
