import axios from 'axios';
import type { AuthResponse, Stats, User, Listing, PaginatedResponse, ListingFilterDto } from '../types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_URL,
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('admin_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

const clearAdminStorage = () => {
  localStorage.removeItem('admin_token');
  localStorage.removeItem('admin_refresh_token');
  localStorage.removeItem('admin_email');
  localStorage.removeItem('admin_userId');
};

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      const refreshToken = localStorage.getItem('admin_refresh_token');
      if (refreshToken) {
        try {
          const response = await axios.post<AuthResponse>(`${API_URL}/auth/refresh`, {
            refreshToken,
          });
          const { token, refreshToken: newRefreshToken } = response.data;
          localStorage.setItem('admin_token', token);
          localStorage.setItem('admin_refresh_token', newRefreshToken);
          originalRequest.headers.Authorization = `Bearer ${token}`;
          return api(originalRequest);
        } catch {
          clearAdminStorage();
          window.location.href = '/login';
        }
      } else {
        clearAdminStorage();
        window.location.href = '/login';
      }
    }
    // Avoid logging full error object to prevent token leakage
    console.error('API Error:', error.message);
    return Promise.reject(error);
  }
);

export const authService = {
  login: async (email: string, password: string): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>('/auth/login', { email, password });
    return response.data;
  },
};

export const adminService = {
  getStats: async (): Promise<Stats> => {
    const response = await api.get<Stats>('/admin/stats');
    return response.data;
  },
  getUsers: async (page = 1, pageSize = 10, searchTerm?: string, sortBy?: string, sortOrder?: string): Promise<PaginatedResponse<User>> => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    if (searchTerm) params.append('searchTerm', searchTerm);
    if (sortBy) params.append('sortBy', sortBy);
    if (sortOrder) params.append('sortOrder', sortOrder);

    const response = await api.get<PaginatedResponse<User>>(`/admin/users?${params.toString()}`);
    return response.data;
  },
  deleteUser: async (id: string): Promise<void> => {
    await api.delete(`/admin/users/${id}`);
  },
  getListings: async (filters: ListingFilterDto = {}): Promise<PaginatedResponse<Listing>> => {
    const params = new URLSearchParams();
    if (filters.page) params.append('Page', filters.page.toString());
    if (filters.pageSize) params.append('PageSize', filters.pageSize.toString());
    if (filters.searchTerm) params.append('SearchTerm', filters.searchTerm);
    if (filters.minPrice) params.append('MinPrice', filters.minPrice.toString());
    if (filters.maxPrice) params.append('MaxPrice', filters.maxPrice.toString());
    if (filters.city) params.append('City', filters.city);
    if (filters.sortBy) params.append('SortBy', filters.sortBy);
    if (filters.sortOrder) params.append('SortOrder', filters.sortOrder);

    const response = await api.get<PaginatedResponse<Listing>>(`/listings?${params.toString()}`);
    return response.data;
  }
};

export default api;
