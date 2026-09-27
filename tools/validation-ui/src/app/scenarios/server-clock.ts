// Work item: TASK-088 (FEAT-020)
/**
 * Estimates the API's clock from its Date header (1 s resolution; nginx passes it through): the estimate is at most about 1 s behind the
 * server, never ahead, so waiting until the estimate passes an instant guarantees the server passed it too.
 */
export class ServerClock {
  private offsetMs = 0;

  observe(dateHeader: string | null, receivedAt: number = Date.now()): void {
    if (!dateHeader) {
      return;
    }
    const server = Date.parse(dateHeader);
    if (!Number.isNaN(server)) {
      this.offsetMs = server - receivedAt;
    }
  }

  now(): number {
    return Date.now() + this.offsetMs;
  }
}
