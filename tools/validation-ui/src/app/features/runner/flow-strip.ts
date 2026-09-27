import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { isTraced } from '../../scenarios/doc-keys';
import { FlowNode } from '../../scenarios/scenario';

// Work item: TASK-089 (FEAT-020), TASK-094 (FEAT-020)
/**
 * The flat flow of a scenario: one box per node; decision boxes show the answer the run proved. The node's documented
 * keys sit above its text, green when the run's trace holds them.
 */
@Component({
  selector: 'app-flow-strip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flow">
      @for (node of nodes(); track node.id; let last = $last) {
        <div class="n" [class.dec]="node.decision" [class]="node.state" [attr.data-state]="node.state">
          @if (node.keys?.length) {
            <span class="keys">
              @for (key of node.keys; track key) {
                <span class="key" [class.seen]="seen(key)">{{ key }}</span>
              }
            </span>
          }
          {{ node.label }}
          @if (node.detail) {
            <small>{{ node.detail }}</small>
          }
        </div>
        @if (!last) {
          <span class="ar" [class.done]="node.state === 'done'">→</span>
        }
      }
    </div>
  `,
})
export class FlowStrip {
  readonly nodes = input.required<readonly FlowNode[]>();
  readonly traced = input<ReadonlySet<string>>(new Set());

  protected seen(key: string): boolean {
    return isTraced(key, this.traced());
  }
}
