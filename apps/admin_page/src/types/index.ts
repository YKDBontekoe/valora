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
  roles: string[];
}

export interface Listing {
  id: string;
  fundaId: string;
  address: string;
  city: string;
  price: number | null;
  status: string | null;
}

export interface ListingFilter {
  searchTerm?: string;
  minPrice?: number;
  maxPrice?: number;
  city?: string;
  minBedrooms?: number;
  minLivingArea?: number;
  maxLivingArea?: number;
  minSafetyScore?: number;
  minCompositeScore?: number;
  sortBy?: 'Price' | 'Date' | 'LivingArea' | 'City' | 'ContextCompositeScore' | 'ContextSafetyScore';
  sortOrder?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

export interface PaginatedResponse<T> {
  items: T[];
  pageIndex: number;
  totalPages: number;
  totalCount: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
