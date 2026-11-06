import { inject } from '@angular/core';
import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpErrorResponse,
} from '@angular/common/http';
import { NotificationService } from '../services/notification.service';
import { ProblemDetails } from '../models/problem-details.model';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const notificationService = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Skip 401 errors handled by AuthInterceptor or auth status checks
      if (error.status === 401 && !req.url.includes('/api/v1/auth/login')) {
        return throwError(() => error);
      }

      if (error.error && typeof error.error === 'object') {
        const problem = error.error as ProblemDetails;
        notificationService.fromProblemDetails(problem);
      } else {
        const title = error.statusText || 'Network Error';
        const detail = error.message || 'An error occurred while contacting the banking core.';
        notificationService.error(title, detail);
      }

      return throwError(() => error);
    })
  );
};
