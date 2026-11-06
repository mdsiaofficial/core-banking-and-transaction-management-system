import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ToastComponent } from '../../../shared/components/toast/toast.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ToastComponent],
  template: `
    <div class="min-h-screen flex items-center justify-center p-4 bg-slate-50 dark:bg-slate-950 transition-colors">
      <div class="w-full max-w-lg">
        <!-- Logo & Title -->
        <div class="text-center mb-8">
          <div class="inline-flex w-14 h-14 rounded-2xl bg-gradient-to-tr from-sky-600 to-indigo-600 items-center justify-center text-white shadow-xl shadow-sky-500/25 mb-4">
            <svg class="w-8 h-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z" />
            </svg>
          </div>
          <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Open Customer Account</h1>
          <p class="text-sm text-slate-500 dark:text-slate-400 mt-1">
            Join ApexCore Banking to start managing accounts with financial integrity
          </p>
        </div>

        <!-- Registration Card -->
        <div class="bg-white dark:bg-slate-900 p-8 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-xl">
          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-4">
            <div>
              <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
                Full Legal Name
              </label>
              <input
                type="text"
                formControlName="fullName"
                placeholder="Rahim Ahmed"
                class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/50 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 transition-all"
              />
              @if (form.get('fullName')?.touched && form.get('fullName')?.invalid) {
                <span class="text-xs text-rose-500 mt-1 block">Full legal name is required</span>
              }
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
                  Email Address
                </label>
                <input
                  type="email"
                  formControlName="email"
                  placeholder="rahim@example.com"
                  class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/50 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 transition-all"
                />
                @if (form.get('email')?.touched && form.get('email')?.invalid) {
                  <span class="text-xs text-rose-500 mt-1 block">Valid email required</span>
                }
              </div>

              <div>
                <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
                  Phone Number
                </label>
                <input
                  type="tel"
                  formControlName="phone"
                  placeholder="+8801700000000"
                  class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/50 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 transition-all"
                />
              </div>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
                  Date of Birth
                </label>
                <input
                  type="date"
                  formControlName="dateOfBirth"
                  class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/50 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 transition-all"
                />
              </div>

              <div>
                <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
                  Password (min 12 chars)
                </label>
                <input
                  type="password"
                  formControlName="password"
                  placeholder="••••••••••••"
                  class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/50 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 transition-all"
                />
                @if (form.get('password')?.touched && form.get('password')?.invalid) {
                  <span class="text-xs text-rose-500 mt-1 block">Password must be at least 12 characters</span>
                }
              </div>
            </div>

            <div>
              <label class="block text-xs font-semibold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-1.5">
                Residential Address
              </label>
              <textarea
                formControlName="address"
                rows="2"
                placeholder="House #12, Road #4, Gulshan-1, Dhaka"
                class="w-full px-4 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/50 text-slate-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-sky-500 transition-all"
              ></textarea>
            </div>

            <div class="p-3 rounded-xl bg-sky-50 dark:bg-sky-950/50 text-xs text-sky-800 dark:text-sky-300 border border-sky-100 dark:border-sky-900/40">
              ℹ️ After registering, a verification token is sent to your inbox. In local development, check <strong>Mailpit</strong> at <code>http://localhost:8025</code>.
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
                <span>Creating Profile...</span>
              } @else {
                <span>Register & Proceed to Verification</span>
              }
            </button>
          </form>

          <div class="mt-6 text-center text-xs text-slate-500 dark:text-slate-400">
            Already registered?
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
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private notification = inject(NotificationService);
  private router = inject(Router);

  public loading = signal(false);

  public form = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(12)]],
    phone: ['+8801712345678', [Validators.required]],
    dateOfBirth: ['1992-05-15', [Validators.required]],
    address: ['House 10, Road 5, Banani, Dhaka-1213', [Validators.required]],
  });

  public onSubmit(): void {
    if (this.form.invalid || this.loading()) return;

    this.loading.set(true);
    const val = this.form.value;

    this.authService
      .register({
        fullName: val.fullName!,
        email: val.email!,
        password: val.password!,
        phone: val.phone!,
        dateOfBirth: val.dateOfBirth!,
        address: val.address!,
      })
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          this.notification.success(
            'Registration Received',
            'Your customer profile has been registered. Please enter the verification token sent to your email.'
          );
          this.router.navigate(['/auth/verify-email'], {
            queryParams: { email: val.email },
          });
        },
        error: () => {
          this.loading.set(false);
        },
      });
  }
}
