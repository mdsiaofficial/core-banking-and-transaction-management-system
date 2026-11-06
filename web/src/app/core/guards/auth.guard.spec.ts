import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';

describe('authGuard', () => {
  let authService: AuthService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        {
          provide: Router,
          useValue: {
            createUrlTree: vi.fn().mockImplementation((commands, extras) => ({
              commands,
              extras,
            })),
            navigate: vi.fn(),
          },
        },
      ],
    });

    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  it('should allow access when user is authenticated', () => {
    authService.accessToken.set('mock-jwt-token');
    authService.currentUser.set({
      id: 'usr-1',
      email: 'customer@apexbank.local',
      fullName: 'Customer Test',
      status: 'ACTIVE',
      roles: ['Customer'],
    });

    const route = {} as ActivatedRouteSnapshot;
    const state = { url: '/customer/dashboard' } as RouterStateSnapshot;

    const result = TestBed.runInInjectionContext(() => authGuard(route, state));
    expect(result).toBe(true);
  });

  it('should redirect to /auth/login when user is not authenticated', () => {
    authService.clearSession();

    const route = {} as ActivatedRouteSnapshot;
    const state = { url: '/customer/dashboard' } as RouterStateSnapshot;

    TestBed.runInInjectionContext(() => authGuard(route, state));
    expect(router.createUrlTree).toHaveBeenCalledWith(['/auth/login'], {
      queryParams: { returnUrl: '/customer/dashboard' },
    });
  });
});
