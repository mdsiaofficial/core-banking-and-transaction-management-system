import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { CustomerDetailDto } from '../../../core/models/admin.models';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, MoneyPipe, BadgeComponent],
  template: `
    <div class="space-y-6 max-w-5xl mx-auto">
      <div class="flex items-center gap-3">
        <a
          routerLink="/admin/customers"
          class="p-2 rounded-xl text-slate-500 hover:text-slate-900 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
        >
          <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </a>
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-slate-900 dark:text-white">Customer Dossier</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400">
            Customer ID: <span class="font-mono">{{ customerId }}</span>
          </p>
        </div>
      </div>

      @if (loading()) {
        <div class="h-64 bg-slate-200 dark:bg-slate-800 rounded-2xl animate-pulse"></div>
      } @else if (customer(); as c) {
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 p-8 shadow-sm space-y-6">
          <div class="flex items-center justify-between pb-6 border-b border-slate-100 dark:border-slate-800">
            <div>
              <div class="flex items-center gap-3">
                <h2 class="text-xl font-bold text-slate-900 dark:text-white">{{ c.fullName }}</h2>
                <app-badge [variant]="c.status === 'ACTIVE' ? 'success' : 'danger'">
                  {{ c.status }}
                </app-badge>
              </div>
              <p class="text-xs text-slate-400 font-mono mt-0.5">{{ c.email }}</p>
            </div>

            <div class="text-right">
              <span class="text-xs text-slate-400 block">Total Portfolio Balance:</span>
              <span class="text-xl font-extrabold text-slate-900 dark:text-white">{{ c.totalBalanceBDT | money }}</span>
            </div>
          </div>

          <!-- Customer Identity Attributes -->
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs">
            <div>
              <span class="text-slate-400 block">Phone</span>
              <span class="font-mono font-medium text-slate-800 dark:text-slate-200">{{ c.phone || '—' }}</span>
            </div>
            <div>
              <span class="text-slate-400 block">Date of Birth</span>
              <span class="font-medium text-slate-800 dark:text-slate-200">{{ c.dateOfBirth }}</span>
            </div>
            <div class="sm:col-span-2">
              <span class="text-slate-400 block">Address</span>
              <span class="font-medium text-slate-800 dark:text-slate-200">{{ c.address }}</span>
            </div>
          </div>

          <!-- Linked Bank Accounts -->
          <div class="pt-6 border-t border-slate-100 dark:border-slate-800">
            <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500 mb-3">
              Linked Bank Accounts
            </h3>

            @if (c.accounts && c.accounts.length > 0) {
              <div class="space-y-3">
                @for (acc of c.accounts; track acc.id) {
                  <div class="p-4 rounded-xl border border-slate-200 dark:border-slate-800 flex items-center justify-between">
                    <div>
                      <div class="flex items-center gap-2">
                        <span class="font-mono font-bold text-sm text-slate-900 dark:text-white">{{ acc.accountNumber }}</span>
                        <span class="text-xs px-2 py-0.5 rounded bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 font-medium">
                          {{ acc.accountType }}
                        </span>
                      </div>
                      <span class="text-xs text-slate-400">Created: {{ acc.createdAt | date: 'shortDate' }}</span>
                    </div>

                    <div class="text-right">
                      <div class="text-sm font-bold text-slate-900 dark:text-white">{{ acc.availableBalance | money }}</div>
                      <span class="text-xs text-slate-400">Ledger: {{ acc.ledgerBalance | money }}</span>
                    </div>
                  </div>
                }
              </div>
            } @else {
              <p class="text-xs text-slate-400 italic">No bank accounts opened yet.</p>
            }
          </div>
        </div>
      }
    </div>
  `,
})
export class CustomerDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private adminService = inject(AdminService);

  public customerId = '';
  public customer = signal<CustomerDetailDto | null>(null);
  public loading = signal(true);

  ngOnInit(): void {
    this.customerId = this.route.snapshot.params['id'];
    this.adminService.getCustomerById(this.customerId).subscribe({
      next: (data) => {
        this.customer.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
