import { Injectable, signal } from '@angular/core';

// Work item: TASK-087 (FEAT-020)
const TOKEN_KEY = 'validation-ui.token';
// Work item: TASK-087 (FEAT-020)
const EMAIL_KEY = 'validation-ui.email';

// Work item: TASK-087 (FEAT-020)
/** The signed-in administrator, kept in sessionStorage so a reload keeps the session of this tab only. */
@Injectable({ providedIn: 'root' })
export class Session {
  private readonly tokenValue = signal<string | null>(read(TOKEN_KEY));
  private readonly emailValue = signal<string | null>(read(EMAIL_KEY));

  readonly token = this.tokenValue.asReadonly();
  readonly email = this.emailValue.asReadonly();

  start(token: string, email: string): void {
    write(TOKEN_KEY, token);
    write(EMAIL_KEY, email);
    this.tokenValue.set(token);
    this.emailValue.set(email);
  }

  clear(): void {
    write(TOKEN_KEY, null);
    write(EMAIL_KEY, null);
    this.tokenValue.set(null);
    this.emailValue.set(null);
  }
}

// Work item: TASK-087 (FEAT-020)
function read(key: string): string | null {
  try {
    return sessionStorage.getItem(key);
  } catch {
    return null;
  }
}

// Work item: TASK-087 (FEAT-020)
function write(key: string, value: string | null): void {
  try {
    if (value === null) {
      sessionStorage.removeItem(key);
    } else {
      sessionStorage.setItem(key, value);
    }
  } catch {
    // Storage blocked: the session lives only in memory for this page.
  }
}
