import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiClient } from '../../core/api/api-client';
import { Session } from '../../core/auth/session';
import { AppConfigStore } from '../../core/config/app-config';

// Work item: TASK-089 (FEAT-020)
/** One-click sign-in: the seeded administrator's e-mail and password come prefilled from config.json. */
@Component({
  selector: 'app-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form class="login" (submit)="signIn($event)">
      <h3>Sign in as the seeded administrator</h3>
      <label>E-mail <input name="email" type="email" [value]="email()" (input)="email.set(text($event))" /></label>
      <label>Password <input name="password" type="password" [value]="password()" (input)="password.set(text($event))" /></label>
      <p class="hint">The development values of <code>Seed:Admin</code>, already filled in. Click Sign in.</p>
      @if (error(); as message) {
        <p class="error">{{ message }}</p>
      }
      <button type="submit" class="run" [disabled]="busy()">Sign in</button>
    </form>
  `,
})
export class LoginPage {
  private readonly api = inject(ApiClient);
  private readonly session = inject(Session);
  private readonly router = inject(Router);
  private readonly hint = inject(AppConfigStore).config()?.loginHint;

  protected readonly email = signal(this.hint?.email ?? '');
  protected readonly password = signal(this.hint?.password ?? '');
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected text(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected async signIn(event: Event): Promise<void> {
    event.preventDefault();
    this.busy.set(true);
    this.error.set(null);
    try {
      const result = await this.api.send('POST', '/api/auth', { email: this.email(), password: this.password() });
      if (result.status === 200) {
        this.session.start(result.body.data.token, this.email());
        await this.router.navigate(['/']);
      } else {
        this.error.set(`Sign-in failed (${result.status}): ${result.body?.detail ?? result.body?.error ?? 'no detail'}`);
      }
    } catch {
      this.error.set('The API cannot be reached.');
    } finally {
      this.busy.set(false);
    }
  }
}
