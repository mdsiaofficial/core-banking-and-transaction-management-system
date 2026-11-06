export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errorCode?: string;
  error_code?: string; // snake_case compatibility
  traceId?: string;
  trace_id?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}
