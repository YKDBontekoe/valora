import { useState, useEffect, useCallback } from 'react';
import { adminService } from '../services/api';
import type { User } from '../types';

export const useUsers = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const currentUserId = localStorage.getItem('admin_userId');

  const fetchUsers = useCallback(async (pageNumber: number) => {
    setLoading(true);
    try {
      const data = await adminService.getUsers(pageNumber);
      setUsers(data.items);
      setTotalPages(data.totalPages);
    } catch (error) {
      console.error('Failed to fetch users', error);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchUsers(page);
  }, [page, fetchUsers]);

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
    refresh: () => fetchUsers(page)
  };
};
