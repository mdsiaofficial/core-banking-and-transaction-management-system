import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { idempotencyInterceptor } from './idempotency.interceptor';
import { IdempotencyService } from '../services/idempotency.service';

describe('idempotencyInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let idempotencyService: IdempotencyService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([idempotencyInterceptor])),
        provideHttpClientTesting(),
        IdempotencyService,
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    idempotencyService = TestBed.inject(IdempotencyService);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should attach Idempotency-Key header on POST /api/v1/transfers', () => {
    http.post('/api/v1/transfers', { amount: '100' }).subscribe();

    const req = httpMock.expectOne('/api/v1/transfers');
    expect(req.request.headers.has('Idempotency-Key')).toBe(true);
    const key = req.request.headers.get('Idempotency-Key');
    expect(key).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i);
    req.flush({ status: 'POSTED' });
  });

  it('should not attach Idempotency-Key header on GET requests', () => {
    http.get('/api/v1/accounts').subscribe();

    const req = httpMock.expectOne('/api/v1/accounts');
    expect(req.request.headers.has('Idempotency-Key')).toBe(false);
    req.flush([]);
  });

  it('should block concurrent identical submission if already in-flight', () => {
    const existingKey = '00000000-0000-4000-8000-000000000001';
    idempotencyService.markInFlight(existingKey);

    let errorResponse: any = null;
    http
      .post(
        '/api/v1/transfers',
        { amount: '100' },
        { headers: { 'Idempotency-Key': existingKey } }
      )
      .subscribe({
        next: () => {},
        error: (err) => {
          errorResponse = err;
        },
      });

    httpMock.expectNone('/api/v1/transfers');
    expect(errorResponse).toBeTruthy();
    expect(errorResponse.status).toBe(409);
  });
});
