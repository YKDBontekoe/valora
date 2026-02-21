import { renderHook, act, waitFor } from '@testing-library/react';
import { useUsers } from './useUsers';
import { adminService } from '../services/api';
import { vi, describe, it, expect, beforeEach, afterEach, type Mock } from 'vitest';
import type { User, PaginatedResponse } from '../types';

vi.mock('../services/api', () => ({
  adminService: {
    getUsers: vi.fn(),
    deleteUser: vi.fn(),
  },
}));

describe('useUsers hook', () => {
  const mockUsers: User[] = [{ id: '1', email: 'test@example.com', role: 'user', created_at: '2023-01-01' }];

  beforeEach(() => {
    vi.clearAllMocks();
    (adminService.getUsers as Mock).mockResolvedValue({
      items: mockUsers,
      totalPages: 5,
      totalCount: 50,
    });
    Object.defineProperty(window, 'localStorage', {
      value: {
        getItem: vi.fn(() => null),
        setItem: vi.fn(),
        removeItem: vi.fn(),
      },
      writable: true
    });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should debounce search input and reset page to 1', async () => {
    const { result } = renderHook(() => useUsers());

    // Initial fetch
    await waitFor(() => expect(adminService.getUsers).toHaveBeenCalledTimes(1));
    (adminService.getUsers as Mock).mockClear();

    // Search input
    act(() => {
      result.current.setSearchQuery('john');
    });

    // Verify debounce hasn't fired immediately
    expect(adminService.getUsers).not.toHaveBeenCalled();

    // Wait for debounce (500ms + buffer)
    await waitFor(() => expect(adminService.getUsers).toHaveBeenCalledTimes(1), { timeout: 1000 });

    expect(adminService.getUsers).toHaveBeenLastCalledWith(1, 10, 'john', undefined);
  });

  it('should fetch users on mount', async () => {
    const { result } = renderHook(() => useUsers());
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.users).toEqual(mockUsers);
  });

  it('should handle fetch error', async () => {
    (adminService.getUsers as Mock).mockRejectedValue(new Error('API Error'));
    const { result } = renderHook(() => useUsers());
    await waitFor(() => expect(result.current.error).toBe('Failed to load users. Please try again.'));
  });

  it('should avoid double fetch when sorting changes while on page 2', async () => {
    const { result } = renderHook(() => useUsers());
    await waitFor(() => expect(adminService.getUsers).toHaveBeenCalledTimes(1));
    (adminService.getUsers as Mock).mockClear();

    act(() => { result.current.setPage(2); });
    await waitFor(() => expect(adminService.getUsers).toHaveBeenCalledTimes(1));
    (adminService.getUsers as Mock).mockClear();

    act(() => { result.current.toggleSort('email'); });
    await waitFor(() => expect(adminService.getUsers).toHaveBeenCalledTimes(1));

    await new Promise(resolve => setTimeout(resolve, 50));
    expect(adminService.getUsers).toHaveBeenCalledTimes(1);
    expect(adminService.getUsers).toHaveBeenLastCalledWith(1, 10, '', 'email_asc');
  });

  it('should handle delete user', async () => {
    const { result } = renderHook(() => useUsers());
    await waitFor(() => expect(result.current.loading).toBe(false));
    await act(async () => { await result.current.deleteUser(mockUsers[0]); });
    expect(adminService.deleteUser).toHaveBeenCalledWith('1');
  });

  it('should handle delete user error', async () => {
    const { result } = renderHook(() => useUsers());
    await waitFor(() => expect(result.current.loading).toBe(false));
    const error = new Error('Delete failed');
    (adminService.deleteUser as Mock).mockRejectedValue(error);
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
    await expect(result.current.deleteUser(mockUsers[0])).rejects.toThrow('Delete failed');
    consoleSpy.mockRestore();
  });

  it('should not allow deleting self', async () => {
    (window.localStorage.getItem as Mock).mockReturnValue('1');
    const { result } = renderHook(() => useUsers());
    await waitFor(() => expect(result.current.loading).toBe(false));
    await expect(result.current.deleteUser(mockUsers[0])).rejects.toThrow('You cannot delete your own account.');
  });

  it('should handle pagination controls', async () => {
      const { result } = renderHook(() => useUsers());
      await waitFor(() => expect(result.current.totalPages).toBe(5));
      act(() => { result.current.nextPage(); });
      await waitFor(() => expect(result.current.page).toBe(2));
      act(() => { result.current.prevPage(); });
      await waitFor(() => expect(result.current.page).toBe(1));
      act(() => { result.current.prevPage(); });
      expect(result.current.page).toBe(1);
  });

  it('should refresh trigger re-fetch', async () => {
      const { result } = renderHook(() => useUsers());
      await waitFor(() => expect(adminService.getUsers).toHaveBeenCalledTimes(1));
      (adminService.getUsers as Mock).mockClear();
      act(() => { result.current.refresh(); });
      await waitFor(() => expect(adminService.getUsers).toHaveBeenCalledTimes(1));
  });

  it('should ignore fetch results if unmounted', async () => {
    let resolveUsers: ((value: PaginatedResponse<User>) => void) | undefined;
    const pendingPromise = new Promise<PaginatedResponse<User>>((resolve) => { resolveUsers = resolve; });
    (adminService.getUsers as Mock).mockReturnValue(pendingPromise);

    const { result, unmount } = renderHook(() => useUsers());

    expect(result.current.loading).toBe(true);

    unmount();

    await act(async () => {
        if (resolveUsers) resolveUsers({ items: mockUsers, totalPages: 5, totalCount: 50 });
    });
  });
});
