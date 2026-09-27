import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { STATUS_SYMBOL, Scenario, ScenarioRun } from '../../scenarios/scenario';
import { FlowStrip } from './flow-strip';
import { StepTable } from './step-table';
import { TracePanel } from './trace-panel';

// Work item: TASK-089 (FEAT-020), TASK-094 (FEAT-020)
/** The selected scenario: title, run button, flow, steps, and, with the trace on, the trace panel. */
@Component({
  selector: 'app-scenario-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FlowStrip, StepTable, TracePanel],
  template: `
    <div class="hdr">
      <span class="id">{{ scenario().id }}</span>
      <h3>{{ scenario().title }}</h3>
      <span class="spacer"></span>
      <span>{{ statusText() }}</span>
      <button type="button" class="run" [disabled]="disabled()" (click)="runClicked.emit(scenario().id)">▶ Run</button>
    </div>
    <p class="desc">{{ scenario().description }}</p>
    <div class="h4">Flow</div>
    <app-flow-strip [nodes]="run().nodes" [traced]="traced()" />
    <div class="h4">Steps</div>
    <app-step-table [steps]="run().steps" [traced]="traced()" />
    @if (traceEnabled()) {
      <app-trace-panel [entries]="run().trace" [status]="run().status" />
    }
  `,
})
export class ScenarioDetail {
  readonly scenario = input.required<Scenario>();
  readonly run = input.required<ScenarioRun>();
  readonly disabled = input(false);
  readonly traceEnabled = input(false);

  readonly runClicked = output<string>();

  /** Every key the run's trace holds, topic and shared keys alike. */
  protected readonly traced = computed<ReadonlySet<string>>(() => new Set(
    this.run().trace.flatMap((entry) => [entry.key, entry.sharedKey]).filter((key): key is string => key !== null),
  ));

  protected readonly statusText = computed(() => {
    const run = this.run();
    const seconds = run.elapsedMs === null ? '' : ` · ${(run.elapsedMs / 1000).toFixed(1)} s`;
    const words = { idle: 'not run', running: 'running', pass: 'passed', fail: 'failed' }[run.status];
    return `${STATUS_SYMBOL[run.status]} ${words}${seconds}`;
  });
}
