import { Json } from '../../core/api/api-client';
import { References } from '../fixtures';
import { ScenarioContext, outcome } from '../scenario';

// Work item: TASK-091 (FEAT-020)
/** Two seconds past the API's clock: a policy may not start in the past, and the Date header has 1 s resolution. */
export function nextValidFrom(ctx: ScenarioContext): string {
  return new Date(ctx.serverNow() + 2_000).toISOString();
}

// Work item: TASK-091 (FEAT-020)
/** Creates a policy of at most 50 units with one tier from 12 units at the given percentage. */
export function createPolicy(
  ctx: ScenarioContext,
  label: string,
  spec: { productId: string; branchId: string | null; percentage: number },
  validFrom: string,
  nodeId: string,
): Promise<string> {
  return ctx.step(`POST /api/discount-policies · ${label}`, '201', async () => {
    const result = await ctx.api.post('/api/discount-policies', {
      productId: spec.productId,
      branchId: spec.branchId,
      validFrom,
      maxQuantityPerProduct: 50,
      tiers: [{ minQuantity: 12, maxQuantity: null, percentage: spec.percentage }],
    });
    const actual = `${result.status}${result.body?.error ? ` ${result.body.error}` : ''}`;
    return outcome(actual, result.status === 201, result.body?.data?.id as string);
  }, nodeId);
}

// Work item: TASK-091 (FEAT-020)
/** Waits until the API's clock passes validFrom, then marks the wait node with the time waited. */
export async function waitForValidFrom(ctx: ScenarioContext, validFrom: string, nodeId: string): Promise<void> {
  const waitMs = Math.max(0, Date.parse(validFrom) - ctx.serverNow() + 100);
  ctx.node(nodeId, 'active', `${(waitMs / 1000).toFixed(1)} s`);
  await ctx.sleep(waitMs);
  ctx.node(nodeId, 'done', `${(waitMs / 1000).toFixed(1)} s`);
}

// Work item: TASK-091 (FEAT-020)
/** Sells 12 units of the scenario's product in the given branch and returns the sale. */
export function sellTwelve(ctx: ScenarioContext, label: string, references: References, branchId: string, nodeId: string): Promise<Json> {
  return ctx.step(`POST /api/sales · 12 units ${label}`, '201', async () => {
    const result = await ctx.api.post('/api/sales', {
      customerId: references.customerId, branchId, items: [{ productId: references.productId, quantity: 12 }],
    });
    return outcome(String(result.status), result.status === 201, result.body?.data);
  }, nodeId);
}
