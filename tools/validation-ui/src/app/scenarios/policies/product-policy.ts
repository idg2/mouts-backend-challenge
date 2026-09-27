import { policyOutcome, prepare, saleBody } from '../fixtures';
import { Scenario, outcome } from '../scenario';
import { createPolicy, nextValidFrom, waitForValidFrom } from './policy-steps';

// Work item: TASK-091 (FEAT-020), TASK-094 (FEAT-020)
export const P1: Scenario = {
  id: 'P1',
  group: 'Discount policies',
  title: 'Product policy beats the default',
  description: 'A policy scoped to one product (at most 50 units, 30% from 12) prices that product: 30 units, above the default max of 20, are accepted at 30%.',
  flow: [
    { id: 'created', label: 'Product policy created', keys: ['DSC-CRT-04'] },
    { id: 'wait', label: 'Wait for validFrom', keys: ['DSC-CRT-03'] },
    { id: 'item', label: 'Item', keys: ['SAL-CRT-01'] },
    { id: 'max', label: 'Over 50?', decision: true, keys: ['SAL-CRT-15'] },
    { id: 'policy', label: 'Policy', decision: true, keys: ['SAL-CRT-14'] },
    { id: 'tier', label: 'Tier', decision: true, keys: ['SAL-CRT-16'] },
  ],
  async run(ctx) {
    const references = await prepare(ctx);
    const validFrom = nextValidFrom(ctx);
    const policyId = await createPolicy(ctx, 'product, 30% from 12', { productId: references.productId, branchId: null, percentage: 30 }, validFrom, 'created');
    await waitForValidFrom(ctx, validFrom, 'wait');
    ctx.node('item', 'done', '30 units');
    const sale = await ctx.step('POST /api/sales · 30 units', '201', async () => {
      const result = await ctx.api.post('/api/sales', saleBody(references, [{ productId: references.productId, quantity: 30 }]));
      return outcome(`${result.status}${result.body?.error ? ` ${result.body.error}` : ''}`, result.status === 201, result.body?.data);
    }, 'max');
    ctx.node('max', 'done', 'no: max 50');
    const item = sale.items[0];
    await ctx.step('Discount policy', 'the product policy', async () => policyOutcome(item.discountPolicyId, policyId, 'the product policy'), 'policy');
    await ctx.step('Item discount', '30%', async () => outcome(`${item.discountPercentage}%`, Number(item.discountPercentage) === 30, null), 'tier');
    ctx.node('tier', 'done', '12+ = 30%');
  },
};
