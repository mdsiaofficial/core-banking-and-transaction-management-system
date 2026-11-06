import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ToastComponent } from '../../../shared/components/toast/toast.component';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ToastComponent],
  template: `
    <div class="min-h-screen flex items-center justify-center p-4 bg-slate-50 dark:bg-slate-950 transition-colors">
      <div class="w-full max-w-md">
        <!-- Header -->
        <div class="text-center mb-8">
          <div class="inline-flex w-14 h-14 rounded-2xl bg-gradient-to-tr from-sky-600 to-indigo-600 items-center justify-center text-white shadow-xl shadow-sky-500/25 mb-4">
            <svg class="w-8 h-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
          </div>
          <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Verify Your Email</h1>
          <p class="text-sm text-slate-500 dark:text-slate-400 mt-1">
            Confirm your identity before opening accounts
          </p>
        </div>

        <div class="bg-white dark:bg-slate-900 p-8 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-xl">
          <!-- Mailpit helper notice -->
          <div class="mb-6 p-4 rounded-xl bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-900/60 text-xs text-amber-900 dark:text-amber-200 space-y-2">
            <div class="font-semibold flex items-center gap-1.5">
              <svg class="w-4 h-4 text-amber-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <span>Development Environment Mailpit</span>
            </div>
            <p>
              In local Docker environment, registration emails are captured by Mailpit. Open
              <a
                href="http://localhost:8025"
                target="_blank"
                class="font-mono underline font-medium text-amber-900 dark:text-amber-100"
              >
                http://localhost:8025
              </a>
              to copy your verification token.
            </p>
          </div>

          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-4">
            <div>
              <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
                Registered Email
              </label>
              <input
                type="email"
                formControlName="email"
                placeholder="customer@apexbank.local"
                class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/50 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 transition-all"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
                Verification Token
              </label>
              <input
                type="text"
                formControlName="token"
                placeholder="Paste token or code from email"
                class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/50 text-slate-900 dark:text-white font-mono text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 transition-all"
              />
              @if (form.get('token')?.touched && form.get('token')?.invalid) {
                <span class="text-xs text-rose-500 mt-1 block">Token is required</span>
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
                <span>Validating Token...</span>
              } @else {
                <span>Verify & Activate Account</span>
              }
            </button>
          </form>

          <div class="mt-6 text-center text-xs text-slate-500 dark:text-slate-400">
            Back to
            <a routerLink="/auth/login" class="text-sky-600 dark:text-sky-400 font-semibold hover:underline">
              Sign In
            </a>
          </div>
        </div>
      </div>
      <app-toast></app-toast>
    </div>
  `,
})
export class VerifyEmailComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private notification = inject(NotificationService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  public loading = signal(false);

  public form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    token: ['', [Validators.required]],
  });

  ngOnInit(): void {
    const emailParam = this.route.snapshot.queryParams['email'];
    const tokenParam = this.route.snapshot.queryParams['token'];
    if (emailParam) this.form.patchValue({ email: emailParam });
    if (tokenParam) this.form.patchValue({ token: tokenParam });
  }

  public onSubmit(): void {
    if (this.form.invalid || this.loading()) return;

    this.loading.set(true);
    const { email, token } = this.form.value;

    this.authService.verifyEmail({ email: email!, token: token! }).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.notification.success(
          'Email Verified',
          'Your email address has been verified successfully. You can now sign in.'
        );
        this.router.navigate(['/auth/login'], {
          queryParams: { verified: 'true' },
        });
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }
}
