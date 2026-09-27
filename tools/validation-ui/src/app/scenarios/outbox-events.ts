import { Json } from '../core/api/api-client';
import { ScenarioContext, ScenarioFailure } from './scenario';

// Work item: TASK-088 (FEAT-020)
/** One outbox row from GET /api/diagnostics/outbox; `payload` keeps the stored PascalCase members. */
export interface OutboxEntry {
  id: string;
  sequence: number;
  type: string;
  occurredAt: string;
  processedAt: string | null;
  payload: Json;
}

// Work item: TASK-088 (FEAT-020)
/** Reads every outbox row after the scenario's start cursor, in sequence order. */
export async function readOutbox(ctx: ScenarioContext): Promise<OutboxEntry[]> {
  const result = await ctx.api.get(`/api/diagnostics/outbox?after=${ctx.cursors.outbox}`);
  if (result.status !== 200) {
    throw new ScenarioFailure(`GET /api/diagnostics/outbox answered ${result.status}`);
  }
  return result.body.data.items as OutboxEntry[];
}

// Work item: TASK-088 (FEAT-020)
/** The sale of an event: `Sale.SaleId` for SaleCreated and SaleModified, `SaleId` for the others. */
export function saleIdOf(entry: OutboxEntry): string | null {
  const id = entry.payload?.Sale?.SaleId ?? entry.payload?.SaleId;
  return typeof id === 'string' ? id.toLowerCase() : null;
}

// Work item: TASK-088 (FEAT-020)
/** The customer of an event; only the events carrying a sale snapshot have one. */
export function customerIdOf(entry: OutboxEntry): string | null {
  const id = entry.payload?.Sale?.CustomerId;
  return typeof id === 'string' ? id.toLowerCase() : null;
}

// Work item: TASK-088 (FEAT-020)
export function eventsForSale(entries: OutboxEntry[], saleId: string): OutboxEntry[] {
  return entries.filter((entry) => saleIdOf(entry) === saleId.toLowerCase());
}

// Work item: TASK-088 (FEAT-020)
export function eventsForCustomer(entries: OutboxEntry[], customerId: string): OutboxEntry[] {
  return entries.filter((entry) => customerIdOf(entry) === customerId.toLowerCase());
}

// Work item: TASK-088 (FEAT-020)
/** The event types joined by ", ", or "none". */
export function typesOf(entries: OutboxEntry[]): string {
  return entries.map((entry) => entry.type).join(', ') || 'none';
}

// Work item: TASK-088 (FEAT-020)
export function inSequenceOrder(entries: OutboxEntry[]): boolean {
  return entries.every((entry, index) => index === 0 || entries[index - 1].sequence < entry.sequence);
}
