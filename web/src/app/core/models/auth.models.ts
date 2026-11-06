export type UserRole = 'Customer' | 'Admin' | 'Auditor';

export interface UserProfile {
  id: string;
  customerId?: string;
  email: string;
  fullName: string;
  phone?: string;
  dateOfBirth?: string;
  address?: string;
  status: 'PENDING_VERIFICATION' | 'ACTIVE' | 'SUSPENDED' | 'CLOSED';
  roles: UserRole[];
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  phone: string;
  dateOfBirth: string;
  address: string;
}

export interface RegisterResponse {
  message: string;
  userId: string;
  customerId?: string;
  status: string;
  emailVerificationRequired: boolean;
}

export interface VerifyEmailRequest {
  email: string;
  token: string;
}

export interface VerifyEmailResponse {
  message: string;
  verified: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresIn: number;
  refreshToken?: string;
  user: UserProfile;
}

export interface RefreshResponse {
  accessToken: string;
  expiresIn: number;
  refreshToken?: string;
}
