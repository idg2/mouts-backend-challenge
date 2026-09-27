import { describe, expect, it } from 'vitest';
import { parseAppConfig } from './app-config';

// Work item: TASK-086 (FEAT-020)
describe('parseAppConfig', () => {
  it('returns the login hint', () => {
    const config = parseAppConfig({ loginHint: { email: 'admin@example.com', password: 'Adm1n@Pass' } });
    expect(config.loginHint).toEqual({ email: 'admin@example.com', password: 'Adm1n@Pass' });
  });

  it.each([
    [{}, 'loginHint.email'],
    [{ loginHint: { password: 'x' } }, 'loginHint.email'],
    [{ loginHint: { email: '', password: 'x' } }, 'loginHint.email'],
    [{ loginHint: { email: 'a@b.c' } }, 'loginHint.password'],
    [null, 'loginHint.email'],
  ])('names the missing field of %j', (raw, field) => {
    expect(() => parseAppConfig(raw)).toThrow(field);
  });
});
