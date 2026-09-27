import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RunStatus } from '../../scenarios/scenario';
import { TraceEntry } from './trace-filter';

// Work item: TASK-089 (FEAT-020)
/** The StepTrace events of the last run, read once when it ended; idle relay cycles are already removed. */
@Component({
  selector: 'app-trace-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <details class="trace">
      <summary>Trace · {{ entries().length }} events (idle relay cycles hidden)</summary>
      <div class="tl">
        @if (status() === 'running') {
          <div class="muted">The trace is read when the scenario ends.</div>
        }
        @for (entry of entries(); track entry.cursor) {
          <div>
            <span class="t">{{ time(entry) }} T{{ entry.threadId }}</span>
            <span class="k">{{ keys(entry) }}</span>
            {{ entry.title }}
            <span class="v">{{ values(entry) }}</span>
            <span class="t">{{ entry.file }}:{{ entry.line }}</span>
          </div>
        }
      </div>
    </details>
  `,
})
export class TracePanel {
  readonly entries = input.required<readonly TraceEntry[]>();
  readonly status = input.required<RunStatus>();

  protected time(entry: TraceEntry): string {
    return entry.at.slice(11, 23);
  }

  protected keys(entry: TraceEntry): string {
    return [entry.key, entry.sharedKey].filter((key) => key !== null).join(' ');
  }

  protected values(entry: TraceEntry): string {
    return entry.values.map((value) => `${value.name}=${value.value}`).join(' ');
  }
}
