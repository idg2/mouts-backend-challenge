import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

// Work item: TASK-086 (FEAT-020)
/** The runtime configuration served as /config.json. */
export interface AppConfig {
  loginHint: { email: string; password: string };
}

// Work item: TASK-086 (FEAT-020)
/** Validates the raw /config.json, throwing an error that names the first missing field. */
export function parseAppConfig(raw: unknown): AppConfig {
  const hint = (raw as { loginHint?: Record<string, unknown> } | null)?.loginHint;
  for (const field of ['email', 'password'] as const) {
    const value = hint?.[field];
    if (typeof value !== 'string' || value === '') {
      throw new Error(`config.json is missing loginHint.${field}`);
    }
  }
  return { loginHint: { email: hint!['email'] as string, password: hint!['password'] as string } };
}

// Work item: TASK-086 (FEAT-020)
/** Loads /config.json before bootstrap; a failure is kept in `error` for the page to show, never thrown. */
@Injectable({ providedIn: 'root' })
export class AppConfigStore {
  private readonly http = inject(HttpClient);

  readonly config = signal<AppConfig | null>(null);
  readonly error = signal<string | null>(null);

  async load(): Promise<void> {
    try {
      this.config.set(parseAppConfig(await firstValueFrom(this.http.get('/config.json'))));
    } catch (error) {
      this.error.set(error instanceof Error ? error.message : 'config.json could not be loaded');
    }
  }
}
