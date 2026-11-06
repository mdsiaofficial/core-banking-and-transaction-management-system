import { Component, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <aside
      class="w-64 bg-slate-50/70 dark:bg-slate-900/60 border-r border-slate-200/80 dark:border-slate-800 flex flex-col justify-between py-6 px-4 shrink-0 transition-colors"
    >
      <div class="space-y-6">
        @if (mode() === 'customer') {
          <div>
            <div class="px-3 text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 mb-2">
              Banking Core
            </div>
            <nav class="space-y-1">
              <a
                routerLink="/customer/dashboard"
                routerLinkActive="bg-sky-50 dark:bg-sky-950/60 text-sky-600 dark:text-sky-400 font-semibold border-r-2 border-sky-600"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all"
              >
                <!-- Dashboard Icon -->
                <svg class="w-5 h-5 opacity-70" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
                </svg>
                <span>Dashboard</span>
              </a>

              <a
                routerLink="/customer/accounts"
                routerLinkActive="bg-sky-50 dark:bg-sky-950/60 text-sky-600 dark:text-sky-400 font-semibold border-r-2 border-sky-600"
                [routerLinkActiveOptions]="{ exact: true }"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all"
              >
                <!-- Accounts Icon -->
                <svg class="w-5 h-5 opacity-70" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
                </svg>
                <span>My Accounts</span>
              </a>

              <a
                routerLink="/customer/accounts/open"
                routerLinkActive="bg-sky-50 dark:bg-sky-950/60 text-sky-600 dark:text-sky-400 font-semibold border-r-2 border-sky-600"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all"
              >
                <!-- Open Account Wizard -->
                <svg class="w-5 h-5 opacity-70" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v3m0 0v3m0-3h3m-3 0H9m12 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>Open New Account</span>
              </a>
            </nav>
          </div>

          <div>
            <div class="px-3 text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 mb-2">
              Money Movement
            </div>
            <nav class="space-y-1">
              <a
                routerLink="/customer/payments/deposit"
                routerLinkActive="bg-sky-50 dark:bg-sky-950/60 text-sky-600 dark:text-sky-400 font-semibold border-r-2 border-sky-600"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all"
              >
                <svg class="w-5 h-5 opacity-70" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                </svg>
                <span>Cash Deposit</span>
              </a>

              <a
                routerLink="/customer/payments/withdraw"
                routerLinkActive="bg-sky-50 dark:bg-sky-950/60 text-sky-600 dark:text-sky-400 font-semibold border-r-2 border-sky-600"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all"
              >
                <svg class="w-5 h-5 opacity-70" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
                </svg>
                <span>Cash Withdrawal</span>
              </a>

              <a
                routerLink="/customer/payments/transfer"
                routerLinkActive="bg-sky-50 dark:bg-sky-950/60 text-sky-600 dark:text-sky-400 font-semibold border-r-2 border-sky-600"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all"
              >
                <svg class="w-5 h-5 opacity-70" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
                </svg>
                <span>Intra-Bank Transfer</span>
              </a>

              <a
                routerLink="/customer/transactions"
                routerLinkActive="bg-sky-50 dark:bg-sky-950/60 text-sky-600 dark:text-sky-400 font-semibold border-r-2 border-sky-600"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all"
              >
                <svg class="w-5 h-5 opacity-70" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
                </svg>
                <span>Ledger History & CSV</span>
              </a>
            </nav>
          </div>
        } @else {
          <!-- Admin View Navigation -->
          <div>
            <div class="px-3 text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 mb-2">
              Back-Office Core
            </div>
            <nav class="space-y-1">
              <a
                routerLink="/admin/dashboard"
                routerLinkActive="bg-sky-50 dark:bg-sky-950/60 text-sky-600 dark:text-sky-400 font-semibold border-r-2 border-sky-600"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all"
              >
                <svg class="w-5 h-5 opacity-70" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                </svg>
                <span>Admin Overview</span>
              </a>

              <a
                routerLink="/admin/customers"
                routerLinkActive="bg-sky-50 dark:bg-sky-950/60 text-sky-600 dark:text-sky-400 font-semibold border-r-2 border-sky-600"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all"
              >
                <svg class="w-5 h-5 opacity-70" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
                </svg>
                <span>Customer Registry</span>
              </a>

              <a
                routerLink="/admin/accounts"
                routerLinkActive="bg-sky-50 dark:bg-sky-950/60 text-sky-600 dark:text-sky-400 font-semibold border-r-2 border-sky-600"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all"
              >
                <svg class="w-5 h-5 opacity-70" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                </svg>
                <span>Account Freeze & Controls</span>
              </a>

              <a
                routerLink="/admin/reversals"
                routerLinkActive="bg-sky-50 dark:bg-sky-950/60 text-sky-600 dark:text-sky-400 font-semibold border-r-2 border-sky-600"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all"
              >
                <svg class="w-5 h-5 opacity-70" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
                <span>Transaction Reversals</span>
              </a>

              <a
                routerLink="/admin/audit-logs"
                routerLinkActive="bg-sky-50 dark:bg-sky-950/60 text-sky-600 dark:text-sky-400 font-semibold border-r-2 border-sky-600"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all"
              >
                <svg class="w-5 h-5 opacity-70" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                <span>Security Audit Logs</span>
              </a>

              <a
                routerLink="/admin/reconciliation"
                routerLinkActive="bg-sky-50 dark:bg-sky-950/60 text-sky-600 dark:text-sky-400 font-semibold border-r-2 border-sky-600"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800/60 transition-all"
              >
                <svg class="w-5 h-5 opacity-70" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 6l3 1m0 0l-3 9a5.002 5.002 0 006.001 0M6 7l3 9M6 7l6-2m6 2l3-1m-3 1l-3 9a5.002 5.002 0 006.001 0M18 7l3 9m-3-9l-6-2m0-2v2m0 16V5m0 16H9m3 0h3" />
                </svg>
                <span>Reconciliation & Trial Balance</span>
              </a>
            </nav>
          </div>
        }
      </div>

      <!-- Footer Info -->
      <div class="pt-4 border-t border-slate-200 dark:border-slate-800">
        <div class="p-3 rounded-xl bg-slate-100 dark:bg-slate-800/70 text-xs text-slate-500 dark:text-slate-400">
          <div class="flex items-center gap-2 font-semibold text-slate-700 dark:text-slate-200">
            <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
            Ledger Immutability
          </div>
          <div class="mt-1 text-[11px] leading-tight opacity-80">
            Double-Entry ACID Engine Active
          </div>
        </div>
      </div>
    </aside>
  `,
})
export class SidebarComponent {
  mode = input<'customer' | 'admin'>('customer');
  public authService = inject(AuthService);
}
