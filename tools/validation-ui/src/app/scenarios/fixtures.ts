import { Json } from '../core/api/api-client';
import { eventsForCustomer, eventsForSale, readOutbox, typesOf } from './outbox-events';
import { Outcome, ScenarioApi, ScenarioContext, ScenarioFailure, outcome } from './scenario';

// Work item: TASK-090 (FEAT-020)
/** The default discount policy seeded by the AddDiscountPolicies migration: the challenge rules. */
export const DEFAULT_POLICY_ID = '7d0c5a6e-2f4b-4c1d-9a39-0f6f2b8a1c01';

// Work item: TASK-090 (FEAT-020)
export const UNIT_PRICE = 10;

// Work item: TASK-090 (FEAT-020)
export interface References {
  customerId: string;
  branchId: string;
  productId: string;
}

// Work item: TASK-090 (FEAT-020)
export interface SaleLine {
  productId: string;
  quantity: number;
  discountPercentage?: number;
}

// Work item: TASK-090 (FEAT-020)
/** A valid CPF: nine random digits (not all equal) and the two check digits. */
export function cpf(random: () => number = Math.random): string {
  let digits: number[];
  do {
    digits = Array.from({ length: 9 }, () => Math.floor(random() * 10));
  } while (new Set(digits).size === 1);
  digits.push(checkDigit(digits, 9));
  digits.push(checkDigit(digits, 10));
  return digits.join('');
}

// Work item: TASK-090 (FEAT-020)
function checkDigit(digits: number[], length: number): number {
  const sum = digits.slice(0, length).reduce((total, digit, index) => total + digit * (length + 1 - index), 0);
  const remainder = sum % 11;
  return remainder < 2 ? 0 : 11 - remainder;
}

// Work item: TASK-090 (FEAT-020)
function suffix(): string {
  return crypto.randomUUID().slice(0, 8);
}

// Work item: TASK-090 (FEAT-020)
async function create(api: ScenarioApi, url: string, body: object): Promise<string> {
  const result = await api.post(url, body);
  if (result.status !== 201) {
    throw new ScenarioFailure(`POST ${url} answered ${result.status}`);
  }
  return result.body.data.id as string;
}

// Work item: TASK-090 (FEAT-020)
export function createBranch(api: ScenarioApi): Promise<string> {
  return create(api, '/api/branches', { name: `Validation branch ${suffix()}` });
}

// Work item: TASK-090 (FEAT-020)
export function createProduct(api: ScenarioApi, unitPrice = UNIT_PRICE): Promise<string> {
  const id = suffix();
  return create(api, '/api/products', { code: `VAL-${id}`, description: `Validation product ${id}`, unitPrice });
}

// Work item: TASK-090 (FEAT-020)
/** The first step of every scenario: a new customer, branch, and product, so runs never share data. */
export function prepare(ctx: ScenarioContext): Promise<References> {
  return ctx.step('Create customer, branch, and product', '3 × 201', async () => {
    const customerId = await create(ctx.api, '/api/customers', { name: `Validation customer ${suffix()}`, document: cpf() });
    const branchId = await createBranch(ctx.api);
    const productId = await createProduct(ctx.api);
    return outcome('3 × 201', true, { customerId, branchId, productId });
  });
}

// Work item: TASK-090 (FEAT-020)
export function saleBody(references: References, lines: SaleLine[]): object {
  return { customerId: references.customerId, branchId: references.branchId, items: lines };
}

// Work item: TASK-090 (FEAT-020)
export function short(id: string): string {
  return id.slice(0, 8);
}

// Work item: TASK-090 (FEAT-020)
export function policyOutcome(actualId: string, expectedId: string, label: string): Outcome<null> {
  const pass = actualId?.toLowerCase() === expectedId.toLowerCase();
  return outcome(pass ? label : `other policy ${short(String(actualId))}`, pass, null);
}

// Work item: TASK-090 (FEAT-020)
export function itemById(sale: Json, id: string): Json {
  return (sale.items as Json[]).find((item) => item.id === id);
}

// Work item: TASK-090 (FEAT-020)
/** A synchronous create commits its SaleCreated with the sale, so it is readable at once. */
export function expectCreatedRecorded(ctx: ScenarioContext, saleId: string, nodeId: string): Promise<null> {
  return ctx.step('Outbox: SaleCreated recorded', 'SaleCreated', async () => {
    const types = typesOf(eventsForSale(await readOutbox(ctx), saleId));
    return outcome(types, types === 'SaleCreated', null);
  }, nodeId);
}

// Work item: TASK-090 (FEAT-020)
/** A rejected write leaves no event: nothing after the start cursor mentions the scenario's customer. */
export function expectNoEventForCustomer(ctx: ScenarioContext, customerId: string, nodeId: string): Promise<null> {
  return ctx.step('Outbox: no event for the customer', 'none', async () => {
    const types = typesOf(eventsForCustomer(await readOutbox(ctx), customerId));
    return outcome(types, types === 'none', null);
  }, nodeId);
}
