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

  // Reset page when filter/sort changes
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, sortBy]);

  const fetchUsers = useCallback(async (pageNumber: number, search: string, sort?: string) => {
    setLoading(true);
    try {
      const data = await adminService.getUsers(pageNumber, 10, search, sort);
      setUsers(data.items);
      setTotalPages(data.totalPages);
    } catch (error) {
      console.error('Failed to fetch users', error);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchUsers(page, debouncedSearch, sortBy);
  }, [page, debouncedSearch, sortBy, fetchUsers]);

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

  const nextPage = () => setPage(p => Math.min(totalPages, p + 1));
  const prevPage = () => setPage(p => Math.max(1, p - 1));

  const toggleSort = (field: string) => {
    setSortBy(current => {
      if (current === `${field}_asc`) return `${field}_desc`;
      if (current === `${field}_desc`) return undefined;
      return `${field}_asc`;
    });
  };

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
    refresh: () => fetchUsers(page, debouncedSearch, sortBy)
  };
};
