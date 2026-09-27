import { createBranch, policyOutcome, prepare } from '../fixtures';
import { Scenario, outcome } from '../scenario';
import { createPolicy, nextValidFrom, sellTwelve, waitForValidFrom } from './policy-steps';

// Work item: TASK-091 (FEAT-020), TASK-094 (FEAT-020)
export const P2: Scenario = {
  id: 'P2',
  group: 'Discount policies',
  title: 'Product and branch beats product',
  description: 'With a product policy (30%) and a product-and-branch policy (25%) in effect, a sale in that branch gets 25% and a sale in another branch gets 30%.',
  flow: [
    { id: 'created', label: 'Two policies created', keys: ['DSC-CRT-04'] },
    { id: 'wait', label: 'Wait for validFrom', keys: ['DSC-CRT-03'] },
    { id: 'saleA', label: 'Sale in branch A', keys: ['SAL-CRT-10'] },
    { id: 'policyA', label: 'Policy', decision: true, keys: ['SAL-CRT-14'] },
    { id: 'saleB', label: 'Sale in branch B', keys: ['SAL-CRT-10'] },
    { id: 'policyB', label: 'Policy', decision: true, keys: ['SAL-CRT-14'] },
  ],
  async run(ctx) {
    const references = await prepare(ctx);
    const otherBranchId = await ctx.step('Create a second branch', '201', async () => outcome('201', true, await createBranch(ctx.api)));
    const validFrom = nextValidFrom(ctx);
    const productPolicy = await createPolicy(ctx, 'product, 30%', { productId: references.productId, branchId: null, percentage: 30 }, validFrom, 'created');
    const branchPolicy = await createPolicy(ctx, 'product and branch A, 25%', { productId: references.productId, branchId: references.branchId, percentage: 25 }, validFrom, 'created');
    ctx.node('created', 'done', 'product 30% · product+A 25%');
    await waitForValidFrom(ctx, validFrom, 'wait');

    const inA = await sellTwelve(ctx, 'in branch A', references, references.branchId, 'saleA');
    await ctx.step('Policy in branch A', 'product and branch', async () => policyOutcome(inA.items[0].discountPolicyId, branchPolicy, 'product and branch'), 'policyA');
    await ctx.step('Discount in branch A', '25%', async () => outcome(`${inA.items[0].discountPercentage}%`, Number(inA.items[0].discountPercentage) === 25, null), 'policyA');
    ctx.node('policyA', 'done', 'product+branch = 25%');

    const inB = await sellTwelve(ctx, 'in branch B', references, otherBranchId, 'saleB');
    await ctx.step('Policy in branch B', 'product', async () => policyOutcome(inB.items[0].discountPolicyId, productPolicy, 'product'), 'policyB');
    await ctx.step('Discount in branch B', '30%', async () => outcome(`${inB.items[0].discountPercentage}%`, Number(inB.items[0].discountPercentage) === 30, null), 'policyB');
    ctx.node('policyB', 'done', 'product = 30%');
  },
};
