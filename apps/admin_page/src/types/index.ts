export interface User {
  id: string;
  email: string;
  roles: string[];
}

export interface Stats {
  totalUsers: number;
  totalListings: number;
  totalNotifications: number;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  email: string;
  userId: string;
}
