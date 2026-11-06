import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CustomerSummaryDto,
  CustomerDetailDto,
  AuditLogDto,
  ReconciliationRunDto,
  TrialBalanceDto,
  AdminDashboardKpiDto,
} from '../models/admin.models';
import { TransactionItemDto, KeysetPagedResult } from '../models/transaction.models';

export interface CustomerSearchParams {
  search?: string;
  status?: string;
  limit?: number;
  offset?: number;
}

export interface AuditLogSearchParams {
  actor?: string;
  action?: string;
  entityType?: string;
  from?: string;
  to?: string;
  cursor?: string;
  limit?: number;
}

@Injectable({
  providedIn: 'root',
})
export class AdminService {
  private http = inject(HttpClient);

  public getDashboardKpis(): Observable<AdminDashboardKpiDto> {
    return this.http.get<AdminDashboardKpiDto>('/api/v1/admin/dashboard');
  }

  public getCustomers(
    params?: CustomerSearchParams
  ): Observable<{ items: CustomerSummaryDto[]; totalCount: number }> {
    let httpParams = new HttpParams();
    if (params?.search) httpParams = httpParams.set('search', params.search);
    if (params?.status) httpParams = httpParams.set('status', params.status);
    if (params?.limit) httpParams = httpParams.set('limit', params.limit.toString());
    if (params?.offset) httpParams = httpParams.set('offset', params.offset.toString());

    return this.http.get<{ items: CustomerSummaryDto[]; totalCount: number }>(
      '/api/v1/admin/customers',
      { params: httpParams }
    );
  }

  public getCustomerById(id: string): Observable<CustomerDetailDto> {
    return this.http.get<CustomerDetailDto>(`/api/v1/admin/customers/${id}`);
  }

  public suspendCustomer(id: string, reason: string): Observable<{ message: string; status: string }> {
    return this.http.post<{ message: string; status: string }>(
      `/api/v1/admin/customers/${id}/suspend`,
      { reason }
    );
  }

  public reactivateCustomer(id: string): Observable<{ message: string; status: string }> {
    return this.http.post<{ message: string; status: string }>(
      `/api/v1/admin/customers/${id}/reactivate`,
      {}
    );
  }

  public freezeAccount(id: string, reason: string): Observable<{ message: string; status: string }> {
    return this.http.post<{ message: string; status: string }>(
      `/api/v1/admin/accounts/${id}/freeze`,
      { reason }
    );
  }

  public unfreezeAccount(id: string): Observable<{ message: string; status: string }> {
    return this.http.post<{ message: string; status: string }>(
      `/api/v1/admin/accounts/${id}/unfreeze`,
      {}
    );
  }

  public reverseTransaction(
    id: string,
    reason: string
  ): Observable<{ message: string; compensatingTransactionId: string; reference: string }> {
    return this.http.post<{ message: string; compensatingTransactionId: string; reference: string }>(
      `/api/v1/admin/transactions/${id}/reversal`,
      { reason }
    );
  }

  public getTransactions(params?: {
    cursor?: string;
    limit?: number;
    type?: string;
    from?: string;
    to?: string;
  }): Observable<KeysetPagedResult<TransactionItemDto>> {
    let httpParams = new HttpParams();
    if (params?.cursor) httpParams = httpParams.set('cursor', params.cursor);
    if (params?.limit) httpParams = httpParams.set('limit', params.limit.toString());
    if (params?.type) httpParams = httpParams.set('type', params.type);
    if (params?.from) httpParams = httpParams.set('from', params.from);
    if (params?.to) httpParams = httpParams.set('to', params.to);

    return this.http.get<KeysetPagedResult<TransactionItemDto>>('/api/v1/admin/transactions', {
      params: httpParams,
    });
  }

  public getAuditLogs(
    params?: AuditLogSearchParams
  ): Observable<KeysetPagedResult<AuditLogDto>> {
    let httpParams = new HttpParams();
    if (params?.actor) httpParams = httpParams.set('actor', params.actor);
    if (params?.action) httpParams = httpParams.set('action', params.action);
    if (params?.entityType) httpParams = httpParams.set('entityType', params.entityType);
    if (params?.from) httpParams = httpParams.set('from', params.from);
    if (params?.to) httpParams = httpParams.set('to', params.to);
    if (params?.cursor) httpParams = httpParams.set('cursor', params.cursor);
    if (params?.limit) httpParams = httpParams.set('limit', params.limit.toString());

    return this.http.get<KeysetPagedResult<AuditLogDto>>('/api/v1/admin/audit-logs', {
      params: httpParams,
    });
  }

  public getTrialBalance(): Observable<TrialBalanceDto> {
    return this.http.get<TrialBalanceDto>('/api/v1/admin/ledger/trial-balance');
  }

  public triggerReconciliation(asOfDate?: string): Observable<ReconciliationRunDto> {
    return this.http.post<ReconciliationRunDto>('/api/v1/admin/reconciliation/runs', {
      asOfDate,
    });
  }

  public getReconciliationRuns(): Observable<ReconciliationRunDto[]> {
    return this.http.get<ReconciliationRunDto[]>('/api/v1/admin/reconciliation/runs');
  }
}
