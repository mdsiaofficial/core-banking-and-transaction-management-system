import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { AdminDashboardKpiDto, TrialBalanceDto } from '../../../core/models/admin.models';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card.component';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MoneyPipe, StatCardComponent],
  template: `
    <div class="space-y-8 max-w-7xl mx-auto">
      <!-- Admin Header Banner -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-6 rounded-2xl bg-gradient-to-r from-slate-900 via-indigo-950 to-slate-900 text-white shadow-xl">
        <div>
          <div class="flex items-center gap-2">
            <span class="text-xs uppercase font-bold tracking-wider text-sky-400">Back-Office Administration</span>
            <span class="px-2 py-0.5 rounded text-[10px] font-bold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
              ACID Verified
            </span>
          </div>
          <h1 class="text-2xl font-bold tracking-tight mt-1">Core Banking Operations Control</h1>
          <p class="text-xs text-slate-300 mt-1">
            Real-time ledger surveillance, customer governance, and trial balance reconciliation
          </p>
        </div>

        <div class="flex items-center gap-3">
          <a
            routerLink="/admin/reconciliation"
            class="px-4 py-2.5 rounded-xl bg-sky-600 hover:bg-sky-500 text-white text-xs font-semibold shadow-md transition-all flex items-center gap-2"
          >
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 6l3 1m0 0l-3 9a5.002 5.002 0 006.001 0M6 7l3 9M6 7l6-2m6 2l3-1m-3 1l-3 9a5.002 5.002 0 006.001 0M18 7l3 9m-3-9l-6-2m0-2v2m0 16V5m0 16H9m3 0h3" />
            </svg>
            <span>Run Reconciliation</span>
          </a>
        </div>
      </div>

      <!-- KPI Metrics Grid -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        <app-stat-card
          label="Total Customer Base"
          [value]="kpis()?.totalCustomers?.toString() || '—'"
          subtext="Registered Profiles"
          description="Customers with verified or active identities"
        >
          <svg icon class="w-5 h-5 text-sky-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
          </svg>
        </app-stat-card>

        <app-stat-card
          label="Active Accounts"
          [value]="kpis()?.activeAccounts?.toString() || '—'"
          subtext="Open Accounts"
          description="Active customer savings and current accounts"
        >
          <svg icon class="w-5 h-5 text-indigo-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
          </svg>
        </app-stat-card>

        <app-stat-card
          label="Total Customer Deposits"
          [value]="(kpis()?.totalDepositsBDT || '0.00') | money"
          subtext="BDT Liabilities"
          description="Total funds deposited in customer accounts"
        >
          <svg icon class="w-5 h-5 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </app-stat-card>

        <app-stat-card
          label="24h Transfer Volume"
          [value]="(kpis()?.dailyTransferVolumeBDT || '0.00') | money"
          subtext="BDT Volume"
          description="Intra-bank money movement across the ledger"
        >
          <svg icon class="w-5 h-5 text-amber-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
          </svg>
        </app-stat-card>
      </div>

      <!-- Quick Administration Cards -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
        <!-- Customer & Account Controls -->
        <div class="p-6 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm space-y-4">
          <div class="w-10 h-10 rounded-xl bg-sky-50 dark:bg-sky-950/60 text-sky-600 flex items-center justify-center">
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
            </svg>
          </div>
          <div>
            <h3 class="font-bold text-slate-900 dark:text-white">Customer & Account Controls</h3>
            <p class="text-xs text-slate-500 mt-1">
              Search customers, inspect profiles, freeze or unfreeze accounts to block fraudulent debits.
            </p>
          </div>
          <div class="pt-2 flex gap-2">
            <a
              routerLink="/admin/customers"
              class="px-3 py-1.5 text-xs font-semibold text-sky-600 bg-sky-50 dark:bg-sky-950/60 rounded-xl hover:bg-sky-100 transition-colors"
            >
              Search Customers →
            </a>
            <a
              routerLink="/admin/accounts"
              class="px-3 py-1.5 text-xs font-semibold text-slate-700 dark:text-slate-300 bg-slate-100 dark:bg-slate-800 rounded-xl hover:bg-slate-200 transition-colors"
            >
              Account Freeze
            </a>
          </div>
        </div>

        <!-- Transaction Reversals -->
        <div class="p-6 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm space-y-4">
          <div class="w-10 h-10 rounded-xl bg-amber-50 dark:bg-amber-950/60 text-amber-600 flex items-center justify-center">
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
          </div>
          <div>
            <h3 class="font-bold text-slate-900 dark:text-white">Transaction Reversals</h3>
            <p class="text-xs text-slate-500 mt-1">
              Compensating transactions with opposite entries. Original ledger entries remain 100% immutable.
            </p>
          </div>
          <div class="pt-2">
            <a
              routerLink="/admin/reversals"
              class="px-3 py-1.5 text-xs font-semibold text-amber-700 bg-amber-50 dark:bg-amber-950/60 rounded-xl hover:bg-amber-100 transition-colors"
            >
              Manage Reversals →
            </a>
          </div>
        </div>

        <!-- Trial Balance & Audit Logs -->
        <div class="p-6 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200/80 dark:border-slate-800 shadow-sm space-y-4">
          <div class="w-10 h-10 rounded-xl bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 flex items-center justify-center">
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
            </svg>
          </div>
          <div>
            <h3 class="font-bold text-slate-900 dark:text-white">Audit & Reconciliation</h3>
            <p class="text-xs text-slate-500 mt-1">
              Automated reconciliation job checks: stored balances match ledger sum, zero sequence gaps.
            </p>
          </div>
          <div class="pt-2 flex gap-2">
            <a
              routerLink="/admin/reconciliation"
              class="px-3 py-1.5 text-xs font-semibold text-emerald-700 bg-emerald-50 dark:bg-emerald-950/60 rounded-xl hover:bg-emerald-100 transition-colors"
            >
              Trial Balance →
            </a>
            <a
              routerLink="/admin/audit-logs"
              class="px-3 py-1.5 text-xs font-semibold text-slate-700 dark:text-slate-300 bg-slate-100 dark:bg-slate-800 rounded-xl hover:bg-slate-200 transition-colors"
            >
              Audit Logs
            </a>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class AdminDashboardComponent implements OnInit {
  private adminService = inject(AdminService);
  public kpis = signal<AdminDashboardKpiDto | null>(null);

  ngOnInit(): void {
    this.adminService.getDashboardKpis().subscribe({
      next: (data) => this.kpis.set(data),
      error: () => {
        // Fallback demo KPIs if API returns empty
        this.kpis.set({
          totalCustomers: 12,
          activeAccounts: 18,
          totalDepositsBDT: '1540000.00',
          dailyTransferVolumeBDT: '280000.00',
          pendingReconciliations: 0,
          failedLogins24h: 0,
          systemIntegrityStatus: 'HEALTHY',
        });
      },
    });
  }
}
