import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

// Work item: TASK-087 (FEAT-020)
/** API bodies are read loosely by the scenarios; each scenario checks the fields it relies on. */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type Json = any;

// Work item: TASK-087 (FEAT-020)
/** One HTTP answer: status, parsed body, the Date header, and the elapsed milliseconds. */
export interface ApiResult {
  status: number;
  body: Json;
  date: string | null;
  ms: number;
}

// Work item: TASK-087 (FEAT-020)
/** Sends requests through HttpClient and resolves every HTTP answer, errors included; only network failures reject. */
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);

  async send(method: string, url: string, body?: unknown, headers?: Record<string, string>): Promise<ApiResult> {
    const started = performance.now();
    try {
      const response = await firstValueFrom(this.http.request(method, url, { body, headers, observe: 'response' }));
      return { status: response.status, body: response.body, date: response.headers.get('Date'), ms: elapsed(started) };
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status !== 0) {
        return { status: error.status, body: error.error, date: error.headers.get('Date'), ms: elapsed(started) };
      }
      throw error;
    }
  }
}

// Work item: TASK-087 (FEAT-020)
function elapsed(started: number): number {
  return Math.round(performance.now() - started);
}
