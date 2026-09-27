import { DEFAULT_POLICY_ID, expectCreatedRecorded, policyOutcome, prepare, saleBody } from '../fixtures';
import { FlowNodeDef, Scenario, outcome } from '../scenario';

// Work item: TASK-090 (FEAT-020), TASK-094 (FEAT-020)
const FLOW: FlowNodeDef[] = [
  { id: 'item', label: 'Item', keys: ['SAL-CRT-01'] },
  { id: 'policy', label: 'Policy', decision: true, keys: ['SAL-CRT-14'] },
  { id: 'max', label: 'Over 20?', decision: true, keys: ['SAL-CRT-15'] },
  { id: 'tier', label: 'Tier', decision: true, keys: ['SAL-CRT-16'] },
  { id: 'stored', label: 'Sale stored', keys: ['SAL-CRT-10', 'SAL-CRT-12'] },
  { id: 'outbox', label: 'Outbox: SaleCreated', keys: ['SAL-CRT-11'] },
];

// Work item: TASK-090 (FEAT-020)
function tierScenario(id: string, title: string, quantity: number, percentage: number, tier: string): Scenario {
  return {
    id,
    group: 'Discount rules',
    title,
    description: `A sale of ${quantity} identical units is priced by the default policy: tier ${tier}, ${percentage}% off, and its SaleCreated is recorded in the outbox in the same transaction.`,
    flow: FLOW,
    async run(ctx) {
      const references = await prepare(ctx);
      ctx.node('item', 'done', `${quantity} units`);
      const sale = await ctx.step(`POST /api/sales · ${quantity} units`, '201', async () => {
        const result = await ctx.api.post('/api/sales', saleBody(references, [{ productId: references.productId, quantity }]));
        return outcome(String(result.status), result.status === 201, result.body?.data);
      }, 'max');
      ctx.node('max', 'done', 'no');
      const item = sale.items[0];
      await ctx.step('Discount policy', 'default', async () => policyOutcome(item.discountPolicyId, DEFAULT_POLICY_ID, 'default'), 'policy');
      await ctx.step('Item discount', `${percentage}%`, async () =>
        outcome(`${item.discountPercentage}%`, Number(item.discountPercentage) === percentage, null), 'tier');
      ctx.node('tier', 'done', `${tier} = ${percentage}%`);
      ctx.node('stored', 'done', `sale ${sale.saleNumber}`);
      await expectCreatedRecorded(ctx, sale.id, 'outbox');
    },
  };
}

// Work item: TASK-090 (FEAT-020)
export const D1 = tierScenario('D1', '3 units → no discount', 3, 0, 'below 4');
// Work item: TASK-090 (FEAT-020)
export const D3 = tierScenario('D3', '4 units → 10%', 4, 10, '4–9');
// Work item: TASK-090 (FEAT-020)
export const D4 = tierScenario('D4', '10 units → 20%', 10, 20, '10–20');
// Work item: TASK-090 (FEAT-020)
export const D5 = tierScenario('D5', '20 units → accepted at the max', 20, 20, '10–20');
