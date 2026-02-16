import axios from 'axios';
import type { AuthResponse, Stats, User, Listing, PaginatedResponse } from '../types';
import { showNotification } from './notification';

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

    const status = error.response?.status;
    const data = error.response?.data;

    let message = 'An unexpected error occurred.';

    if (status === 400) {
      if (Array.isArray(data)) {
         message = data.map((e: { error?: string; message?: string }) => e.error || e.message).join('\n') || 'Validation failed';
      } else {
        message = 'Invalid request. Please check your input.';
      }
    } else if (status === 403) {
      message = 'You do not have permission to perform this action.';
    } else if (status === 404) {
      message = 'The requested resource was not found.';
    } else if (status === 429) {
      message = 'Too many requests. Please try again later.';
    } else if (status >= 500) {
      message = 'Server error. Please try again later.';
    } else if (error.code === 'ERR_NETWORK') {
      message = 'Network error. Please check your connection.';
    }

    if (status !== 401) {
      showNotification(message, 'error');
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
