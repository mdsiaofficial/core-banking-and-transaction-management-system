import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  TransactionItemDto,
  TransactionReceiptDto,
  DepositRequest,
  WithdrawalRequest,
  TransferRequest,
  KeysetPagedResult,
} from '../models/transaction.models';

export interface TransactionFilterParams {
  cursor?: string;
  limit?: number;
  direction?: 'DEBIT' | 'CREDIT';
  type?: string;
  from?: string;
  to?: string;
}

@Injectable({
  providedIn: 'root',
})
export class TransactionService {
  private http = inject(HttpClient);

  public deposit(request: DepositRequest): Observable<TransactionReceiptDto> {
    return this.http.post<TransactionReceiptDto>('/api/v1/deposits', request);
  }

  public withdraw(request: WithdrawalRequest): Observable<TransactionReceiptDto> {
    return this.http.post<TransactionReceiptDto>('/api/v1/withdrawals', request);
  }

  public transfer(request: TransferRequest): Observable<TransactionReceiptDto> {
    return this.http.post<TransactionReceiptDto>('/api/v1/transfers', request);
  }

  public getTransactionById(id: string): Observable<TransactionItemDto> {
    return this.http.get<TransactionItemDto>(`/api/v1/transactions/${id}`);
  }

  public getAccountTransactions(
    accountId: string,
    filters?: TransactionFilterParams
  ): Observable<KeysetPagedResult<TransactionItemDto>> {
    let params = new HttpParams();
    if (filters?.cursor) params = params.set('cursor', filters.cursor);
    if (filters?.limit) params = params.set('limit', filters.limit.toString());
    if (filters?.direction) params = params.set('direction', filters.direction);
    if (filters?.type) params = params.set('type', filters.type);
    if (filters?.from) params = params.set('from', filters.from);
    if (filters?.to) params = params.set('to', filters.to);

    return this.http.get<KeysetPagedResult<TransactionItemDto>>(
      `/api/v1/accounts/${accountId}/transactions`,
      { params }
    );
  }
}
