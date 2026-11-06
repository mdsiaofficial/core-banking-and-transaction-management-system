import { TestBed } from '@angular/core/testing';
import { NotificationService } from './notification.service';
import { ProblemDetails } from '../models/problem-details.model';

describe('NotificationService', () => {
  let service: NotificationService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(NotificationService);
    service.clearAll();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should push a success notification', () => {
    service.success('Success Title', 'Everything went well');
    const list = service.notifications();
    expect(list.length).toBe(1);
    expect(list[0].title).toBe('Success Title');
    expect(list[0].type).toBe('success');
  });

  it('should correctly format an RFC 9457 ProblemDetails object with trace ID', () => {
    const problem: ProblemDetails = {
      type: 'https://tools.ietf.org/html/rfc9457',
      title: 'Insufficient Balance',
      status: 422,
      detail: 'The requested withdrawal amount exceeds account balance.',
      errorCode: 'INSUFFICIENT_FUNDS',
      traceId: 'trace-abc-123',
    };

    service.fromProblemDetails(problem);
    const list = service.notifications();
    expect(list.length).toBe(1);
    expect(list[0].type).toBe('error');
    expect(list[0].title).toBe('Insufficient Balance');
    expect(list[0].errorCode).toBe('INSUFFICIENT_FUNDS');
    expect(list[0].traceId).toBe('trace-abc-123');
  });

  it('should dismiss a notification by ID', () => {
    const id = service.info('Notice', 'Test info');
    expect(service.notifications().length).toBe(1);

    service.dismiss(id);
    expect(service.notifications().length).toBe(0);
  });
});
