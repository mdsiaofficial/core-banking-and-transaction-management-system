import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  // Public Auth routes
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        loadComponent: () =>
          import('./features/auth/login/login.component').then((m) => m.LoginComponent),
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./features/auth/register/register.component').then((m) => m.RegisterComponent),
      },
      {
        path: 'verify-email',
        loadComponent: () =>
          import('./features/auth/verify-email/verify-email.component').then(
            (m) => m.VerifyEmailComponent
          ),
      },
      {
        path: '',
        redirectTo: 'login',
        pathMatch: 'full',
      },
    ],
  },

  // Customer Portal (Protected)
  {
    path: 'customer',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/customer/customer-layout.component').then(
        (m) => m.CustomerLayoutComponent
      ),
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/customer/dashboard/dashboard.component').then(
            (m) => m.DashboardComponent
          ),
      },
      {
        path: 'accounts',
        loadComponent: () =>
          import('./features/customer/accounts/accounts-list/accounts-list.component').then(
            (m) => m.AccountsListComponent
          ),
      },
      {
        path: 'accounts/open',
        loadComponent: () =>
          import(
            './features/customer/accounts/open-account-wizard/open-account-wizard.component'
          ).then((m) => m.OpenAccountWizardComponent),
      },
      {
        path: 'accounts/:id',
        loadComponent: () =>
          import('./features/customer/accounts/account-detail/account-detail.component').then(
            (m) => m.AccountDetailComponent
          ),
      },
      {
        path: 'payments/deposit',
        loadComponent: () =>
          import('./features/customer/money-movement/deposit/deposit.component').then(
            (m) => m.DepositComponent
          ),
      },
      {
        path: 'payments/withdraw',
        loadComponent: () =>
          import('./features/customer/money-movement/withdrawal/withdrawal.component').then(
            (m) => m.WithdrawalComponent
          ),
      },
      {
        path: 'payments/transfer',
        loadComponent: () =>
          import('./features/customer/money-movement/transfer/transfer.component').then(
            (m) => m.TransferComponent
          ),
      },
      {
        path: 'transactions',
        loadComponent: () =>
          import(
            './features/customer/transactions/transaction-history/transaction-history.component'
          ).then((m) => m.TransactionHistoryComponent),
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./features/customer/profile/profile.component').then((m) => m.ProfileComponent),
      },
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
    ],
  },

  // Back-Office Administrative Portal (Protected & Role Guarded)
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard(['Admin', 'Auditor'])],
    loadComponent: () =>
      import('./features/admin/admin-layout.component').then((m) => m.AdminLayoutComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/admin/admin-dashboard/admin-dashboard.component').then(
            (m) => m.AdminDashboardComponent
          ),
      },
      {
        path: 'customers',
        loadComponent: () =>
          import('./features/admin/customer-management/customer-management.component').then(
            (m) => m.CustomerManagementComponent
          ),
      },
      {
        path: 'customers/:id',
        loadComponent: () =>
          import('./features/admin/customer-detail/customer-detail.component').then(
            (m) => m.CustomerDetailComponent
          ),
      },
      {
        path: 'accounts',
        loadComponent: () =>
          import('./features/admin/account-controls/account-controls.component').then(
            (m) => m.AccountControlsComponent
          ),
      },
      {
        path: 'reversals',
        loadComponent: () =>
          import('./features/admin/transaction-reversals/transaction-reversals.component').then(
            (m) => m.TransactionReversalsComponent
          ),
      },
      {
        path: 'audit-logs',
        loadComponent: () =>
          import('./features/admin/audit-logs/audit-logs.component').then(
            (m) => m.AuditLogViewerComponent
          ),
      },
      {
        path: 'reconciliation',
        loadComponent: () =>
          import('./features/admin/reconciliation/reconciliation.component').then(
            (m) => m.ReconciliationDashboardComponent
          ),
      },
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
    ],
  },

  // Fallbacks
  {
    path: '',
    redirectTo: 'customer/dashboard',
    pathMatch: 'full',
  },
  {
    path: '**',
    redirectTo: 'customer/dashboard',
  },
];
