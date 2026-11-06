import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ToastComponent } from '../../../shared/components/toast/toast.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ToastComponent],
  template: `
    <div class="min-h-screen flex items-center justify-center p-4 bg-slate-50 dark:bg-slate-950 transition-colors">
      <div class="w-full max-w-md">
        <!-- Logo & Title -->
        <div class="text-center mb-8">
          <div class="inline-flex w-14 h-14 rounded-2xl bg-gradient-to-tr from-sky-600 to-indigo-600 items-center justify-center text-white shadow-xl shadow-sky-500/25 mb-4">
            <svg class="w-8 h-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
            </svg>
          </div>
          <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Sign in to ApexCore</h1>
          <p class="text-sm text-slate-500 dark:text-slate-400 mt-1">
            Enterprise Financial Core & Double-Entry Ledger
          </p>
        </div>

        <!-- Login Card -->
        <div class="bg-white dark:bg-slate-900 p-8 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-xl">
          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-5">
            <div>
              <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
                Email Address
              </label>
              <input
                type="email"
                formControlName="email"
                placeholder="name@apexbank.local"
                class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/50 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 transition-all"
                [class.border-rose-500]="form.get('email')?.touched && form.get('email')?.invalid"
              />
              @if (form.get('email')?.touched && form.get('email')?.invalid) {
                <span class="text-xs text-rose-500 mt-1 block">Valid email address required</span>
              }
            </div>

            <div>
              <div class="flex items-center justify-between mb-1.5">
                <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300">
                  Password
                </label>
                <a routerLink="/auth/verify-email" class="text-xs text-sky-600 dark:text-sky-400 hover:underline">
                  Verify Email?
                </a>
              </div>
              <input
                type="password"
                formControlName="password"
                placeholder="••••••••••••"
                class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/50 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 transition-all"
                [class.border-rose-500]="form.get('password')?.touched && form.get('password')?.invalid"
              />
              @if (form.get('password')?.touched && form.get('password')?.invalid) {
                <span class="text-xs text-rose-500 mt-1 block">Password is required (min 8 characters)</span>
              }
            </div>

            <button
              type="submit"
              [disabled]="form.invalid || loading()"
              class="w-full py-3 px-4 rounded-xl text-sm font-semibold text-white bg-sky-600 hover:bg-sky-500 active:bg-sky-700 shadow-lg shadow-sky-600/25 disabled:opacity-50 disabled:cursor-not-allowed transition-all flex items-center justify-center gap-2"
            >
              @if (loading()) {
                <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <span>Authenticating...</span>
              } @else {
                <span>Sign In Securely</span>
              }
            </button>
          </form>

          <!-- Quick Test Credentials -->
          <div class="mt-6 pt-6 border-t border-slate-100 dark:border-slate-800">
            <div class="text-[11px] font-semibold uppercase tracking-wider text-slate-400 dark:text-slate-500 mb-2">
              Demo Fast Fill
            </div>
            <div class="grid grid-cols-3 gap-2">
              <button
                type="button"
                (click)="fillCredentials('customer@apexbank.local', 'Pass12345678!')"
                class="px-2 py-1.5 text-xs text-slate-600 dark:text-slate-300 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 rounded-lg transition-colors text-center font-medium"
              >
                Customer
              </button>
              <button
                type="button"
                (click)="fillCredentials('admin@apexbank.local', 'Pass12345678!')"
                class="px-2 py-1.5 text-xs text-sky-700 dark:text-sky-300 bg-sky-50 dark:bg-sky-950/60 hover:bg-sky-100 dark:hover:bg-sky-900/60 rounded-lg transition-colors text-center font-medium"
              >
                Admin
              </button>
              <button
                type="button"
                (click)="fillCredentials('auditor@apexbank.local', 'Pass12345678!')"
                class="px-2 py-1.5 text-xs text-emerald-700 dark:text-emerald-300 bg-emerald-50 dark:bg-emerald-950/60 hover:bg-emerald-100 dark:hover:bg-emerald-900/60 rounded-lg transition-colors text-center font-medium"
              >
                Auditor
              </button>
            </div>
          </div>

          <!-- Register Link -->
          <div class="mt-6 text-center text-xs text-slate-500 dark:text-slate-400">
            Don't have an account yet?
            <a routerLink="/auth/register" class="text-sky-600 dark:text-sky-400 font-semibold hover:underline">
              Register as Customer
            </a>
          </div>
        </div>
      </div>
      <app-toast></app-toast>
    </div>
  `,
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private notification = inject(NotificationService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  public loading = signal(false);

  public form = this.fb.group({
    email: ['customer@apexbank.local', [Validators.required, Validators.email]],
    password: ['Pass12345678!', [Validators.required, Validators.minLength(8)]],
  });

  public fillCredentials(email: string, pass: string): void {
    this.form.patchValue({ email, password: pass });
  }

  public onSubmit(): void {
    if (this.form.invalid || this.loading()) return;

    this.loading.set(true);
    const { email, password } = this.form.value;

    this.authService
      .login({ email: email!, password: password! })
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          this.notification.success('Authentication Successful', `Welcome back, ${res.user.fullName}!`);

          const returnUrl = this.route.snapshot.queryParams['returnUrl'];
          if (returnUrl) {
            this.router.navigateByUrl(returnUrl);
          } else if (res.user.roles.includes('Admin') || res.user.roles.includes('Auditor')) {
            this.router.navigate(['/admin/dashboard']);
          } else {
            this.router.navigate(['/customer/dashboard']);
          }
        },
        error: (err) => {
          this.loading.set(false);
          // Handled by ErrorInterceptor
        },
      });
  }
}
