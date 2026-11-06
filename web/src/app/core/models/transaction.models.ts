export type TransactionType = 'DEPOSIT' | 'WITHDRAWAL' | 'TRANSFER' | 'REVERSAL' | 'FEE' | 'INTEREST';
export type EntryDirection = 'DEBIT' | 'CREDIT';

export interface LedgerEntryDto {
  id: string;
  transactionId: string;
  accountId: string;
  accountNumber?: string;
  accountName?: string;
  direction: EntryDirection;
  amount: string;
  currency: string;
  balanceAfter: string;
  accountSeq: number;
  createdAt: string;
}

export interface TransactionItemDto {
  id: string;
  reference: string;
  type: TransactionType;
  amount: string;
  currency: string;
  direction: EntryDirection; // From perspective of the queried account
  counterpartyAccountNumber?: string;
  counterpartyName?: string;
  description: string;
  status: 'POSTED' | 'REVERSED';
  reversesTransactionId?: string | null;
  reversedByTransactionId?: string | null;
  postedAt: string;
  ledgerEntries?: LedgerEntryDto[];
}

export interface KeysetPagedResult<T> {
  items: T[];
  nextCursor: string | null;
  hasMore: boolean;
  totalCount?: number;
}

export interface DepositRequest {
  accountId: string;
  amount: string;
  currency: string;
  notes?: string;
  channel?: string;
}

export interface WithdrawalRequest {
  accountId: string;
  amount: string;
  currency: string;
  notes?: string;
}

export interface TransferRequest {
  sourceAccountId: string;
  destinationAccountNumber: string;
  amount: string;
  currency: string;
  referenceNote: string;
}

export interface TransactionReceiptDto {
  id: string;
  reference: string;
  type: TransactionType;
  amount: string;
  currency: string;
  sourceAccountNumber?: string;
  destinationAccountNumber?: string;
  destinationAccountHolder?: string;
  balanceAfter?: string;
  status: string;
  timestamp: string;
  idempotencyKey?: string;
}
