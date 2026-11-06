import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors, HttpErrorResponse } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authService: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        {
          provide: Router,
          useValue: { navigate: () => {} },
        },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authService = TestBed.inject(AuthService);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should attach Authorization Bearer header when token is available', () => {
    authService.accessToken.set('valid-sample-jwt');

    http.get('/api/v1/accounts').subscribe();

    const req = httpMock.expectOne('/api/v1/accounts');
    expect(req.request.headers.get('Authorization')).toBe('Bearer valid-sample-jwt');
    req.flush([]);
  });

  it('should not attach Authorization header on login or register endpoints', () => {
    authService.accessToken.set('valid-sample-jwt');

    http.post('/api/v1/auth/login', { email: 'a@b.com', password: 'pwd' }).subscribe();

    const req = httpMock.expectOne('/api/v1/auth/login');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('should trigger single-flight token refresh and retry request on 401', () => {
    authService.accessToken.set('expired-jwt');
    const refreshSpy = vi.spyOn(authService, 'refreshToken').mockReturnValue(
      of({ accessToken: 'new-refreshed-jwt', expiresIn: 900 })
    );

    let finalResponse: any = null;
    http.get('/api/v1/accounts').subscribe((res) => {
      finalResponse = res;
    });

    // Initial request fails with 401
    const req1 = httpMock.expectOne('/api/v1/accounts');
    expect(req1.request.headers.get('Authorization')).toBe('Bearer expired-jwt');
    req1.flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(refreshSpy).toHaveBeenCalled();

    // Retried request with new token
    const req2 = httpMock.expectOne('/api/v1/accounts');
    expect(req2.request.headers.get('Authorization')).toBe('Bearer new-refreshed-jwt');
    req2.flush([{ id: 'acc-1' }]);

    expect(finalResponse).toEqual([{ id: 'acc-1' }]);
  });
});
