import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AccountService } from '../../../../core/services/account.service';
import { AccountDto } from '../../../../core/models/account.models';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';

@Component({
  selector: 'app-accounts-list',
  standalone: true,
  imports: [CommonModule, RouterLink, MoneyPipe, BadgeComponent],
  template: `
    <div class="space-y-6 max-w-7xl mx-auto">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Account Portfolio</h1>
          <p class="text-sm text-slate-500 dark:text-slate-400 mt-1">
            Overview of your active, frozen, or closed accounts
          </p>
        </div>

        <a
          routerLink="/customer/accounts/open"
          class="px-4 py-2.5 rounded-xl bg-sky-600 hover:bg-sky-500 text-white text-xs font-semibold shadow-md shadow-sky-600/20 transition-all flex items-center gap-2 self-start sm:self-auto"
        >
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          <span>Open New Account</span>
        </a>
      </div>

      @if (loading()) {
        <div class="space-y-3">
          <div class="h-20 bg-slate-200 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
          <div class="h-20 bg-slate-200 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
        </div>
      } @else if (accounts().length === 0) {
        <div class="p-12 rounded-2xl border border-dashed border-slate-300 dark:border-slate-800 text-center bg-white dark:bg-slate-900">
          <h3 class="font-semibold text-slate-900 dark:text-white">No accounts found</h3>
          <p class="text-xs text-slate-500 mt-1">You don't have any accounts registered yet.</p>
          <a
            routerLink="/customer/accounts/open"
            class="inline-block mt-4 px-4 py-2 text-xs font-semibold text-white bg-sky-600 hover:bg-sky-500 rounded-xl"
          >
            Open an Account Now
          </a>
        </div>
      } @else {
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 overflow-hidden shadow-sm">
          <div class="overflow-x-auto">
            <table class="w-full text-left text-sm text-slate-600 dark:text-slate-300">
              <thead class="bg-slate-50 dark:bg-slate-800/60 text-[11px] uppercase font-bold text-slate-400 dark:text-slate-400 border-b border-slate-200/70 dark:border-slate-800">
                <tr>
                  <th class="px-6 py-4">Account Number</th>
                  <th class="px-6 py-4">Type</th>
                  <th class="px-6 py-4">Status</th>
                  <th class="px-6 py-4 text-right">Available Balance</th>
                  <th class="px-6 py-4 text-right">Ledger Balance</th>
                  <th class="px-6 py-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
                @for (acc of accounts(); track acc.id) {
                  <tr class="hover:bg-slate-50/70 dark:hover:bg-slate-800/40 transition-colors">
                    <td class="px-6 py-4 font-mono font-medium text-slate-900 dark:text-white">
                      {{ acc.accountNumber }}
                    </td>
                    <td class="px-6 py-4">
                      <span class="inline-flex px-2 py-0.5 rounded text-xs font-semibold bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
                        {{ acc.accountType }}
                      </span>
                    </td>
                    <td class="px-6 py-4">
                      <app-badge [variant]="acc.status === 'ACTIVE' ? 'success' : acc.status === 'FROZEN' ? 'warning' : 'danger'">
                        {{ acc.status }}
                      </app-badge>
                    </td>
                    <td class="px-6 py-4 text-right font-bold text-slate-900 dark:text-white">
                      {{ acc.availableBalance | money }}
                    </td>
                    <td class="px-6 py-4 text-right font-medium text-slate-500 dark:text-slate-400">
                      {{ acc.ledgerBalance | money }}
                    </td>
                    <td class="px-6 py-4 text-right">
                      <div class="flex items-center justify-end gap-2">
                        <a
                          [routerLink]="['/customer/accounts', acc.id]"
                          class="px-2.5 py-1 text-xs font-semibold text-slate-700 dark:text-slate-300 hover:text-sky-600 dark:hover:text-sky-400 bg-slate-100 dark:bg-slate-800 rounded-lg transition-colors"
                        >
                          Details
                        </a>
                        <a
                          [routerLink]="['/customer/payments/transfer']"
                          [queryParams]="{ source: acc.id }"
                          class="px-2.5 py-1 text-xs font-semibold text-white bg-sky-600 hover:bg-sky-500 rounded-lg transition-colors"
                        >
                          Transfer
                        </a>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }
    </div>
  `,
})
export class AccountsListComponent implements OnInit {
  private accountService = inject(AccountService);
  public accounts = signal<AccountDto[]>([]);
  public loading = signal(true);

  ngOnInit(): void {
    this.accountService.getAccounts().subscribe({
      next: (res) => {
        this.accounts.set(res || []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
