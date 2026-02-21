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
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}
