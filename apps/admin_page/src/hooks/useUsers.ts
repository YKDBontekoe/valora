import { useState, useEffect, useCallback } from 'react';
import { adminService } from '../services/api';
import type { User } from '../types';

export const useUsers = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [sortBy, setSortBy] = useState<string | undefined>(undefined);
  const currentUserId = localStorage.getItem('admin_userId');

  // Debounce search query
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchQuery);
    }, 500);
    return () => clearTimeout(handler);
  }, [searchQuery]);

  // Combined effect to handle fetching and avoid race conditions
  useEffect(() => {
    let ignore = false;

    const fetchData = async () => {
      setLoading(true);
      try {
        const data = await adminService.getUsers(page, 10, debouncedSearch, sortBy);
        if (!ignore) {
          setUsers(data.items);
          setTotalPages(Math.max(1, data.totalPages));
        }
      } catch (error) {
        if (!ignore) console.error('Failed to fetch users', error);
      } finally {
        if (!ignore) setLoading(false);
      }
    };

    fetchData();

    return () => {
      ignore = true;
    };
  }, [page, debouncedSearch, sortBy]);

  // When debounced search or sort changes, we MUST reset to page 1.
  // However, `setPage(1)` triggers the main effect again if page changed.
  // The standard way to avoid double fetch is to make the fetch dependent on page change,
  // OR handle the reset logic inside the fetch effect (complex),
  // OR just accept that React 18+ strict mode double-invokes anyway and we have `ignore` flag.
  // BUT:
  // 1. User types "test" -> debouncedSearch changes -> effect runs with OLD page (say 5) -> 404/Empty? -> ignore=false -> updates state.
  // 2. Separate effect sets page=1 -> effect runs with NEW page (1) -> fetch -> ignore=false -> updates state.
  // Result: User sees empty state flash then correct results.
  // To fix: We need to coordinate.

  // Ref-based coordination to detect if we should fetch.
  // If search/sort changed, we set page=1 and DO NOT fetch yet (return early?).
  // But we can't skip the fetch easily in the effect.

  // Better: Only reset page if it's NOT 1.
  useEffect(() => {
     if (page !== 1) {
         setPage(1);
     }
  }, [debouncedSearch, sortBy]);

  const deleteUser = async (user: User) => {
    if (user.id === currentUserId) {
      throw new Error('You cannot delete your own account.');
    }

    try {
      await adminService.deleteUser(user.id);
      setUsers(prev => prev.filter(u => u.id !== user.id));
    } catch (error) {
      console.error('Failed to delete user', error);
      throw error;
    }
  };

  const nextPage = () => setPage(p => Math.min(Math.max(totalPages, 1), p + 1));
  const prevPage = () => setPage(p => Math.max(1, p - 1));

  const toggleSort = (field: string) => {
    setSortBy(current => {
      if (current === `${field}_asc`) return `${field}_desc`;
      if (current === `${field}_desc`) return undefined;
      return `${field}_asc`;
    });
  };

  const refresh = useCallback(async () => {
    setLoading(true);
    try {
        const data = await adminService.getUsers(page, 10, debouncedSearch, sortBy);
        setUsers(data.items);
        setTotalPages(Math.max(1, data.totalPages));
    } catch (error) {
        console.error('Failed to refresh users', error);
    } finally {
        setLoading(false);
    }
  }, [page, debouncedSearch, sortBy]);

  return {
    users,
    loading,
    page,
    totalPages,
    currentUserId,
    deleteUser,
    setPage,
    nextPage,
    prevPage,
    searchQuery,
    setSearchQuery,
    sortBy,
    toggleSort,
    refresh
  };
};
