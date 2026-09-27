import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Diagnostics } from '../../core/diagnostics/diagnostics';
import { Runner } from './runner';
import { ScenarioDetail } from './scenario-detail';
import { ScenarioList } from './scenario-list';

// Work item: TASK-089 (FEAT-020)
/** The main screen: banners for a Release or unreachable API, the scenario list, and the selected scenario. */
@Component({
  selector: 'app-runner-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ScenarioList, ScenarioDetail],
  template: `
    @switch (diagnostics.mode()) {
      @case ('release') {
        <div class="banner warn">
          The API was compiled in Release, so the outbox and the trace are not exposed and the scenarios cannot be proven.
          Start the API with <code>make debug-up</code> or <code>dotnet run</code> (Debug).
        </div>
      }
      @case ('unreachable') {
        <div class="banner warn">The API cannot be reached. Reload this page once it is up.</div>
      }
    }
    <div class="body">
      <aside class="side">
        <app-scenario-list
          [scenarios]="runner.scenarios" [runs]="runner.runs()" [counts]="runner.counts()"
          [selectedId]="selectedId()" [disabled]="disabled()"
          (selected)="selectedId.set($event)" (run)="run($event)" (runAll)="runAll()" />
      </aside>
      <main class="main">
        @if (selected(); as scenario) {
          <!-- One detail instance per scenario, so its expanded rows never carry over to another scenario. -->
          @for (current of [scenario]; track current.id) {
            <app-scenario-detail
              [scenario]="current" [run]="runner.runs()[current.id]" [disabled]="disabled()"
              [traceEnabled]="diagnostics.traceEnabled()" (runClicked)="run($event)" />
          }
        }
      </main>
    </div>
  `,
})
export class RunnerPage {
  protected readonly runner = inject(Runner);
  protected readonly diagnostics = inject(Diagnostics);

  protected readonly selectedId = signal(this.runner.scenarios[0]?.id ?? '');
  protected readonly selected = computed(() => this.runner.scenarios.find((scenario) => scenario.id === this.selectedId()));
  protected readonly disabled = computed(() => this.diagnostics.mode() !== 'debug' || this.runner.busy());

  protected run(id: string): void {
    this.selectedId.set(id);
    void this.runner.run(id);
  }

  protected runAll(): void {
    void this.runner.runAll();
  }
}
