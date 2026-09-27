import { Injectable, inject, signal } from '@angular/core';
import { ApiClient } from '../api/api-client';

// Work item: TASK-087 (FEAT-020)
/** What the status probe found: a Debug API, a Release API, or none. */
export type ApiMode = 'checking' | 'debug' | 'release' | 'unreachable';

// Work item: TASK-087 (FEAT-020)
/** Probes GET /api/diagnostics: 200 is a Debug build, 404 a Release build, anything else unreachable. */
@Injectable({ providedIn: 'root' })
export class Diagnostics {
  private readonly api = inject(ApiClient);

  readonly mode = signal<ApiMode>('checking');
  readonly traceEnabled = signal(false);

  async probe(): Promise<void> {
    try {
      const result = await this.api.send('GET', '/api/diagnostics');
      if (result.status === 200) {
        this.traceEnabled.set(result.body?.data?.traceEnabled === true);
        this.mode.set('debug');
      } else {
        this.mode.set(result.status === 404 ? 'release' : 'unreachable');
      }
    } catch {
      this.mode.set('unreachable');
    }
  }
}
