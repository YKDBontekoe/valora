import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import api from './api';
import axios from 'axios';

describe('API Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    delete api.defaults.adapter;
    vi.stubGlobal('location', { href: '' });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('request interceptor adds authorization header if token exists', async () => {
    localStorage.setItem('admin_token', 'test-token');

    const mockAdapter = vi.fn().mockResolvedValue({
      data: {}, status: 200, statusText: 'OK', headers: {}, config: {},
    });
    api.defaults.adapter = mockAdapter;

    await api.get('/test');

    expect(mockAdapter).toHaveBeenCalled();
    const config = mockAdapter.mock.calls[0][0];
    expect(config.headers.Authorization).toBe('Bearer test-token');
  });

  it('handles 401 and attempts refresh', async () => {
    localStorage.setItem('admin_refresh_token', 'refresh-token');

    const mockAdapter = vi.fn()
      .mockRejectedValueOnce({
        response: { status: 401 },
        config: { url: '/test', headers: {} }
      })
      .mockResolvedValueOnce({
        data: { success: true }, status: 200, statusText: 'OK', headers: {}, config: {},
      });

    api.defaults.adapter = mockAdapter;

    const axiosPostSpy = vi.spyOn(axios, 'post').mockResolvedValue({
      data: { token: 'new-token', refreshToken: 'new-refresh-token' }
    });

    const response = await api.get('/test');

    expect(axiosPostSpy).toHaveBeenCalledWith(expect.stringContaining('/auth/refresh'), {
      refreshToken: 'refresh-token'
    });
    expect(localStorage.getItem('admin_token')).toBe('new-token');
    expect(response.data.success).toBe(true);
  });

  it('queues concurrent 401 requests and retries them after a single refresh', async () => {
    localStorage.setItem('admin_refresh_token', 'refresh-token');

    let callCount = 0;
    const mockAdapter = vi.fn().mockImplementation((config) => {
        callCount++;
        // If it's a retry (has Authorization: Bearer new-token), succeed
        if (config.headers?.Authorization === 'Bearer new-token') {
             return Promise.resolve({
                data: { success: true, id: callCount }, status: 200, statusText: 'OK', headers: {}, config
             });
        }
        // Otherwise fail with 401
        return Promise.reject({
            response: { status: 401 },
            config: { ...config, headers: {} } // shallow copy config
        });
    });

    api.defaults.adapter = mockAdapter;

    // Mock refresh to return new token with a slight delay
    const axiosPostSpy = vi.spyOn(axios, 'post').mockImplementation(async () => {
        await new Promise(resolve => setTimeout(resolve, 50));
        return {
            data: { token: 'new-token', refreshToken: 'new-refresh-token' }
        };
    });

    const req1 = api.get('/test1');
    const req2 = api.get('/test2');
    const req3 = api.get('/test3');

    await Promise.all([req1, req2, req3]);

    // Refresh should be called exactly once
    expect(axiosPostSpy).toHaveBeenCalledTimes(1);
    expect(axiosPostSpy).toHaveBeenCalledWith(expect.stringContaining('/auth/refresh'), {
        refreshToken: 'refresh-token'
    });

    // Storage should be updated
    expect(localStorage.getItem('admin_token')).toBe('new-token');
    expect(localStorage.getItem('admin_refresh_token')).toBe('new-refresh-token');
  });

  it('clears storage and redirects if refresh fails', async () => {
    localStorage.setItem('admin_refresh_token', 'refresh-token');

    const mockAdapter = vi.fn().mockRejectedValue({
        response: { status: 401 },
        config: { url: '/test', headers: {} }
    });

    api.defaults.adapter = mockAdapter;

    // Mock refresh failure
    vi.spyOn(axios, 'post').mockRejectedValue(new Error('Refresh failed'));

    try {
        await api.get('/test');
    } catch {
        // Expected to fail
    }

    expect(localStorage.getItem('admin_token')).toBeNull();
    expect(localStorage.getItem('admin_refresh_token')).toBeNull();
    expect(window.location.href).toBe('/login');
  });

  it('prevents recursive loops', async () => {
      localStorage.setItem('admin_refresh_token', 'refresh-token');

      const mockAdapter = vi.fn();

      // 1. Initial request -> 401
      mockAdapter.mockRejectedValueOnce({
          response: { status: 401 },
          config: { url: '/test', headers: {} }
      });

      // 2. Retry request -> 401 again
      mockAdapter.mockRejectedValueOnce({
          response: { status: 401 },
          config: { url: '/test', headers: { Authorization: 'Bearer new-token' }, _isAuthRetry: true }
      });

      api.defaults.adapter = mockAdapter;

      vi.spyOn(axios, 'post').mockResolvedValue({
          data: { token: 'new-token', refreshToken: 'new-refresh-token' }
      });

      try {
          await api.get('/test');
      } catch {
          // Expected to fail
      }

      // Refresh called once
      expect(axios.post).toHaveBeenCalledTimes(1);
  });
});
