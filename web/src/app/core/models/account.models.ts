export type AccountType = 'SAVINGS' | 'CURRENT';
export type AccountStatus = 'ACTIVE' | 'FROZEN' | 'CLOSED';

export interface AccountDto {
  id: string;
  customerId: string;
  accountNumber: string;
  accountType: AccountType;
  currency: string;
  status: AccountStatus;
  ledgerBalance: string;
  availableBalance: string;
  overdraftLimit: string;
  minimumBalance: string;
  createdAt: string;
  updatedAt: string;
}

export interface OpenAccountRequest {
  accountType: AccountType;
  initialDeposit: string;
  currency: string;
  branchCode?: string;
}

export interface AccountBalanceDto {
  accountId: string;
  accountNumber: string;
  ledgerBalance: string;
  availableBalance: string;
  overdraftLimit: string;
  currency: string;
  status: AccountStatus;
  asOf: string;
}

export interface BeneficiaryLookupDto {
  accountId: string;
  accountNumber: string;
  accountHolderName: string;
  accountType: AccountType;
  status: AccountStatus;
  currency: string;
}
