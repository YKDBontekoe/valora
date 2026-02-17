import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import api, { listingService } from './api';

describe('Listing Service', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('getListings', () => {
    it('calls API with default parameters when no filter is provided', async () => {
      const getSpy = vi.spyOn(api, 'get').mockResolvedValue({
        data: { items: [], totalCount: 0 }
      });

      await listingService.getListings();

      expect(getSpy).toHaveBeenCalledWith('/listings?page=1&pageSize=10');
    });

    it('calls API with search term', async () => {
      const getSpy = vi.spyOn(api, 'get').mockResolvedValue({
        data: { items: [], totalCount: 0 }
      });

      await listingService.getListings({ searchTerm: 'test' });

      // Order of query params might vary depending on implementation detail but URLSearchParams usually preserves insertion order or specific browser behavior.
      // However, we can just check if the string contains the expected params.
      // Or we can check the exact string if we know the implementation.
      // My implementation: page, pageSize, then filters in specific order.
      expect(getSpy).toHaveBeenCalledWith(expect.stringContaining('searchTerm=test'));
      expect(getSpy).toHaveBeenCalledWith(expect.stringContaining('page=1'));
    });

    it('calls API with price range filters', async () => {
      const getSpy = vi.spyOn(api, 'get').mockResolvedValue({
        data: { items: [], totalCount: 0 }
      });

      await listingService.getListings({ minPrice: 100, maxPrice: 500 });

      expect(getSpy).toHaveBeenCalledWith(expect.stringContaining('minPrice=100'));
      expect(getSpy).toHaveBeenCalledWith(expect.stringContaining('maxPrice=500'));
    });

    it('calls API with city filter', async () => {
      const getSpy = vi.spyOn(api, 'get').mockResolvedValue({
        data: { items: [], totalCount: 0 }
      });

      await listingService.getListings({ city: 'Amsterdam' });

      expect(getSpy).toHaveBeenCalledWith(expect.stringContaining('city=Amsterdam'));
    });

    it('calls API with sorting parameters', async () => {
      const getSpy = vi.spyOn(api, 'get').mockResolvedValue({
        data: { items: [], totalCount: 0 }
      });

      await listingService.getListings({ sortBy: 'Price', sortOrder: 'asc' });

      expect(getSpy).toHaveBeenCalledWith(expect.stringContaining('sortBy=Price'));
      expect(getSpy).toHaveBeenCalledWith(expect.stringContaining('sortOrder=asc'));
    });

    it('calls API with all parameters combined', async () => {
      const getSpy = vi.spyOn(api, 'get').mockResolvedValue({
        data: { items: [], totalCount: 0 }
      });

      await listingService.getListings({
        page: 2,
        pageSize: 20,
        searchTerm: 'query',
        minPrice: 1000,
        city: 'Rotterdam',
        sortBy: 'City',
        sortOrder: 'desc'
      });

      const callArg = getSpy.mock.calls[0][0];

      expect(callArg).toContain('page=2');
      expect(callArg).toContain('pageSize=20');
      expect(callArg).toContain('searchTerm=query');
      expect(callArg).toContain('minPrice=1000');
      expect(callArg).toContain('city=Rotterdam');
      expect(callArg).toContain('sortBy=City');
      expect(callArg).toContain('sortOrder=desc');
    });

    it('handles numeric 0 values correctly (e.g., minPrice=0)', async () => {
        const getSpy = vi.spyOn(api, 'get').mockResolvedValue({
          data: { items: [], totalCount: 0 }
        });

        // 0 should be included, not treated as falsy/undefined
        await listingService.getListings({ minPrice: 0 });

        expect(getSpy).toHaveBeenCalledWith(expect.stringContaining('minPrice=0'));
      });
  });
});
