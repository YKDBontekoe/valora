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

export interface DatasetStatus {
  city: string;
  neighborhoodCount: number;
  lastUpdated: string | null;
}

export interface HealthStatus {
  status: string;
  databaseStatus: string;
  apiLatencyMs: number;
  activeJobs: number;
  queuedJobs: number;
  failedJobs: number;
  lastPipelineSuccess: string | null;
  timestamp: string;
}
