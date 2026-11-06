import { AccountDto } from './account.models';
import { TransactionItemDto, LedgerEntryDto } from './transaction.models';

export interface CustomerSummaryDto {
  id: string;
  userId: string;
  fullName: string;
  email: string;
  phone: string;
  dateOfBirth: string;
  address: string;
  status: 'PENDING_VERIFICATION' | 'ACTIVE' | 'SUSPENDED' | 'CLOSED';
  createdAt: string;
  accountsCount: number;
  totalBalanceBDT: string;
}

export interface CustomerDetailDto extends CustomerSummaryDto {
  accounts: AccountDto[];
  auditHistory?: AuditLogDto[];
}

export interface SuspendCustomerRequest {
  reason: string;
}

export interface FreezeAccountRequest {
  reason: string;
}

export interface ReverseTransactionRequest {
  reason: string;
}

export interface AuditLogDto {
  id: string;
  occurredAt: string;
  actorUserId: string;
  actorEmail?: string;
  actorRole: string;
  actorIp: string;
  correlationId: string;
  action: string;
  entityType: string;
  entityId: string;
  outcome: 'SUCCESS' | 'FAILURE' | 'DENIED';
  before?: Record<string, unknown> | null;
  after?: Record<string, unknown> | null;
  metadata?: Record<string, unknown> | null;
}

export interface ReconciliationRunDto {
  id: string;
  startedAt: string;
  completedAt: string;
  status: 'COMPLETED' | 'FAILED' | 'DISCREPANCY_DETECTED';
  accountsChecked: number;
  discrepanciesCount: number;
  totalAssetsBDT: string;
  totalLiabilitiesBDT: string;
  totalEquityBDT: string;
  isBalanced: boolean;
  notes?: string;
  findings: ReconciliationFindingDto[];
}

export interface ReconciliationFindingDto {
  id: string;
  runId: string;
  accountId: string;
  accountNumber: string;
  expectedBalance: string;
  actualBalance: string;
  difference: string;
  findingType: 'BALANCE_MISMATCH' | 'GAP_IN_SEQUENCE' | 'ORPHAN_ENTRY';
  details: string;
}

export interface TrialBalanceDto {
  asOf: string;
  totalDebits: string;
  totalCredits: string;
  totalAssets: string;
  totalLiabilities: string;
  totalEquity: string;
  isBalanced: boolean;
  currency: string;
  chartBreakdown: {
    code: string;
    name: string;
    accountCategory: 'ASSET' | 'LIABILITY' | 'EQUITY' | 'INCOME' | 'EXPENSE';
    debitTotal: string;
    creditTotal: string;
    netBalance: string;
  }[];
}

export interface AdminDashboardKpiDto {
  totalCustomers: number;
  activeAccounts: number;
  totalDepositsBDT: string;
  dailyTransferVolumeBDT: string;
  pendingReconciliations: number;
  failedLogins24h: number;
  systemIntegrityStatus: 'HEALTHY' | 'WARNING' | 'ALERT';
}
