import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError, of } from 'rxjs';
import {
  UserProfile,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
  VerifyEmailRequest,
  VerifyEmailResponse,
  RefreshResponse,
} from '../models/auth.models';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly TOKEN_KEY = 'apex_access_token';
  private readonly USER_KEY = 'apex_user_profile';

  public accessToken = signal<string | null>(this.getStoredToken());
  public currentUser = signal<UserProfile | null>(this.getStoredUser());

  public isAuthenticated = computed(() => !!this.accessToken() && !!this.currentUser());
  public isCustomer = computed(() => this.hasRole('Customer'));
  public isAdmin = computed(() => this.hasRole('Admin'));
  public isAuditor = computed(() => this.hasRole('Auditor'));

  private getStoredToken(): string | null {
    if (typeof sessionStorage !== 'undefined') {
      return sessionStorage.getItem(this.TOKEN_KEY);
    }
    return null;
  }

  private getStoredUser(): UserProfile | null {
    if (typeof sessionStorage !== 'undefined') {
      const raw = sessionStorage.getItem(this.USER_KEY);
      if (raw) {
        try {
          return JSON.parse(raw);
        } catch {
          return null;
        }
      }
    }
    return null;
  }

  public hasRole(role: string): boolean {
    const user = this.currentUser();
    return !!user?.roles?.includes(role as any);
  }

  public register(payload: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>('/api/v1/auth/register', payload);
  }

  public verifyEmail(payload: VerifyEmailRequest): Observable<VerifyEmailResponse> {
    return this.http.post<VerifyEmailResponse>('/api/v1/auth/verify-email', payload);
  }

  public login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/v1/auth/login', payload).pipe(
      tap((res) => {
        this.setSession(res.accessToken, res.user);
      })
    );
  }

  public refreshToken(): Observable<RefreshResponse> {
    return this.http.post<RefreshResponse>('/api/v1/auth/refresh', {}).pipe(
      tap((res) => {
        if (res.accessToken) {
          this.accessToken.set(res.accessToken);
          if (typeof sessionStorage !== 'undefined') {
            sessionStorage.setItem(this.TOKEN_KEY, res.accessToken);
          }
        }
      }),
      catchError((err) => {
        this.clearSession();
        return throwError(() => err);
      })
    );
  }

  public fetchCurrentUser(): Observable<UserProfile> {
    return this.http.get<UserProfile>('/api/v1/me').pipe(
      tap((user) => {
        this.currentUser.set(user);
        if (typeof sessionStorage !== 'undefined') {
          sessionStorage.setItem(this.USER_KEY, JSON.stringify(user));
        }
      })
    );
  }

  public setSession(token: string, user: UserProfile): void {
    this.accessToken.set(token);
    this.currentUser.set(user);
    if (typeof sessionStorage !== 'undefined') {
      sessionStorage.setItem(this.TOKEN_KEY, token);
      sessionStorage.setItem(this.USER_KEY, JSON.stringify(user));
    }
  }

  public logout(): void {
    // Fire and forget backend logout
    this.http.post('/api/v1/auth/logout', {}).subscribe({
      next: () => {},
      error: () => {},
    });
    this.clearSession();
    this.router.navigate(['/auth/login']);
  }

  public clearSession(): void {
    this.accessToken.set(null);
    this.currentUser.set(null);
    if (typeof sessionStorage !== 'undefined') {
      sessionStorage.removeItem(this.TOKEN_KEY);
      sessionStorage.removeItem(this.USER_KEY);
    }
  }
}
