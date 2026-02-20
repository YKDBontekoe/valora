import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import api from './api';
import axios from 'axios';

describe('API Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    // @ts-expect-error - Mocking read-only property
    delete api.defaults.adapter;
    vi.stubGlobal('location', { href: '' });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('request interceptor adds authorization header if token exists', async () => {
    localStorage.setItem('admin_token', 'test-token');

    // Mock the adapter
    const mockAdapter = vi.fn().mockResolvedValue({
      data: {}, status: 200, statusText: 'OK', headers: {}, config: {},
    });
    api.defaults.adapter = mockAdapter;

    await api.get('/test');

    // Check if the mock was called
    expect(mockAdapter).toHaveBeenCalled();
    // Verify the config passed to the adapter
    const config = mockAdapter.mock.calls[0][0];
    expect(config.headers.Authorization).toBe('Bearer test-token');
  });

  it('handles 401 and attempts refresh', async () => {
    localStorage.setItem('admin_refresh_token', 'refresh-token');

    // Mock first call failing with 401, second call succeeding
    const mockAdapter = vi.fn()
      .mockRejectedValueOnce({
        response: { status: 401 },
        config: { url: '/test', headers: {} }
      })
      .mockResolvedValueOnce({
        data: { success: true }, status: 200, statusText: 'OK', headers: {}, config: {},
      });

    api.defaults.adapter = mockAdapter;

    // Mock the axios.post for refresh
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
});
