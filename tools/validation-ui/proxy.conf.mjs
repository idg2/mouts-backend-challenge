// Work item: TASK-086 (FEAT-020)
// `npm start` forwards /api to the API at API_URL, so the browser never needs CORS.
const target = process.env.API_URL;
if (!target) {
  throw new Error('API_URL is not set. Point it at the running API, for example http://localhost:8080 for make debug-up.');
}

export default { '/api': { target, secure: false, changeOrigin: true } };
