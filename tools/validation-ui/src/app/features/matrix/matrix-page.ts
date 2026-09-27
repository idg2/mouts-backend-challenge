import { ChangeDetectionStrategy, Component, OnInit, WritableSignal, inject, signal } from '@angular/core';
import { ApiClient, ApiResult, Json } from '../../core/api/api-client';
import { DEFAULT_POLICY_ID, References, cpf } from '../../scenarios/fixtures';
import { ServerClock } from '../../scenarios/server-clock';
import { CheckRow, Tier, TierInput, checkRow, parseQuantities, policyBody, tierText } from './matrix';

// Work item: TASK-095 (FEAT-020)
/** A policy shown in the matrix table. */
interface MatrixEntry {
  id: string;
  label: string;
  scope: string;
  validFrom: string;
  maximum: number;
  tiers: Tier[];
}

// Work item: TASK-095 (FEAT-020)
/**
 * Discount matrix demo: registers policies for a product of its own, so the scenarios and other sales are untouched,
 * then prices real sales per quantity to show which policy applied. Works with a Release API too.
 */
@Component({
  selector: 'app-matrix-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="main matrix-page">
      <p class="desc">
        This page creates its own customer, branch, and product on load, so the policies you register here apply to that product only.
        Register a policy, then check some quantities: each one is a real sale, priced by the newest policy of the product.
      </p>
      @if (setupError(); as message) {
        <div class="banner warn">{{ message }}</div>
      }

      <div class="h4">Matrix</div>
      <table class="steps matrix">
        <thead><tr><th>Policy</th><th>Scope</th><th>Valid from</th><th class="num">Max per product</th><th>Tiers</th></tr></thead>
        <tbody>
          @for (entry of entries(); track entry.id) {
            <tr>
              <td>{{ entry.label }}</td>
              <td>{{ entry.scope }}</td>
              <td>{{ entry.validFrom }}</td>
              <td class="num">{{ entry.maximum }}</td>
              <td><span class="keys">@for (tier of entry.tiers; track $index) {<span class="tier">{{ tierText(tier) }}</span>}</span></td>
            </tr>
          }
        </tbody>
      </table>

      <div class="cards">
        <section class="card">
          <div class="h4">Register a policy for this page's product</div>
          <div class="field">
            <label for="maximum">Max per product</label>
            <input id="maximum" type="number" min="1" [value]="maximum() ?? ''" (input)="maximum.set(number($event))" />
          </div>
          <table class="tiers">
            <thead><tr><th></th><th>From</th><th>To</th><th>Discount %</th></tr></thead>
            <tbody>
              @for (row of rows(); track $index; let index = $index) {
                <tr>
                  <td>Tier {{ index + 1 }}</td>
                  <td><input type="number" min="1" [value]="row.from ?? ''" (input)="setRow(index, 'from', $event)" /></td>
                  <td><input type="number" min="1" placeholder="max" [value]="row.to ?? ''" (input)="setRow(index, 'to', $event)" /></td>
                  <td><input type="number" min="0" step="0.01" [value]="row.percentage ?? ''" (input)="setRow(index, 'percentage', $event)" /></td>
                </tr>
              }
            </tbody>
          </table>
          <p class="hint">Leave "From" empty to skip a row, and "To" empty for no upper bound. The policy starts 2 s after the API's clock.</p>
          @if (registerError(); as message) {
            <p class="error">{{ message }}</p>
          }
          <button type="button" class="run register" [disabled]="busy() || !references()" (click)="register()">Register policy</button>
        </section>

        <section class="card">
          <div class="h4">Check quantities</div>
          <div class="field">
            <label for="quantities">Quantities</label>
            <input id="quantities" class="wide" [value]="quantities()" (input)="quantities.set(text($event))" />
          </div>
          <p class="hint">Each quantity is one real sale of this page's product, after the newest policy has started.</p>
          @if (checkError(); as message) {
            <p class="error">{{ message }}</p>
          }
          <button type="button" class="run check" [disabled]="busy() || !references()" (click)="check()">Check</button>
        </section>
      </div>

      <table class="steps checks">
        <thead><tr><th class="num">Quantity</th><th>Policy</th><th class="num">Ceiling</th><th class="num">Discount</th><th class="num">Total</th></tr></thead>
        <tbody>
          @for (row of checks(); track $index) {
            <tr [class.failed]="row.error">
              <td class="num">{{ row.quantity }}</td>
              @if (row.error) {
                <td colspan="4">{{ row.error }}</td>
              } @else {
                <td>{{ row.policy }}</td><td class="num">{{ row.ceiling }}</td><td class="num">{{ row.discount }}</td><td class="num">{{ row.total }}</td>
              }
            </tr>
          } @empty {
            <tr><td colspan="5" class="muted">Not checked yet.</td></tr>
          }
        </tbody>
      </table>
    </main>
  `,
})
export class MatrixPage implements OnInit {
  private readonly api = inject(ApiClient);
  private readonly clock = new ServerClock();
  private latestValidFrom = 0;

  protected readonly references = signal<References | null>(null);
  protected readonly entries = signal<MatrixEntry[]>([]);
  protected readonly setupError = signal<string | null>(null);
  protected readonly registerError = signal<string | null>(null);
  protected readonly checkError = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly maximum = signal<number | null>(30);
  protected readonly rows = signal<TierInput[]>([
    { from: 5, to: 9, percentage: 15 },
    { from: 10, to: 30, percentage: 25 },
    { from: null, to: null, percentage: null },
  ]);
  protected readonly quantities = signal('3, 5, 10, 31');
  protected readonly checks = signal<CheckRow[]>([]);
  protected readonly tierText = tierText;

  async ngOnInit(): Promise<void> {
    try {
      const policy = await this.send('GET', `/api/discount-policies/${DEFAULT_POLICY_ID}`);
      if (policy.status === 200) {
        this.entries.set([this.entry(policy.body.data, 'Default', 'every product')]);
      }
      const customerId = await this.create('/api/customers', { name: `Matrix customer ${suffix()}`, document: cpf() });
      const branchId = await this.create('/api/branches', { name: `Matrix branch ${suffix()}` });
      const code = suffix();
      const productId = await this.create('/api/products', { code: `MTX-${code}`, description: `Matrix product ${code}`, unitPrice: 10 });
      this.references.set({ customerId, branchId, productId });
    } catch (error) {
      this.setupError.set(`The page could not prepare its data: ${(error as Error).message}. Reload once the API is up.`);
    }
  }

  protected text(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected number(event: Event): number | null {
    const value = this.text(event);
    return value === '' ? null : Number(value);
  }

  protected setRow(index: number, field: keyof TierInput, event: Event): void {
    const value = this.number(event);
    this.rows.update((rows) => rows.map((row, current) => (current === index ? { ...row, [field]: value } : row)));
  }

  protected async register(): Promise<void> {
    await this.guard(this.registerError, async () => {
      const validFrom = new Date(this.clock.now() + 2_000).toISOString();
      const result = await this.send('POST', '/api/discount-policies', policyBody(this.references()!.productId, validFrom, this.maximum(), this.rows()));
      if (result.status !== 201) {
        this.registerError.set(`${result.status} ${[result.body?.error, result.body?.detail].filter(Boolean).join(': ')}`);
        return;
      }
      this.latestValidFrom = Date.parse(validFrom);
      const label = `#${this.entries().length}`;
      this.entries.update((entries) => [...entries, this.entry(result.body.data, label, "this page's product")]);
    });
  }

  protected async check(): Promise<void> {
    await this.guard(this.checkError, async () => {
      const quantities = parseQuantities(this.quantities());
      if (!quantities) {
        this.checkError.set('Type positive whole numbers separated by commas, for example 3, 5, 10.');
        return;
      }
      const waitMs = this.latestValidFrom - this.clock.now() + 100;
      if (waitMs > 0) {
        await new Promise((resolve) => setTimeout(resolve, waitMs));
      }
      const labels = new Map(this.entries().map((entry) => [entry.id, entry.label]));
      const { customerId, branchId, productId } = this.references()!;
      this.checks.set([]);
      for (const quantity of quantities) {
        const result = await this.send('POST', '/api/sales', { customerId, branchId, items: [{ productId, quantity }] });
        this.checks.update((rows) => [...rows, checkRow(quantity, result, labels)]);
      }
    });
  }

  /** Runs one action with the buttons disabled, showing a network failure in `error`. */
  private async guard(error: WritableSignal<string | null>, action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    error.set(null);
    try {
      await action();
    } catch {
      error.set('The API cannot be reached.');
    } finally {
      this.busy.set(false);
    }
  }

  private async send(method: string, url: string, body?: unknown): Promise<ApiResult> {
    const result = await this.api.send(method, url, body);
    this.clock.observe(result.date);
    return result;
  }

  private async create(url: string, body: object): Promise<string> {
    const result = await this.send('POST', url, body);
    if (result.status !== 201) {
      throw new Error(`POST ${url} answered ${result.status}`);
    }
    return result.body.data.id as string;
  }

  private entry(policy: Json, label: string, scope: string): MatrixEntry {
    return { id: policy.id, label, scope, validFrom: policy.validFrom, maximum: policy.maxQuantityPerProduct, tiers: policy.tiers };
  }
}

// Work item: TASK-095 (FEAT-020)
function suffix(): string {
  return crypto.randomUUID().slice(0, 8);
}
