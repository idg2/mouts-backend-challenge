import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { ApiClient, ApiResult } from '../../core/api/api-client';
import { DEFAULT_POLICY_ID } from '../../scenarios/fixtures';
import { MatrixPage } from './matrix-page';

// Work item: TASK-095 (FEAT-020)
describe('MatrixPage', () => {
  const defaultPolicy = {
    id: DEFAULT_POLICY_ID, validFrom: '2026-01-01T00:00:00Z', maxQuantityPerProduct: 20,
    tiers: [{ minQuantity: 4, maxQuantity: 9, percentage: 10 }, { minQuantity: 10, maxQuantity: 20, percentage: 20 }],
  };

  function answer(status: number, body: unknown): ApiResult {
    return { status, body, date: null, ms: 1 };
  }

  async function render(policyAnswer: ApiResult) {
    const calls: { method: string; url: string; body: unknown }[] = [];
    const api = {
      send: async (method: string, url: string, body?: unknown) => {
        calls.push({ method, url, body });
        if (method === 'GET') return answer(200, { data: defaultPolicy });
        if (url === '/api/discount-policies') return policyAnswer;
        if (url === '/api/sales') {
          return answer(201, { data: { items: [{ discountPolicyId: DEFAULT_POLICY_ID, discountCeilingPercentage: 10, discountAmount: 5, totalAmount: 45 }] } });
        }
        return answer(201, { data: { id: `${url}-id` } });
      },
    };
    TestBed.configureTestingModule({ imports: [MatrixPage], providers: [{ provide: ApiClient, useValue: api }] });
    const fixture = TestBed.createComponent(MatrixPage);
    await fixture.whenStable();
    await new Promise((resolve) => setTimeout(resolve));
    await fixture.whenStable();
    return { fixture, calls, element: fixture.nativeElement as HTMLElement };
  }

  function rows(element: HTMLElement, table: string): string[] {
    // Cells, and the tiers inside a cell, joined by spaces: textContent alone runs them together.
    const cell = (td: Element) => {
      const tiers = Array.from(td.querySelectorAll('.keys > span'));
      return tiers.length > 0 ? tiers.map((tier) => tier.textContent).join(' ') : td.textContent;
    };
    return Array.from(element.querySelectorAll(`table.${table} tbody tr`)).map((row) =>
      Array.from(row.querySelectorAll('td')).map(cell).join(' ').replace(/\s+/g, ' ').trim());
  }

  async function click(fixture: { whenStable(): Promise<unknown> }, button: HTMLButtonElement) {
    button.click();
    await new Promise((resolve) => setTimeout(resolve));
    await fixture.whenStable();
  }

  it('creates its own customer, branch, and product, and shows the default policy', async () => {
    const { calls, element } = await render(answer(201, {}));

    expect(calls.filter((call) => call.method === 'POST').map((call) => call.url)).toEqual(['/api/customers', '/api/branches', '/api/products']);
    expect(rows(element, 'matrix')).toEqual(['Default every product 2026-01-01T00:00:00Z 20 4–9 → 10% 10–20 → 20%']);
  });

  it('adds a registered policy to the matrix as #1, scoped to the page product', async () => {
    const created = {
      id: 'policy-1', validFrom: '2026-09-27T10:00:02Z', maxQuantityPerProduct: 30,
      tiers: [{ minQuantity: 5, maxQuantity: 9, percentage: 15 }, { minQuantity: 10, maxQuantity: 30, percentage: 25 }],
    };
    const { fixture, calls, element } = await render(answer(201, { data: created }));

    await click(fixture, element.querySelector<HTMLButtonElement>('button.register')!);

    const post = calls.find((call) => call.url === '/api/discount-policies')!;
    expect(post.body).toMatchObject({ productId: '/api/products-id', branchId: null, maxQuantityPerProduct: 30 });
    expect(rows(element, 'matrix')[1]).toBe('#1 this page\'s product 2026-09-27T10:00:02Z 30 5–9 → 15% 10–30 → 25%');
  });

  it('shows the error of a refused policy and adds nothing', async () => {
    const { fixture, element } = await render(answer(400, { error: 'InvalidTierSet', detail: 'Tiers overlap.' }));

    await click(fixture, element.querySelector<HTMLButtonElement>('button.register')!);

    expect(element.querySelector('.error')?.textContent).toContain('400 InvalidTierSet: Tiers overlap.');
    expect(rows(element, 'matrix').length).toBe(1);
  });

  it('disables Check while a policy is being registered', async () => {
    const { fixture, element } = await render(new Promise<ApiResult>(() => undefined) as unknown as ApiResult);

    await click(fixture, element.querySelector<HTMLButtonElement>('button.register')!);

    expect(element.querySelector<HTMLButtonElement>('button.check')!.disabled).toBe(true);
  });

  it('checks each quantity with a real sale and shows the policy that priced it', async () => {
    const { fixture, calls, element } = await render(answer(201, {}));

    await click(fixture, element.querySelector<HTMLButtonElement>('button.check')!);

    const sales = calls.filter((call) => call.url === '/api/sales');
    expect(sales.map((call) => (call.body as { items: { quantity: number }[] }).items[0].quantity)).toEqual([3, 5, 10, 31]);
    expect(rows(element, 'checks')[0]).toBe('3 Default 10% 5.00 45.00');
  });
});
