import { describe, expect, it } from 'vitest';
import { cpf } from './fixtures';

// Work item: TASK-090 (FEAT-020)
function checkDigit(digits: number[], length: number): number {
  const sum = digits.slice(0, length).reduce((total, digit, index) => total + digit * (length + 1 - index), 0);
  const remainder = sum % 11;
  return remainder < 2 ? 0 : 11 - remainder;
}

// Work item: TASK-090 (FEAT-020)
describe('cpf', () => {
  it('generates 11 digits with valid check digits and not all equal', () => {
    for (let run = 0; run < 50; run++) {
      const value = cpf();
      const digits = [...value].map(Number);
      expect(value).toMatch(/^\d{11}$/);
      expect(digits[9]).toBe(checkDigit(digits, 9));
      expect(digits[10]).toBe(checkDigit(digits, 10));
      expect(new Set(digits.slice(0, 9)).size).toBeGreaterThan(1);
    }
  });
});
