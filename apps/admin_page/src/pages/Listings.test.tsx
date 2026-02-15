import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import Listings from './Listings';
import { adminService } from '../services/api';

// Mock the API service
vi.mock('../services/api', () => ({
  adminService: {
    getListings: vi.fn(),
  },
}));

describe('Listings', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders loading state initially', () => {
    vi.mocked(adminService.getListings).mockImplementation(() => new Promise(() => {}));
    render(<Listings />);
    expect(screen.queryByText('Address')).not.toBeInTheDocument();
  });

  it('renders listings when loaded successfully', async () => {
    vi.mocked(adminService.getListings).mockResolvedValue({
      items: [
        { id: '1', address: '123 Main St', price: 500000, city: 'Amsterdam' },
        { id: '2', address: '456 Oak Ave', price: 300000, city: 'Rotterdam' },
      ],
      totalPages: 1,
      totalCount: 2,
    });

    render(<Listings />);

    await waitFor(() => {
      expect(screen.getByText('123 Main St')).toBeInTheDocument();
      expect(screen.getByText('€500,000')).toBeInTheDocument();
      expect(screen.getByText('Amsterdam')).toBeInTheDocument();
      expect(screen.getByText('456 Oak Ave')).toBeInTheDocument();
    });
  });

  it('renders error state on failure', async () => {
    vi.mocked(adminService.getListings).mockRejectedValue(new Error('Failed to fetch'));

    render(<Listings />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load listings')).toBeInTheDocument();
      expect(screen.getByText('Try Again')).toBeInTheDocument();
    });
  });

  it('retries fetching listings when retry button is clicked', async () => {
    vi.mocked(adminService.getListings)
      .mockRejectedValueOnce(new Error('Failed to fetch'))
      .mockResolvedValueOnce({
        items: [{ id: '1', address: '123 Main St', price: 500000, city: 'Amsterdam' }],
        totalPages: 1,
        totalCount: 1,
      });

    render(<Listings />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load listings')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Try Again'));

    await waitFor(() => {
      expect(screen.getByText('123 Main St')).toBeInTheDocument();
    });

    expect(adminService.getListings).toHaveBeenCalledTimes(2);
  });
});
