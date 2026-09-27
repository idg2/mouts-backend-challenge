import { D2, D6 } from './discount/rejection';
import { D8 } from './discount/requested-discount';
import { D7 } from './discount/split-lines';
import { D1, D3, D4, D5 } from './discount/tier';
import { O3, O4, O5 } from './outbox/change-sale';
import { O1, O2 } from './outbox/create-sale';
import { O6 } from './outbox/delete-sale';
import { O7 } from './outbox/rejected-async';
import { P2 } from './policies/branch-policy';
import { P3 } from './policies/disabled-policy';
import { P1 } from './policies/product-policy';
import { Scenario } from './scenario';

// Work item: TASK-089 (FEAT-020), TASK-090 (FEAT-020), TASK-091 (FEAT-020), TASK-092 (FEAT-020)
/** The 18 scenarios in display order: discount rules, discount policies, outbox and events. */
export const CATALOG: readonly Scenario[] = [
  D1, D2, D3, D4, D5, D6, D7, D8,
  P1, P2, P3,
  O1, O2, O3, O4, O5, O6, O7,
];
