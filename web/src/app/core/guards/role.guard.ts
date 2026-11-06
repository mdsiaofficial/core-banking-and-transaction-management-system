import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard = (allowedRoles: string[]): CanActivateFn => {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
      return router.createUrlTree(['/auth/login']);
    }

    const hasPermission = allowedRoles.some((role) => authService.hasRole(role));
    if (hasPermission) {
      return true;
    }

    // Redirect to customer dashboard or home if unauthorized for this role
    return router.createUrlTree(['/customer/dashboard']);
  };
};
