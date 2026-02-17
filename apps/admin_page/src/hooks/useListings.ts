import { useState, useEffect, useRef, useCallback } from 'react';
import { listingService } from '../services/api';
import type { Listing } from '../types';

export const useListings = (pageSize = 10) => {
  const [listings, setListings] = useState<Listing[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const lastSuccessPage = useRef(1);

  const fetchListings = useCallback(async (pageNumber: number) => {
    setLoading(true);
    try {
      const data = await listingService.getListings(pageNumber, pageSize);
      setListings(data.items || []);
      setTotalPages(data.totalPages || 1);
      lastSuccessPage.current = pageNumber;
    } catch {
      // Log sanitized error or nothing
      console.error('Failed to fetch listings. Please try again.');
      // Revert page on failure to keep UI in sync
      setPage(lastSuccessPage.current);
    } finally {
      setLoading(false);
    }
  }, [pageSize]);

  useEffect(() => {
    fetchListings(page);
  }, [page, fetchListings]);

  const nextPage = () => {
    if (page < totalPages) setPage(p => p + 1);
  };

  const prevPage = () => {
    if (page > 1) setPage(p => p - 1);
  };

  return {
    listings,
    loading,
    page,
    totalPages,
    setPage,
    nextPage,
    prevPage,
    refresh: () => fetchListings(page)
  };
};
