import { renderHook, act, waitFor } from '@testing-library/react';
import { useUsers } from './useUsers';
import { adminService } from '../services/api';
import { vi, describe, it, expect, beforeEach, type Mock } from 'vitest';

vi.mock('../services/api', () => ({
  adminService: {
    getUsers: vi.fn(),
    deleteUser: vi.fn(),
  },
}));

describe('useUsers hook', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (adminService.getUsers as Mock).mockResolvedValue({
      items: [],
      totalPages: 5,
      totalCount: 50,
    });
  });

  it('should avoid double fetch when sorting changes while on page 2', async () => {
    const { result } = renderHook(() => useUsers());

    // Initial fetch (Page 1)
    await waitFor(() => expect(adminService.getUsers).toHaveBeenCalledTimes(1));
    (adminService.getUsers as Mock).mockClear();

    // Move to Page 2
    act(() => {
      result.current.setPage(2);
    });

    // Wait for fetch (Page 2)
    await waitFor(() => expect(adminService.getUsers).toHaveBeenCalledTimes(1));
    expect(adminService.getUsers).toHaveBeenLastCalledWith(2, 10, '', undefined);
    (adminService.getUsers as Mock).mockClear();

    // Toggle sort
    act(() => {
      result.current.toggleSort('email');
    });

    // Wait for effects.
    // We want to assert that we ONLY fetch with Page 1.

    // Wait for at least one call
    await waitFor(() => expect(adminService.getUsers).toHaveBeenCalled());

    // Allow effects to settle (if double fetch happens)
    await new Promise(resolve => setTimeout(resolve, 100));

    // Check calls
    const calls = (adminService.getUsers as Mock).mock.calls;

    // We expect EXACTLY 1 call, and it must be for Page 1.
    expect(calls.length).toBe(1);
    expect(calls[0][0]).toBe(1); // Page argument
    expect(calls[0][3]).toBe('email_asc'); // Sort argument
  });
});
