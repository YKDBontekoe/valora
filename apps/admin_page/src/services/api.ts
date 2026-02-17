import axios from 'axios';
import axiosRetry from 'axios-retry';
import { notificationService } from './notificationService';
import type { AuthResponse, Stats, User, Listing, PaginatedResponse } from '../types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_URL,
});

axiosRetry(api, {
  retries: 3,
  retryDelay: axiosRetry.exponentialDelay,
  retryCondition: (error) => {
    return (
      axiosRetry.isNetworkOrIdempotentRequestError(error) ||
      error.response?.status === 429 ||
      (error.response?.status !== undefined && error.response.status >= 500)
    );
  },
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

    // Notify user about error
    if (error.response) {
      const status = error.response.status;
      const data = error.response.data;

      // Determine error message
      let message = data?.detail || data?.title || 'An unexpected error occurred';

      if (Array.isArray(data)) {
        // Legacy error array format
        message = data.map((e: any) => e.error || JSON.stringify(e)).join('\n');
      } else if (data?.errors) {
        // FluentValidation errors
        message = Object.values(data.errors).flat().join('\n');
      }

      if (status >= 500) {
        notificationService.error(`Server Error: ${message}`);
      } else if (status === 403) {
        notificationService.warning('You do not have permission to perform this action.');
      } else if (status === 404) {
        notificationService.warning('Resource not found.');
      } else if (status === 400) {
        notificationService.error(message);
      } else if (status === 429) {
        notificationService.warning('Too many requests. Please try again later.');
      } else if (status !== 401) {
        // 401 is handled by retry logic above
        notificationService.error(message);
      }
    } else if (error.request) {
      notificationService.error('Network error. Please check your connection.');
    } else {
      notificationService.error(error.message);
    }

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
  getUsers: async (page = 1, pageSize = 10): Promise<PaginatedResponse<User>> => {
    const response = await api.get<PaginatedResponse<User>>(`/admin/users?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },
  deleteUser: async (id: string): Promise<void> => {
    await api.delete(`/admin/users/${id}`);
  },
};

export const listingService = {
  getListings: async (page = 1, pageSize = 10): Promise<PaginatedResponse<Listing>> => {
    const response = await api.get<PaginatedResponse<Listing>>(`/listings?page=${page}&pageSize=${pageSize}`);
    return response.data;
  }
};

export default api;
