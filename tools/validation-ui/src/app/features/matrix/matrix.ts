import { ApiResult, Json } from '../../core/api/api-client';

// Work item: TASK-095 (FEAT-020)
/** One tier row of the form; a row without "from" is left out of the policy. */
export interface TierInput {
  from: number | null;
  to: number | null;
  percentage: number | null;
}

// Work item: TASK-095 (FEAT-020)
/** A tier as the API stores and returns it. */
export interface Tier {
  minQuantity: number;
  maxQuantity: number | null;
  percentage: number;
}

// Work item: TASK-095 (FEAT-020)
/** One line of the check: the policy that priced the quantity, or the error of the rejected sale. */
export interface CheckRow {
  quantity: number;
  policy: string;
  ceiling: string;
  discount: string;
  total: string;
  error: string | null;
}

// Work item: TASK-095 (FEAT-020)
/** The POST /api/discount-policies body: scoped to the page's product, every branch. */
export function policyBody(productId: string, validFrom: string, maximum: number | null, rows: readonly TierInput[]): object {
  return {
    productId,
    branchId: null,
    validFrom,
    maxQuantityPerProduct: maximum,
    tiers: rows
      .filter((row) => row.from !== null)
      .map((row) => ({ minQuantity: row.from, maxQuantity: row.to, percentage: row.percentage })),
  };
}

// Work item: TASK-095 (FEAT-020)
/** Positive integers separated by commas or spaces; null when the list is empty or holds anything else. */
export function parseQuantities(text: string): number[] | null {
  const parts = text.split(/[\s,]+/).filter((part) => part !== '');
  if (parts.length === 0 || parts.some((part) => !/^\d+$/.test(part) || Number(part) === 0)) {
    return null;
  }
  return parts.map(Number);
}

// Work item: TASK-095 (FEAT-020)
export function tierText(tier: Tier): string {
  const range = tier.maxQuantity === null ? `${tier.minQuantity}+` : `${tier.minQuantity}–${tier.maxQuantity}`;
  return `${range} → ${tier.percentage}%`;
}

// Work item: TASK-095 (FEAT-020)
/** Reads the single item of a check sale; `labels` maps the known policy ids to their names. */
export function checkRow(quantity: number, result: ApiResult, labels: ReadonlyMap<string, string>): CheckRow {
  if (result.status !== 201) {
    const body: Json = result.body;
    const error = [body?.error, body?.detail].filter(Boolean).join(': ');
    return { quantity, policy: '', ceiling: '', discount: '', total: '', error: `${result.status} ${error}`.trim() };
  }
  const item: Json = result.body.data.items[0];
  const policyId = item.discountPolicyId as string;
  return {
    quantity,
    policy: labels.get(policyId) ?? policyId.slice(0, 8),
    ceiling: `${item.discountCeilingPercentage}%`,
    discount: Number(item.discountAmount).toFixed(2),
    total: Number(item.totalAmount).toFixed(2),
    error: null,
  };
}
