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

  it('renders nothing when closed', () => {
    const { container } = render(<DatasetStatusModal isOpen={false} onClose={() => {}} />);
    expect(container).toBeEmptyDOMElement();
  });

  it('renders loading state and then data', async () => {
    const mockData = [
      {
        city: 'Amsterdam',
        neighborhoodCount: 100,
        lastUpdated: new Date().toISOString(),
      },
    ];
    (adminService.getDatasetStatus as Mock).mockResolvedValue(mockData);

    render(<DatasetStatusModal isOpen={true} onClose={() => {}} />);

    expect(screen.getByText('Dataset Catalog')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('Amsterdam')).toBeInTheDocument();
      expect(screen.getByText('100')).toBeInTheDocument();
    });
  });

  it('refreshes data when refresh button clicked', async () => {
    (adminService.getDatasetStatus as Mock).mockResolvedValue([]);
    render(<DatasetStatusModal isOpen={true} onClose={() => {}} />);

    await waitFor(() => {
        expect(screen.getByText('Dataset Catalog')).toBeInTheDocument();
    });

    const refreshButtons = screen.getAllByRole('button');
    // The refresh button is the one with the RefreshCw icon
    fireEvent.click(refreshButtons[0]);

    await waitFor(() => {
        expect(adminService.getDatasetStatus).toHaveBeenCalledTimes(2); // Once on mount, once on click
    });
  });

  it('renders error message when API call fails', async () => {
    (adminService.getDatasetStatus as Mock).mockRejectedValue(new Error('API Error'));

    render(<DatasetStatusModal isOpen={true} onClose={() => {}} />);

    await waitFor(() => {
      expect(screen.getByText('Failed to load dataset status.')).toBeInTheDocument();
    });
  });
});
