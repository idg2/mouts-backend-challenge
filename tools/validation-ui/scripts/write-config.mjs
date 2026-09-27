// Work item: TASK-086 (FEAT-020)
// Writes public/config.json for `npm start` from the environment; the container writes its own at startup.
import { writeFileSync } from 'node:fs';

const missing = ['UI_LOGIN_EMAIL', 'UI_LOGIN_PASSWORD'].filter((name) => !process.env[name]);
if (missing.length > 0) {
  console.error(`write-config: set ${missing.join(' and ')} (the seeded administrator of the API you point API_URL at).`);
  process.exit(1);
}

const config = { loginHint: { email: process.env.UI_LOGIN_EMAIL, password: process.env.UI_LOGIN_PASSWORD } };
writeFileSync(new URL('../public/config.json', import.meta.url), `${JSON.stringify(config, null, 2)}\n`);
