import axios from 'axios';
import type { AuthResponse, Stats, User, Listing, PaginatedResponse, BatchJob } from '../types';
import { showToast } from './toast';

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

const MAX_RETRIES = 3;
const RETRY_DELAY = 1000;

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (!originalRequest) return Promise.reject(error);

    // Auth Retry (401)
    if (error.response?.status === 401 && !originalRequest._isAuthRetry) {
      originalRequest._isAuthRetry = true;
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
          return Promise.reject(error);
        }
      } else {
        clearAdminStorage();
        window.location.href = '/login';
        return Promise.reject(error);
      }
    }

    // Resilience Logic
    const isNetworkError = !error.response;
    const isServerError = error.response?.status >= 500 || error.response?.status === 429;
    const isIdempotent = ['get', 'put', 'delete', 'head', 'options'].includes(originalRequest.method?.toLowerCase());

    if ((isNetworkError || (isServerError && isIdempotent))) {
        originalRequest._retryCount = originalRequest._retryCount || 0;

        if (originalRequest._retryCount < MAX_RETRIES) {
             originalRequest._retryCount++;
             const delay = RETRY_DELAY * Math.pow(2, originalRequest._retryCount - 1);
             await new Promise(resolve => setTimeout(resolve, delay));
             return api(originalRequest);
        }
    }

    // Global Error Feedback
    const message = error.response?.data?.detail
      || error.response?.data?.title
      || error.message
      || 'An unexpected error occurred';

    if (error.response?.status !== 401) {
       showToast(message, 'error');
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
  getUsers: async (page = 1, pageSize = 10, search?: string, sort?: string): Promise<PaginatedResponse<User>> => {
    let url = `/admin/users?page=${page}&pageSize=${pageSize}`;
    if (search) url += `&q=${encodeURIComponent(search)}`;
    if (sort) url += `&sort=${encodeURIComponent(sort)}`;
    const response = await api.get<PaginatedResponse<User>>(url);
    return response.data;
  },
  deleteUser: async (id: string): Promise<void> => {
    await api.delete(`/admin/users/${id}`);
  },
  getJobs: async (limit = 10): Promise<BatchJob[]> => {
    const response = await api.get<BatchJob[]>(`/admin/jobs?limit=${limit}`);
    return response.data;
  },
  startJob: async (type: string, target: string): Promise<BatchJob> => {
    const response = await api.post<BatchJob>("/admin/jobs", { type, target });
    return response.data;
  },
};

export const listingService = {
  getListings: async (page = 1, pageSize = 10): Promise<PaginatedResponse<Listing>> => {
    const response = await api.get<PaginatedResponse<Listing>>(`/listings?page=${page}&pageSize=${pageSize}`);
    return response.data;
  }
};

export default api;
