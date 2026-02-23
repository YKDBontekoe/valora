import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest';
import Users from './Users';
import { adminService } from '../services/api';

vi.mock('../services/api', () => ({
  adminService: {
    getUsers: vi.fn(),
  },
}));

describe('Users Page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders users list when API call succeeds', async () => {
    const mockUsers = {
      items: [
        { id: '1', email: 'user@example.com', roles: ['Admin'] },
        { id: '2', email: 'guest@example.com', roles: ['User'] },
      ],
      totalPages: 1,
    };
    (adminService.getUsers as Mock).mockResolvedValue(mockUsers);

    render(<Users />);

    expect(screen.getByText('Users')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('user@example.com')).toBeInTheDocument();
      expect(screen.getByText('guest@example.com')).toBeInTheDocument();
    });
  });

  it('renders error message when API call fails', async () => {
    (adminService.getUsers as Mock).mockRejectedValue(new Error('API Error'));

    render(<Users />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load users. Please try again.')).toBeInTheDocument();
    });
  });
});
