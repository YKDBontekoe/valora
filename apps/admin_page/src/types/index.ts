export interface User {
  id: string;
  email: string;
  roles: string[];
}

export interface Stats {
  totalUsers: number;
  totalNotifications: number;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  email: string;
  userId: string;
  roles: string[];
}

export interface PaginatedResponse<T> {
  items: T[];
  pageIndex: number;
  totalPages: number;
  totalCount: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface BatchJob {
  id: string;
  type: string;
  status: string;
  target: string;
  progress: number;
  error: string | null;
  resultSummary: string | null;
  executionLog?: string | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}

export interface SystemHealth {
  status: 'healthy' | 'degraded';
  timestamp: string;
  database: 'connected' | 'disconnected';
  dbLatencyMs: number;
  apiLatency: {
    p50: number;
    p95: number;
    p99: number;
  };
  jobs: {
    active: number;
    queued: number;
    failed: number;
    lastSuccess: string | null;
  };
}
