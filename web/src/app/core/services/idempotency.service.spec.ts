import { TestBed } from '@angular/core/testing';
import { IdempotencyService } from './idempotency.service';

describe('IdempotencyService', () => {
  let service: IdempotencyService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(IdempotencyService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should generate a valid UUIDv4 key for an action', () => {
    const key = service.getOrCreateKey('test_action');
    expect(key).toBeTruthy();
    expect(key).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i);
  });

  it('should reuse the same key for the same action tag until refreshed', () => {
    const key1 = service.getOrCreateKey('transfer_1');
    const key2 = service.getOrCreateKey('transfer_1');
    expect(key1).toBe(key2);

    const refreshed = service.refreshKey('transfer_1');
    expect(refreshed).not.toBe(key1);
    expect(service.getOrCreateKey('transfer_1')).toBe(refreshed);
  });

  it('should track in-flight actions to prevent double clicks', () => {
    const key = 'flight_key_1';
    expect(service.isInFlight(key)).toBe(false);

    service.markInFlight(key);
    expect(service.isInFlight(key)).toBe(true);

    service.clearInFlight(key);
    expect(service.isInFlight(key)).toBe(false);
  });
});
