import { describe, expect, it } from 'vitest';
import { OutboxEntry, customerIdOf, eventsForCustomer, eventsForSale, inSequenceOrder, saleIdOf, typesOf } from './outbox-events';

// Work item: TASK-088 (FEAT-020)
function entry(sequence: number, type: string, payload: object): OutboxEntry {
  return { id: `id-${sequence}`, sequence, type, occurredAt: '', processedAt: null, payload };
}

const SALE = '0B6E0000-0000-0000-0000-000000000001';
const OTHER = '0b6e0000-0000-0000-0000-000000000002';
const CUSTOMER = 'c0000000-0000-0000-0000-000000000009';

// Work item: TASK-088 (FEAT-020)
describe('outbox events', () => {
  const entries = [
    entry(1, 'SaleCreated', { Sale: { SaleId: SALE.toLowerCase(), CustomerId: CUSTOMER } }),
    entry(2, 'SaleCreated', { Sale: { SaleId: OTHER, CustomerId: 'someone-else' } }),
    entry(3, 'ItemCancelled', { SaleId: SALE.toLowerCase(), ItemId: 'i', ProductId: 'p' }),
    entry(4, 'SaleDeleted', { SaleId: OTHER }),
  ];

  it('reads the sale id from both payload shapes', () => {
    expect(saleIdOf(entries[0])).toBe(SALE.toLowerCase());
    expect(saleIdOf(entries[2])).toBe(SALE.toLowerCase());
  });

  it('filters by sale, ignoring GUID case', () => {
    expect(typesOf(eventsForSale(entries, SALE))).toBe('SaleCreated, ItemCancelled');
  });

  it('filters by customer from the sale snapshot', () => {
    expect(customerIdOf(entries[2])).toBeNull();
    expect(typesOf(eventsForCustomer(entries, CUSTOMER))).toBe('SaleCreated');
    expect(typesOf(eventsForCustomer(entries, 'nobody'))).toBe('none');
  });

  it('checks the sequence order', () => {
    expect(inSequenceOrder(entries)).toBe(true);
    expect(inSequenceOrder([entries[1], entries[0]])).toBe(false);
  });
});
