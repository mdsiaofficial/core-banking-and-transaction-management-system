import { Injectable, signal } from '@angular/core';
import { generateUuidV4 } from '../../shared/utils/uuid.util';

@Injectable({
  providedIn: 'root',
})
export class IdempotencyService {
  private activeKeys = new Map<string, string>();
  private inFlightActions = signal<Set<string>>(new Set());

  /**
   * Returns an existing idempotency key for this user action or generates a new one.
   * Reusing the same key across automatic retries guarantees server idempotency.
   */
  public getOrCreateKey(actionKey: string): string {
    let key = this.activeKeys.get(actionKey);
    if (!key) {
      key = generateUuidV4();
      this.activeKeys.set(actionKey, key);
    }
    return key;
  }

  /**
   * Resets the idempotency key for this action so the next submission gets a fresh key.
   */
  public refreshKey(actionKey: string): string {
    const key = generateUuidV4();
    this.activeKeys.set(actionKey, key);
    return key;
  }

  public clearKey(actionKey: string): void {
    this.activeKeys.delete(actionKey);
  }

  public markInFlight(actionKey: string): void {
    this.inFlightActions.update((set) => {
      const next = new Set(set);
      next.add(actionKey);
      return next;
    });
  }

  public clearInFlight(actionKey: string): void {
    this.inFlightActions.update((set) => {
      const next = new Set(set);
      next.delete(actionKey);
      return next;
    });
  }

  public isInFlight(actionKey: string): boolean {
    return this.inFlightActions().has(actionKey);
  }
}
