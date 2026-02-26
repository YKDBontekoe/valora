import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import DatasetStatusModal from './DatasetStatusModal';
import { adminService } from '../services/api';

// Mock the adminService
vi.mock('../services/api', () => ({
  adminService: {
    getDatasetStatus: vi.fn(),
  },
}));

describe('DatasetStatusModal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('does not render when closed', () => {
    const { container } = render(<DatasetStatusModal isOpen={false} onClose={() => {}} />);
    expect(container.firstChild).toBeNull();
  });

  it('renders loading state initially', async () => {
    (adminService.getDatasetStatus as any).mockReturnValue(new Promise(() => {})); // Never resolves
    render(<DatasetStatusModal isOpen={true} onClose={() => {}} />);

    expect(screen.getByText('Dataset Status')).toBeInTheDocument();
  });

  it('renders dataset items after loading', async () => {
    const mockData = [
      { city: 'Rotterdam', neighborhoodCount: 10, lastUpdated: new Date().toISOString() },
      { city: 'Amsterdam', neighborhoodCount: 20, lastUpdated: '2020-01-01T00:00:00Z' }, // Stale
    ];
    (adminService.getDatasetStatus as any).mockResolvedValue(mockData);

    render(<DatasetStatusModal isOpen={true} onClose={() => {}} />);

    await waitFor(() => {
      expect(screen.getByText('Rotterdam')).toBeInTheDocument();
      expect(screen.getByText('Amsterdam')).toBeInTheDocument();
    });

    expect(screen.getByText('Fresh')).toBeInTheDocument();
    expect(screen.getByText('Stale')).toBeInTheDocument();
  });

  it('renders error state when API fails', async () => {
    (adminService.getDatasetStatus as any).mockRejectedValue(new Error('API Error'));

    render(<DatasetStatusModal isOpen={true} onClose={() => {}} />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load dataset status.')).toBeInTheDocument();
    });

    const retryButton = screen.getByText('Retry Sync');
    fireEvent.click(retryButton);
    expect(adminService.getDatasetStatus).toHaveBeenCalledTimes(2);
  });

  it('calls onClose when X button is clicked', async () => {
    (adminService.getDatasetStatus as any).mockResolvedValue([]);
    const handleClose = vi.fn();
    render(<DatasetStatusModal isOpen={true} onClose={handleClose} />);

    await waitFor(() => {
        expect(screen.getByText('Dataset Status')).toBeInTheDocument();
    });

    const closeButton = screen.getByLabelText('Close modal');
    fireEvent.click(closeButton);
    expect(handleClose).toHaveBeenCalled();
  });
});
