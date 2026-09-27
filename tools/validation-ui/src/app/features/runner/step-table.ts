import { ChangeDetectionStrategy, Component, effect, input, signal } from '@angular/core';
import { isTraced } from '../../scenarios/doc-keys';
import { HttpExchange, STATUS_SYMBOL, StepResult } from '../../scenarios/scenario';

// Work item: TASK-089 (FEAT-020), TASK-094 (FEAT-020)
/**
 * Expected versus actual per step, with the step's documented keys (green when traced); a row expands to the HTTP
 * exchanges it made.
 */
@Component({
  selector: 'app-step-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <table class="steps">
      <thead><tr><th></th><th>Key</th><th>Step</th><th>Expected</th><th>Actual</th><th></th></tr></thead>
      <tbody>
        @for (step of steps(); track $index; let index = $index) {
          <tr [class.failed]="step.status === 'fail'">
            <td>{{ symbol(step) }}</td>
            <td>
              <span class="keys">
                @for (key of step.keys; track key) {
                  <span class="key" [class.seen]="seen(key)">{{ key }}</span>
                }
              </span>
            </td>
            <td>{{ step.label }}</td>
            <td>{{ step.expected }}</td>
            <td>{{ step.actual }}</td>
            <td>
              @if (step.exchanges.length > 0) {
                <button type="button" class="link" (click)="toggle(index)">{{ expanded().has(index) ? 'hide' : 'view' }}</button>
              }
            </td>
          </tr>
          @if (expanded().has(index)) {
            @for (exchange of step.exchanges; track $index) {
              <tr class="exchange">
                <td></td>
                <td colspan="5">
                  <div class="pair">
                    <pre>{{ requestText(exchange) }}</pre>
                    <pre>{{ responseText(exchange) }}</pre>
                  </div>
                </td>
              </tr>
            }
          }
        } @empty {
          <tr><td colspan="6" class="muted">Not run yet.</td></tr>
        }
      </tbody>
    </table>
  `,
})
export class StepTable {
  readonly steps = input.required<readonly StepResult[]>();
  readonly traced = input<ReadonlySet<string>>(new Set());
  protected readonly expanded = signal<ReadonlySet<number>>(new Set());

  constructor() {
    // A new run starts with no steps: rows expanded in the previous run must not reopen by index.
    effect(() => {
      if (this.steps().length === 0) {
        this.expanded.set(new Set());
      }
    });
  }

  protected seen(key: string): boolean {
    return isTraced(key, this.traced());
  }

  protected symbol(step: StepResult): string {
    return step.status === 'running' ? STATUS_SYMBOL.running : STATUS_SYMBOL[step.status];
  }

  protected toggle(index: number): void {
    this.expanded.update((current) => {
      const next = new Set(current);
      if (!next.delete(index)) {
        next.add(index);
      }
      return next;
    });
  }

  protected requestText(exchange: HttpExchange): string {
    return `${exchange.method} ${exchange.url}${exchange.requestBody === null ? '' : `\n${JSON.stringify(exchange.requestBody, null, 2)}`}`;
  }

  protected responseText(exchange: HttpExchange): string {
    return `${exchange.status} · ${exchange.ms} ms${exchange.responseBody == null ? '' : `\n${JSON.stringify(exchange.responseBody, null, 2)}`}`;
  }
}
