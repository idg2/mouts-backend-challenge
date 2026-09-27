import { describe, expect, it } from 'vitest';
import { checkRow, parseQuantities, policyBody, tierText } from './matrix';

// Work item: TASK-095 (FEAT-020)
describe('policyBody', () => {
  it('scopes the policy to the product, keeps the filled tier rows, and sends an empty "to" as null', () => {
    const body = policyBody('p-1', '2026-09-27T10:00:02.000Z', 30, [
      { from: 5, to: 9, percentage: 15 },
      { from: 10, to: null, percentage: 25 },
      { from: null, to: null, percentage: null },
    ]);

    expect(body).toEqual({
      productId: 'p-1',
      branchId: null,
      validFrom: '2026-09-27T10:00:02.000Z',
      maxQuantityPerProduct: 30,
      tiers: [
        { minQuantity: 5, maxQuantity: 9, percentage: 15 },
        { minQuantity: 10, maxQuantity: null, percentage: 25 },
      ],
    });
  });
});

// Work item: TASK-095 (FEAT-020)
describe('parseQuantities', () => {
  it('reads positive integers separated by commas or spaces', () => {
    expect(parseQuantities(' 3, 4 10,,25 ')).toEqual([3, 4, 10, 25]);
  });

  it('refuses an empty list, zero, a fraction, or text', () => {
    expect(parseQuantities('  ')).toBeNull();
    expect(parseQuantities('3, 0')).toBeNull();
    expect(parseQuantities('3, 4.5')).toBeNull();
    expect(parseQuantities('3, x')).toBeNull();
  });
});

// Work item: TASK-095 (FEAT-020)
describe('tierText', () => {
  it('shows a closed and an open tier', () => {
    expect(tierText({ minQuantity: 4, maxQuantity: 9, percentage: 10 })).toBe('4–9 → 10%');
    expect(tierText({ minQuantity: 10, maxQuantity: null, percentage: 20 })).toBe('10+ → 20%');
  });
});

// Work item: TASK-095 (FEAT-020)
describe('checkRow', () => {
  const labels = new Map([['default-id', 'Default'], ['policy-1', '#1']]);

  it('names the policy that priced the item and shows its ceiling, discount, and total', () => {
    const sale = {
      status: 201, date: null, ms: 1,
      body: { data: { items: [{ discountPolicyId: 'policy-1', discountCeilingPercentage: 15, discountAmount: 9, totalAmount: 51 }] } },
    };

    expect(checkRow(6, sale, labels)).toEqual({ quantity: 6, policy: '#1', ceiling: '15%', discount: '9.00', total: '51.00', error: null });
  });

  it('shows the short id of a policy the page does not know', () => {
    const sale = {
      status: 201, date: null, ms: 1,
      body: { data: { items: [{ discountPolicyId: 'abcdef12-3456', discountCeilingPercentage: 0, discountAmount: 0, totalAmount: 30 }] } },
    };

    expect(checkRow(3, sale, labels).policy).toBe('abcdef12');
  });

  it('shows the error code and detail of a rejected sale', () => {
    const rejected = { status: 400, date: null, ms: 1, body: { error: 'QuantityLimitExceeded', detail: 'At most 30 units.' } };

    expect(checkRow(31, rejected, labels)).toEqual({
      quantity: 31, policy: '', ceiling: '', discount: '', total: '', error: '400 QuantityLimitExceeded: At most 30 units.',
    });
  });
});
