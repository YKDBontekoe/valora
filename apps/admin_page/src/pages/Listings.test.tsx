import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import Listings from './Listings';
import { listingService } from '../services/api';

// Mock the service
vi.mock('../services/api', async (importOriginal) => {
  const actual = await importOriginal();
  return {
    ...actual,
    listingService: {
      getListings: vi.fn(),
    },
  };
});

const mockListingsResponse = {
  items: [
    { id: '1', address: 'Test St 1', price: 500000, city: 'City A', fundaId: 'f1', status: 'active' },
    { id: '2', address: 'Test St 2', price: 300000, city: 'City B', fundaId: 'f2', status: 'sold' },
  ],
  totalPages: 2,
  pageIndex: 1,
  totalCount: 2,
  hasNextPage: true,
  hasPreviousPage: false,
};

describe('Listings Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (listingService.getListings as any).mockResolvedValue(mockListingsResponse);
  });

  it('renders listings table correctly', async () => {
    render(<Listings />);

    await waitFor(() => {
      expect(screen.getByText('Test St 1')).toBeInTheDocument();
      expect(screen.getByText('Test St 2')).toBeInTheDocument();
    });

    expect(listingService.getListings).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 10 }));
  });

  it('filters by search term (debounced)', async () => {
    render(<Listings />);
    const user = userEvent.setup();

    const searchInput = screen.getByPlaceholderText('Address, City...');
    await user.type(searchInput, 'Test');

    await waitFor(() => {
      expect(listingService.getListings).toHaveBeenCalledWith(expect.objectContaining({ searchTerm: 'Test' }));
    }, { timeout: 2000 });
  });

  it('filters by price range', async () => {
    render(<Listings />);
    const user = userEvent.setup();

    const minPriceInput = screen.getByPlaceholderText('€ 0');
    await user.type(minPriceInput, '400000');

    await waitFor(() => {
      expect(listingService.getListings).toHaveBeenCalledWith(expect.objectContaining({ minPrice: 400000 }));
    });
  });

  it('filters by city', async () => {
    render(<Listings />);
    const user = userEvent.setup();

    const cityInput = screen.getByPlaceholderText('Filter by city...');
    await user.type(cityInput, 'City A');

    await waitFor(() => {
        expect(listingService.getListings).toHaveBeenCalledWith(expect.objectContaining({ city: 'City A' }));
    });
  });

  it('sorts by price', async () => {
    render(<Listings />);
    const user = userEvent.setup();

    // Use getByRole 'combobox' which is standard for select elements
    const select = screen.getByRole('combobox');
    await user.selectOptions(select, 'Price');

    await waitFor(() => {
      expect(listingService.getListings).toHaveBeenCalledWith(expect.objectContaining({ sortBy: 'Price' }));
    });
  });

  it('toggles sort order', async () => {
    render(<Listings />);
    const user = userEvent.setup();

    // Find the toggle button by aria-label
    const toggleButton = screen.getByRole('button', { name: 'Toggle sort order' });
    await user.click(toggleButton);

    await waitFor(() => {
      expect(listingService.getListings).toHaveBeenCalledWith(expect.objectContaining({ sortOrder: 'asc' }));
    });
  });

  it('clears filters', async () => {
    render(<Listings />);
    const user = userEvent.setup();

    // Set a filter to make "Clear Filters" appear
    const cityInput = screen.getByPlaceholderText('Filter by city...');
    await user.type(cityInput, 'City A');

    await waitFor(() => {
        expect(listingService.getListings).toHaveBeenCalledWith(expect.objectContaining({ city: 'City A' }));
    });

    const clearButton = await screen.findByRole('button', { name: /clear filters/i });
    await user.click(clearButton);

    await waitFor(() => {
      // Should revert to default (empty city)
      expect(listingService.getListings).toHaveBeenCalledWith(
        expect.objectContaining({
             city: undefined,
             searchTerm: undefined
        })
      );
      expect(cityInput).toHaveValue('');
    });
  });
});
