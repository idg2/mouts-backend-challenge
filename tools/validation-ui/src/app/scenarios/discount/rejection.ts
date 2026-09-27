import { expectNoEventForCustomer, prepare, saleBody } from '../fixtures';
import { FlowNodeDef, Scenario, outcome } from '../scenario';

// Work item: TASK-090 (FEAT-020)
interface Rejection {
  id: string;
  title: string;
  description: string;
  quantity: number;
  discountPercentage?: number;
  decision: FlowNodeDef;
  answer: string;
  error: string;
}

// Work item: TASK-090 (FEAT-020), TASK-094 (FEAT-020)
function rejectionScenario(rejection: Rejection): Scenario {
  return {
    id: rejection.id,
    group: 'Discount rules',
    title: rejection.title,
    description: rejection.description,
    flow: [
      { id: 'item', label: 'Item', keys: ['SAL-CRT-01'] },
      rejection.decision,
      { id: 'rejected', label: 'Rejected', keys: ['CMN-RSP-06'] },
      { id: 'outbox', label: 'Outbox: no event', keys: ['CMN-TXN-07'] },
    ],
    async run(ctx) {
      const references = await prepare(ctx);
      const asked = rejection.discountPercentage === undefined ? '' : ` asking ${rejection.discountPercentage}%`;
      ctx.node('item', 'done', `${rejection.quantity} units${asked}`);
      const line = { productId: references.productId, quantity: rejection.quantity, discountPercentage: rejection.discountPercentage };
      await ctx.step(`POST /api/sales · ${rejection.quantity} units${asked}`, `400 ${rejection.error}`, async () => {
        const result = await ctx.api.post('/api/sales', saleBody(references, [line]));
        const actual = `${result.status} ${result.body?.error ?? ''}`.trim();
        return outcome(actual, result.status === 400 && result.body?.error === rejection.error, null);
      }, rejection.decision.id);
      ctx.node(rejection.decision.id, 'done', rejection.answer);
      ctx.node('rejected', 'done', `400 ${rejection.error}`);
      await expectNoEventForCustomer(ctx, references.customerId, 'outbox');
    },
  };
}

// Work item: TASK-090 (FEAT-020), TASK-094 (FEAT-020)
export const D2 = rejectionScenario({
  id: 'D2',
  title: '3 units asking 10% → rejected',
  description: 'Below 4 units the ceiling is 0%, so asking for 10% is refused with DiscountAboveAllowed and the rolled-back write leaves no event.',
  quantity: 3,
  discountPercentage: 10,
  decision: { id: 'tier', label: 'Above the ceiling?', decision: true, keys: ['SAL-CRT-15'] },
  answer: 'yes: ceiling 0%',
  error: 'DiscountAboveAllowed',
});

// Work item: TASK-090 (FEAT-020), TASK-094 (FEAT-020)
export const D6 = rejectionScenario({
  id: 'D6',
  title: '21 units → rejected, no event',
  description: 'More than 20 identical units are refused with QuantityLimitExceeded, and the rolled-back write leaves no event in the outbox.',
  quantity: 21,
  decision: { id: 'max', label: 'Over 20?', decision: true, keys: ['SAL-CRT-15'] },
  answer: 'yes',
  error: 'QuantityLimitExceeded',
});
