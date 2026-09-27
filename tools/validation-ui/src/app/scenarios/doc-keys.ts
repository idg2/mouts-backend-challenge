import { HttpExchange } from './scenario';

// Work item: TASK-094 (FEAT-020)
/** The documented flow (topic key in docs/) each API call of the scenarios runs; diagnostics calls have none. */
const TOPICS: { method: string; path: RegExp; key: string }[] = [
  { method: 'POST', path: /^\/api\/customers$/, key: 'CUS-CRT' },
  { method: 'POST', path: /^\/api\/branches$/, key: 'BRN-CRT' },
  { method: 'POST', path: /^\/api\/products$/, key: 'PRD-CRT' },
  { method: 'POST', path: /^\/api\/sales$/, key: 'SAL-CRT' },
  { method: 'PUT', path: /^\/api\/sales\/[^/?]+$/, key: 'SAL-UPD' },
  { method: 'DELETE', path: /^\/api\/sales\/[^/?]+$/, key: 'SAL-DEL' },
  { method: 'GET', path: /^\/api\/sales\/[^/?]+$/, key: 'SAL-GET' },
  { method: 'POST', path: /^\/api\/discount-policies$/, key: 'DSC-CRT' },
  { method: 'POST', path: /^\/api\/discount-policies\/disable$/, key: 'DSC-DIS' },
];

// Work item: TASK-094 (FEAT-020)
/** The topic keys of the calls, each once, in call order. */
export function topicKeysOf(exchanges: readonly HttpExchange[]): string[] {
  const keys: string[] = [];
  for (const exchange of exchanges) {
    const key = TOPICS.find((topic) => topic.method === exchange.method && topic.path.test(exchange.url))?.key;
    if (key && !keys.includes(key)) {
      keys.push(key);
    }
  }
  return keys;
}

// Work item: TASK-094 (FEAT-020)
/** True when the run's trace holds the key: a step key exactly, a topic key through any of its steps. */
export function isTraced(key: string, traced: ReadonlySet<string>): boolean {
  if (traced.has(key)) {
    return true;
  }
  const prefix = `${key}-`;
  return key.split('-').length === 2 && [...traced].some((candidate) => candidate.startsWith(prefix));
}
