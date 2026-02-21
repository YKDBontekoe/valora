import { useState, useEffect, useCallback } from 'react';
import { adminService } from '../services/api';
import type { User } from '../types';

export const useUsers = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [sortBy, setSortBy] = useState<string | undefined>(undefined);
  const [refreshTrigger, setRefreshTrigger] = useState(0); // Trigger for manual refresh
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
      setError(null);
      try {
        const data = await adminService.getUsers(page, 10, debouncedSearch, sortBy);
        if (!ignore) {
          setUsers(data.items);
          setTotalPages(Math.max(1, data.totalPages));
        }
      } catch (err: unknown) {
        if (!ignore) {
            console.error('Failed to fetch users', err);
            setError('Failed to load users. Please try again.');
        }
      } finally {
        if (!ignore) setLoading(false);
      }
    };

    fetchData();

    return () => {
      ignore = true;
    };
  }, [page, debouncedSearch, sortBy, refreshTrigger]);

  // When debounced search or sort changes, we reset to page 1.
  // We rely on React's behavior to bail out if state is already 1, avoiding unnecessary updates.
  useEffect(() => {
     setPage(1);
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

  const refresh = useCallback(() => {
    setRefreshTrigger(prev => prev + 1);
  }, []);

  return {
    users,
    loading,
    error,
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
