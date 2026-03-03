import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, type Mock } from 'vitest';
import DatasetStatusModal from './DatasetStatusModal';
import { adminService } from '../services/api';

vi.mock('../services/api', () => ({
  adminService: {
    getDatasetStatus: vi.fn(),
  },
}));

describe('DatasetStatusModal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading state initially and then displays data', async () => {
    const mockData = [
      {
        city: 'Amsterdam',
        neighborhoodCount: 10,
        lastUpdated: new Date().toISOString(),
      },
      {
        city: 'Rotterdam',
        neighborhoodCount: 5,
        lastUpdated: new Date(Date.now() - 40 * 24 * 60 * 60 * 1000).toISOString(), // 40 days ago (stale)
      },
    ];
    (adminService.getDatasetStatus as Mock).mockResolvedValue(mockData);

    render(<DatasetStatusModal isOpen={true} onClose={() => {}} />);

    expect(screen.getByText('Dataset Catalog')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('Amsterdam')).toBeInTheDocument();
      expect(screen.getByText('Rotterdam')).toBeInTheDocument();
    });

    expect(screen.getByText('Fresh')).toBeInTheDocument();
    expect(screen.getByText('Stale')).toBeInTheDocument();
    expect(screen.getByText('10')).toBeInTheDocument();
    expect(screen.getByText('5')).toBeInTheDocument();
  });

  it('renders error state when API call fails', async () => {
    (adminService.getDatasetStatus as Mock).mockRejectedValue(new Error('API Error'));

    render(<DatasetStatusModal isOpen={true} onClose={() => {}} />);

    await waitFor(() => {
      expect(screen.getByText('Query Failure')).toBeInTheDocument();
      expect(screen.getByText('Failed to load dataset status.')).toBeInTheDocument();
    });

    const retryButton = screen.getByText('Retry Sync');
    fireEvent.click(retryButton);

    expect(adminService.getDatasetStatus).toHaveBeenCalledTimes(2);
  });

  it('renders empty state when no data is returned', async () => {
    (adminService.getDatasetStatus as Mock).mockResolvedValue([]);

    render(<DatasetStatusModal isOpen={true} onClose={() => {}} />);

    await waitFor(() => {
      expect(screen.getByText('Repository Empty')).toBeInTheDocument();
      expect(screen.getByText('No municipalities have been provisioned yet.')).toBeInTheDocument();
    });
  });

  it('calls onClose when close button is clicked', () => {
    const onClose = vi.fn();
    render(<DatasetStatusModal isOpen={true} onClose={onClose} />);

    const closeButton = screen.getByLabelText('Close modal');
    fireEvent.click(closeButton);

    expect(onClose).toHaveBeenCalled();
  });

  it('refreshes data when refresh button is clicked', async () => {
    (adminService.getDatasetStatus as Mock).mockResolvedValue([]);

    render(<DatasetStatusModal isOpen={true} onClose={() => {}} />);

    await waitFor(() => {
      expect(screen.getByText('Repository Empty')).toBeInTheDocument();
    });

    const refreshButton = screen.getByLabelText('Refresh data');
    fireEvent.click(refreshButton);

    expect(adminService.getDatasetStatus).toHaveBeenCalledTimes(2);
  });
});
