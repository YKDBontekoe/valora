import axios from 'axios';
import axiosRetry from 'axios-retry';
import toast from 'react-hot-toast';
import type { AuthResponse, Stats, User, Listing, PaginatedResponse } from '../types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_URL,
});

// Configure retries
axiosRetry(api, {
  retries: 3,
  retryDelay: axiosRetry.exponentialDelay,
  retryCondition: (error) => {
    // Retry on network errors or idempotent requests
    // Also retry on 503 (Service Unavailable) which is safe to retry
    // Avoiding blanket 5xx retry to prevent non-idempotent side effects
    return axiosRetry.isNetworkOrIdempotentRequestError(error) ||
           (error.response?.status === 503);
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

    // Handle 401 Refresh Logic
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      const refreshToken = localStorage.getItem('admin_refresh_token');
      if (refreshToken) {
        try {
          // We create a new instance to avoid interceptors loop or use axios directly
          // But we need the baseURL.
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
          return Promise.reject(error);
        }
      } else {
        clearAdminStorage();
        window.location.href = '/login';
        return Promise.reject(error);
      }
    }

    // Global Error Notification
    // Extract error message from ProblemDetails or standard response
    let message = 'An unexpected error occurred';
    if (error.response?.data) {
        if (typeof error.response.data === 'string') {
            message = error.response.data;
        } else if (error.response.data.detail) {
            message = error.response.data.detail;
        } else if (error.response.data.title) {
            message = error.response.data.title;
        }
    } else if (error.message) {
        message = error.message;
    }

    // Don't show toast for 401 (handled by redirect)
    if (error.response?.status !== 401) {
        toast.error(message);
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
  getUsers: async (page = 1, pageSize = 10): Promise<PaginatedResponse<User>> => {
    const response = await api.get<PaginatedResponse<User>>(`/admin/users?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },
  deleteUser: async (id: string): Promise<void> => {
    await api.delete(`/admin/users/${id}`);
  },
  getListings: async (): Promise<PaginatedResponse<Listing>> => {
    const response = await api.get<PaginatedResponse<Listing>>('/listings');
    return response.data;
  }
};

export default api;
