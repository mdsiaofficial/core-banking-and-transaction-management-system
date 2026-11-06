import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AccountService } from '../../../core/services/account.service';
import { TransactionService } from '../../../core/services/transaction.service';
import { AuthService } from '../../../core/services/auth.service';
import { AccountDto } from '../../../core/models/account.models';
import { TransactionItemDto } from '../../../core/models/transaction.models';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { MoneyUtil } from '../../../shared/utils/money.util';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card.component';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';

@Component({
  selector: 'app-customer-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MoneyPipe, StatCardComponent, BadgeComponent],
  template: `
    <div class="space-y-8 max-w-7xl mx-auto">
      <!-- Welcome Banner -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-6 rounded-2xl bg-gradient-to-r from-sky-900 to-indigo-950 text-white shadow-xl shadow-sky-950/20">
        <div>
          <span class="text-xs uppercase font-semibold tracking-wider text-sky-400">Personal Banking</span>
          <h1 class="text-2xl font-bold tracking-tight mt-1">
            Welcome back, {{ authService.currentUser()?.fullName }}
          </h1>
          <p class="text-sm text-sky-200/80 mt-1">
            Status: <span class="font-medium text-emerald-400">{{ authService.currentUser()?.status }}</span> · All accounts protected by Double-Entry Ledger.
          </p>
        </div>

        <div class="flex items-center gap-3">
          <a
            routerLink="/customer/payments/transfer"
            class="px-4 py-2.5 rounded-xl bg-sky-500 hover:bg-sky-400 text-white text-xs font-semibold shadow-md transition-all flex items-center gap-2"
          >
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
            </svg>
            <span>Send Money</span>
          </a>
          <a
            routerLink="/customer/accounts/open"
            class="px-4 py-2.5 rounded-xl bg-white/10 hover:bg-white/20 text-white text-xs font-semibold border border-white/20 transition-all flex items-center gap-2"
          >
            <span>+ Open Account</span>
          </a>
        </div>
      </div>

      <!-- Financial Metrics Summary -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        <app-stat-card
          label="Total Available Balance"
          [value]="totalAvailableBalance() | money"
          subtext="BDT (Bangladeshi Taka)"
          description="Liquid funds immediately accessible for transfers and withdrawals"
        >
          <svg icon class="w-5 h-5 text-emerald-600 dark:text-emerald-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </app-stat-card>

        <app-stat-card
          label="Total Ledger Balance"
          [value]="totalLedgerBalance() | money"
          subtext="BDT"
          description="Sum of all immutable posted journal lines across your accounts"
        >
          <svg icon class="w-5 h-5 text-sky-600 dark:text-sky-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
        </app-stat-card>

        <app-stat-card
          label="Active Accounts"
          [value]="activeAccountsCount().toString()"
          subtext="Savings & Current"
          description="Accounts currently open and transacting"
        >
          <svg icon class="w-5 h-5 text-indigo-600 dark:text-indigo-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
          </svg>
        </app-stat-card>

        <app-stat-card
          label="Ledger Invariant"
          value="100% Verified"
          subtext="Debits = Credits"
          description="Strict ACID double-entry guarantee on all postings"
        >
          <svg icon class="w-5 h-5 text-emerald-600 dark:text-emerald-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
          </svg>
        </app-stat-card>
      </div>

      <!-- Accounts Section -->
      <div class="space-y-4">
        <div class="flex items-center justify-between">
          <h2 class="text-lg font-bold text-slate-900 dark:text-white">Your Bank Accounts</h2>
          <a routerLink="/customer/accounts" class="text-xs font-semibold text-sky-600 dark:text-sky-400 hover:underline">
            Manage All Accounts →
          </a>
        </div>

        @if (loadingAccounts()) {
          <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
            <div class="h-44 rounded-2xl bg-slate-200 dark:bg-slate-800 animate-pulse"></div>
            <div class="h-44 rounded-2xl bg-slate-200 dark:bg-slate-800 animate-pulse"></div>
          </div>
        } @else if (accounts().length === 0) {
          <div class="p-8 rounded-2xl border border-dashed border-slate-300 dark:border-slate-800 text-center bg-white dark:bg-slate-900">
            <div class="w-12 h-12 mx-auto rounded-full bg-sky-50 dark:bg-sky-950 flex items-center justify-center text-sky-600 mb-3">
              <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
              </svg>
            </div>
            <h3 class="font-semibold text-slate-900 dark:text-white">No Accounts Found</h3>
            <p class="text-xs text-slate-500 mt-1 max-w-sm mx-auto">
              You haven't opened a bank account yet. Open your first Savings or Current account to start transacting.
            </p>
            <a
              routerLink="/customer/accounts/open"
              class="inline-block mt-4 px-4 py-2 text-xs font-semibold text-white bg-sky-600 hover:bg-sky-500 rounded-xl shadow-sm transition-all"
            >
              Open Account Wizard
            </a>
          </div>
        } @else {
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
            @for (acc of accounts(); track acc.id) {
              <div
                class="p-6 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm hover:shadow-md transition-all flex flex-col justify-between"
              >
                <div>
                  <div class="flex items-center justify-between mb-4">
                    <span class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                      {{ acc.accountType }} ACCOUNT
                    </span>
                    <app-badge [variant]="acc.status === 'ACTIVE' ? 'success' : acc.status === 'FROZEN' ? 'warning' : 'danger'">
                      {{ acc.status }}
                    </app-badge>
                  </div>

                  <div class="font-mono text-sm tracking-wider text-slate-700 dark:text-slate-300 mb-4 bg-slate-50 dark:bg-slate-800/60 px-3 py-1.5 rounded-lg border border-slate-200/50 dark:border-slate-700/50">
                    {{ acc.accountNumber }}
                  </div>

                  <div class="space-y-1">
                    <div class="text-xs text-slate-500 dark:text-slate-400">Available Balance</div>
                    <div class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">
                      {{ acc.availableBalance | money }}
                    </div>
                  </div>

                  <div class="mt-3 pt-3 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between text-xs text-slate-500 dark:text-slate-400">
                    <span>Ledger Balance:</span>
                    <span class="font-semibold text-slate-700 dark:text-slate-300">{{ acc.ledgerBalance | money }}</span>
                  </div>
                </div>

                <div class="mt-6 pt-4 border-t border-slate-100 dark:border-slate-800 flex items-center gap-2">
                  <a
                    [routerLink]="['/customer/accounts', acc.id]"
                    class="flex-1 text-center py-2 px-3 text-xs font-semibold text-slate-700 dark:text-slate-200 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 rounded-xl transition-all"
                  >
                    View Details
                  </a>
                  <a
                    [routerLink]="['/customer/payments/transfer']"
                    [queryParams]="{ source: acc.id }"
                    class="flex-1 text-center py-2 px-3 text-xs font-semibold text-white bg-sky-600 hover:bg-sky-500 rounded-xl shadow-sm transition-all"
                  >
                    Transfer
                  </a>
                </div>
              </div>
            }
          </div>
        }
      </div>

      <!-- Quick Action Shortcuts -->
      <div class="p-6 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm">
        <h3 class="text-sm font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-4">
          Quick Actions
        </h3>
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
          <a
            routerLink="/customer/payments/transfer"
            class="p-4 rounded-xl border border-slate-200/70 dark:border-slate-800 hover:border-sky-500 dark:hover:border-sky-500 hover:bg-sky-50/50 dark:hover:bg-sky-950/30 transition-all flex flex-col items-center text-center group"
          >
            <div class="w-10 h-10 rounded-xl bg-sky-100 dark:bg-sky-950 text-sky-600 flex items-center justify-center mb-2 group-hover:scale-110 transition-transform">
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
              </svg>
            </div>
            <span class="text-xs font-semibold text-slate-800 dark:text-slate-200">Intra-Bank Transfer</span>
            <span class="text-[11px] text-slate-400 mt-0.5">Move funds between accounts</span>
          </a>

          <a
            routerLink="/customer/payments/deposit"
            class="p-4 rounded-xl border border-slate-200/70 dark:border-slate-800 hover:border-emerald-500 dark:hover:border-emerald-500 hover:bg-emerald-50/50 dark:hover:bg-emerald-950/30 transition-all flex flex-col items-center text-center group"
          >
            <div class="w-10 h-10 rounded-xl bg-emerald-100 dark:bg-emerald-950 text-emerald-600 flex items-center justify-center mb-2 group-hover:scale-110 transition-transform">
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
              </svg>
            </div>
            <span class="text-xs font-semibold text-slate-800 dark:text-slate-200">Cash Deposit</span>
            <span class="text-[11px] text-slate-400 mt-0.5">Counter deposit simulation</span>
          </a>

          <a
            routerLink="/customer/payments/withdraw"
            class="p-4 rounded-xl border border-slate-200/70 dark:border-slate-800 hover:border-amber-500 dark:hover:border-amber-500 hover:bg-amber-50/50 dark:hover:bg-amber-950/30 transition-all flex flex-col items-center text-center group"
          >
            <div class="w-10 h-10 rounded-xl bg-amber-100 dark:bg-amber-950 text-amber-600 flex items-center justify-center mb-2 group-hover:scale-110 transition-transform">
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
              </svg>
            </div>
            <span class="text-xs font-semibold text-slate-800 dark:text-slate-200">Cash Withdrawal</span>
            <span class="text-[11px] text-slate-400 mt-0.5">Balance limit checks</span>
          </a>

          <a
            routerLink="/customer/transactions"
            class="p-4 rounded-xl border border-slate-200/70 dark:border-slate-800 hover:border-indigo-500 dark:hover:border-indigo-500 hover:bg-indigo-50/50 dark:hover:bg-indigo-950/30 transition-all flex flex-col items-center text-center group"
          >
            <div class="w-10 h-10 rounded-xl bg-indigo-100 dark:bg-indigo-950 text-indigo-600 flex items-center justify-center mb-2 group-hover:scale-110 transition-transform">
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <span class="text-xs font-semibold text-slate-800 dark:text-slate-200">Ledger Statement</span>
            <span class="text-[11px] text-slate-400 mt-0.5">Cursor history & CSV export</span>
          </a>
        </div>
      </div>
    </div>
  `,
})
export class DashboardComponent implements OnInit {
  public authService = inject(AuthService);
  private accountService = inject(AccountService);

  public accounts = signal<AccountDto[]>([]);
  public loadingAccounts = signal(true);

  public totalAvailableBalance = signal('0.00');
  public totalLedgerBalance = signal('0.00');
  public activeAccountsCount = signal(0);

  ngOnInit(): void {
    this.loadAccounts();
  }

  public loadAccounts(): void {
    this.loadingAccounts.set(true);
    this.accountService.getAccounts().subscribe({
      next: (data) => {
        this.accounts.set(data || []);
        this.calculateTotals(data || []);
        this.loadingAccounts.set(false);
      },
      error: () => {
        this.loadingAccounts.set(false);
      },
    });
  }

  private calculateTotals(accounts: AccountDto[]): void {
    let avail = '0';
    let ledger = '0';
    let active = 0;

    for (const acc of accounts) {
      if (acc.status === 'ACTIVE') active++;
      avail = MoneyUtil.add(avail, acc.availableBalance || '0');
      ledger = MoneyUtil.add(ledger, acc.ledgerBalance || '0');
    }

    this.totalAvailableBalance.set(avail);
    this.totalLedgerBalance.set(ledger);
    this.activeAccountsCount.set(active);
  }
}
