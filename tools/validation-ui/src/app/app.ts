import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Session } from './core/auth/session';
import { AppConfigStore } from './core/config/app-config';
import { Diagnostics } from './core/diagnostics/diagnostics';

// Work item: TASK-089 (FEAT-020), TASK-095 (FEAT-020)
/** The shell: top bar with the API badge, the page links, and the signed-in user, a config error banner, and the routed page. */
@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="top">
      <b>DeveloperStore · Guided validation</b>
      <span class="badge" [class.ok]="diagnostics.mode() === 'debug'">{{ badge() }}</span>
      @if (session.email()) {
        <nav class="pages">
          <a routerLink="/" routerLinkActive="on" [routerLinkActiveOptions]="{ exact: true }">Scenarios</a>
          <a routerLink="/matrix" routerLinkActive="on">Discount matrix</a>
        </nav>
      }
      <span class="spacer"></span>
      @if (session.email(); as email) {
        <span class="user">{{ email }}</span>
        <button type="button" (click)="signOut()">Sign out</button>
      }
    </header>
    @if (config.error(); as error) {
      <div class="banner warn">{{ error }}</div>
    }
    <router-outlet />
  `,
})
export class App implements OnInit {
  protected readonly diagnostics = inject(Diagnostics);
  protected readonly session = inject(Session);
  protected readonly config = inject(AppConfigStore);
  private readonly router = inject(Router);

  protected readonly badge = computed(() => {
    switch (this.diagnostics.mode()) {
      case 'checking': return 'API: checking…';
      case 'debug': return this.diagnostics.traceEnabled() ? 'API: Debug · trace on' : 'API: Debug · trace off';
      case 'release': return 'API: Release · diagnostics unavailable';
      case 'unreachable': return 'API: unreachable';
    }
  });

  ngOnInit(): void {
    void this.diagnostics.probe();
  }

  protected signOut(): void {
    this.session.clear();
    void this.router.navigate(['/login']);
  }
}
