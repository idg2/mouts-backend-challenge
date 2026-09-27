import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RunStatus, STATUS_SYMBOL, Scenario, ScenarioGroup, ScenarioRun } from '../../scenarios/scenario';

// Work item: TASK-089 (FEAT-020)
/** The scenarios grouped as in the catalog, each with its status and its own run button, plus "Run all". */
@Component({
  selector: 'app-scenario-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button type="button" class="run-all" [disabled]="disabled()" (click)="runAll.emit()">▶ Run all ({{ scenarios().length }})</button>
    <div class="sum">
      <span>✅ {{ counts().pass }}</span><span>❌ {{ counts().fail }}</span>
      <span>⏳ {{ counts().running }}</span><span class="muted">· {{ counts().idle }}</span>
    </div>
    @for (group of groups(); track group.name) {
      <div class="grp">{{ group.name }}</div>
      @for (scenario of group.scenarios; track scenario.id) {
        <div class="sc" [class.sel]="scenario.id === selectedId()" (click)="selected.emit(scenario.id)">
          <span class="id">{{ scenario.id }}</span>
          <span class="nm">{{ scenario.title }}</span>
          <button type="button" class="play" [disabled]="disabled()" (click)="$event.stopPropagation(); run.emit(scenario.id)">▶</button>
          <span>{{ symbol(scenario.id) }}</span>
        </div>
      }
    }
  `,
})
export class ScenarioList {
  readonly scenarios = input.required<readonly Scenario[]>();
  readonly runs = input.required<Record<string, ScenarioRun>>();
  readonly counts = input.required<Record<RunStatus, number>>();
  readonly selectedId = input.required<string>();
  readonly disabled = input(false);

  readonly selected = output<string>();
  readonly run = output<string>();
  readonly runAll = output<void>();

  protected readonly groups = computed(() => {
    const groups: { name: ScenarioGroup; scenarios: Scenario[] }[] = [];
    for (const scenario of this.scenarios()) {
      const group = groups.find((candidate) => candidate.name === scenario.group);
      if (group) {
        group.scenarios.push(scenario);
      } else {
        groups.push({ name: scenario.group, scenarios: [scenario] });
      }
    }
    return groups;
  });

  protected symbol(id: string): string {
    return STATUS_SYMBOL[this.runs()[id]?.status ?? 'idle'];
  }
}
