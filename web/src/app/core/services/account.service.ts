import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AccountDto,
  AccountBalanceDto,
  OpenAccountRequest,
  BeneficiaryLookupDto,
} from '../models/account.models';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private http = inject(HttpClient);

  public getAccounts(): Observable<AccountDto[]> {
    return this.http.get<AccountDto[]>('/api/v1/accounts');
  }

  public getAccountById(id: string): Observable<AccountDto> {
    return this.http.get<AccountDto>(`/api/v1/accounts/${id}`);
  }

  public getAccountBalance(id: string): Observable<AccountBalanceDto> {
    return this.http.get<AccountBalanceDto>(`/api/v1/accounts/${id}/balance`);
  }

  public openAccount(request: OpenAccountRequest): Observable<AccountDto> {
    return this.http.post<AccountDto>('/api/v1/accounts', request);
  }

  public closeAccount(id: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`/api/v1/accounts/${id}/close`, {});
  }

  public lookupBeneficiary(accountNumber: string): Observable<BeneficiaryLookupDto> {
    const params = new HttpParams().set('accountNumber', accountNumber);
    return this.http.get<BeneficiaryLookupDto>('/api/v1/accounts/lookup', { params });
  }

  public downloadStatementCsv(id: string, fromDate?: string, toDate?: string): Observable<Blob> {
    let params = new HttpParams().set('format', 'csv');
    if (fromDate) params = params.set('from', fromDate);
    if (toDate) params = params.set('to', toDate);

    return this.http.get(`/api/v1/accounts/${id}/statement`, {
      params,
      responseType: 'blob',
    });
  }
}
