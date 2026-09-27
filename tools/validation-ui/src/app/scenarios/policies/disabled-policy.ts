import { DEFAULT_POLICY_ID, policyOutcome, prepare } from '../fixtures';
import { Scenario, outcome } from '../scenario';
import { createPolicy, nextValidFrom, sellTwelve, waitForValidFrom } from './policy-steps';

// Work item: TASK-091 (FEAT-020), TASK-094 (FEAT-020)
export const P3: Scenario = {
  id: 'P3',
  group: 'Discount policies',
  title: 'Disabled policy → back to the default',
  description: 'A sale uses the product policy; after that policy is disabled, a new sale of the same product is priced by the default policy again (20% for 12 units).',
  flow: [
    { id: 'created', label: 'Product policy created', keys: ['DSC-CRT-04'] },
    { id: 'wait', label: 'Wait for validFrom', keys: ['DSC-CRT-03'] },
    { id: 'sale1', label: 'First sale', keys: ['SAL-CRT-10'] },
    { id: 'policy1', label: 'Policy', decision: true, keys: ['SAL-CRT-14'] },
    { id: 'disabled', label: 'Policy disabled', keys: ['DSC-DIS-05'] },
    { id: 'sale2', label: 'Second sale', keys: ['SAL-CRT-10'] },
    { id: 'policy2', label: 'Policy', decision: true, keys: ['SAL-CRT-14'] },
  ],
  async run(ctx) {
    const references = await prepare(ctx);
    const validFrom = nextValidFrom(ctx);
    const policyId = await createPolicy(ctx, 'product, 30%', { productId: references.productId, branchId: null, percentage: 30 }, validFrom, 'created');
    await waitForValidFrom(ctx, validFrom, 'wait');

    const first = await sellTwelve(ctx, 'before disabling', references, references.branchId, 'sale1');
    await ctx.step('Policy of the first sale', 'the product policy', async () => policyOutcome(first.items[0].discountPolicyId, policyId, 'the product policy'), 'policy1');

    await ctx.step('POST /api/discount-policies/disable', '200', async () => {
      const result = await ctx.api.post('/api/discount-policies/disable', { ids: [policyId] });
      return outcome(String(result.status), result.status === 200, null);
    }, 'disabled');

    const second = await sellTwelve(ctx, 'after disabling', references, references.branchId, 'sale2');
    await ctx.step('Policy of the second sale', 'default', async () => policyOutcome(second.items[0].discountPolicyId, DEFAULT_POLICY_ID, 'default'), 'policy2');
    await ctx.step('Discount of the second sale', '20%', async () => outcome(`${second.items[0].discountPercentage}%`, Number(second.items[0].discountPercentage) === 20, null), 'policy2');
    ctx.node('policy2', 'done', 'default = 20%');
  },
};
