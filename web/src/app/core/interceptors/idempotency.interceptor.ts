import { inject } from '@angular/core';
import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpErrorResponse,
} from '@angular/common/http';
import { IdempotencyService } from '../services/idempotency.service';
import { generateUuidV4 } from '../../shared/utils/uuid.util';
import { finalize, throwError } from 'rxjs';

const MONEY_MUTATING_PATTERNS = [
  /\/api\/v1\/deposits/,
  /\/api\/v1\/withdrawals/,
  /\/api\/v1\/transfers/,
  /\/api\/v1\/admin\/transactions\/[^/]+\/reversal/,
];

function isMoneyMutatingRequest(req: HttpRequest<unknown>): boolean {
  if (req.method !== 'POST') return false;
  return MONEY_MUTATING_PATTERNS.some((pattern) => pattern.test(req.url));
}

export const idempotencyInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  if (!isMoneyMutatingRequest(req)) {
    return next(req);
  }

  const idempotencyService = inject(IdempotencyService);
  let idempotencyKey = req.headers.get('Idempotency-Key');

  if (!idempotencyKey) {
    idempotencyKey = generateUuidV4();
  }

  // Prevent double-clicks on client-side if identical key is already flying
  if (idempotencyService.isInFlight(idempotencyKey)) {
    return throwError(
      () =>
        new HttpErrorResponse({
          status: 409,
          statusText: 'Client Duplicate Submission Blocked',
          error: {
            title: 'Action In-Flight',
            detail: 'A transaction with this submission key is currently being processed. Please wait.',
            errorCode: 'REQUEST_IN_PROGRESS',
          },
        })
    );
  }

  idempotencyService.markInFlight(idempotencyKey);

  const modifiedReq = req.clone({
    setHeaders: {
      'Idempotency-Key': idempotencyKey,
    },
  });

  return next(modifiedReq).pipe(
    finalize(() => {
      if (idempotencyKey) {
        idempotencyService.clearInFlight(idempotencyKey);
      }
    })
  );
};
