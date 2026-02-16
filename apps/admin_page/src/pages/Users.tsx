import { useState, useEffect, useCallback } from 'react';
import { adminService } from '../services/api';
import type { User } from '../types';
import { Trash2, Loader2 } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import Pagination from '../components/Pagination';
import ConfirmationDialog from '../components/ConfirmationDialog';
import DebouncedInput from '../components/DebouncedInput';
import SortableHeader from '../components/SortableHeader';

const Users = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);

  // New state for filtering/sorting
  const [searchTerm, setSearchTerm] = useState('');
  const [sortBy, setSortBy] = useState<string>('');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc');

  // Confirmation Dialog State
  const [userToDelete, setUserToDelete] = useState<User | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const currentUserId = localStorage.getItem('admin_userId');

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    try {
      const data = await adminService.getUsers(page, 10, searchTerm, sortBy, sortOrder);
      setUsers(data.items);
      setTotalPages(data.totalPages);
    } catch {
      console.error('Failed to fetch users');
    } finally {
      setLoading(false);
    }
  }, [page, searchTerm, sortBy, sortOrder]);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  const handleSort = (field: string) => {
    if (sortBy === field) {
      setSortOrder(prev => prev === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(field);
      setSortOrder('asc');
    }
    setPage(1); // Reset to first page on sort change
  };

  const handleSearch = (term: string) => {
    setSearchTerm(term);
    setPage(1); // Reset to first page on search
  };

  const handleDeleteClick = (user: User) => {
    if (user.id === currentUserId) return;
    setUserToDelete(user);
  };

  const confirmDelete = async () => {
    if (!userToDelete) return;
    setIsDeleting(true);
    try {
      await adminService.deleteUser(userToDelete.id);
      setUsers(prev => prev.filter(u => u.id !== userToDelete.id));
      setUserToDelete(null);
    } catch {
      alert('Failed to delete user. It might be protected or you might have lost permissions.');
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-center justify-between mb-8 gap-4">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-3xl font-bold text-brand-900">User Management</h1>
            {loading && users.length > 0 && (
              <Loader2 className="h-6 w-6 text-primary-600 animate-spin" />
            )}
          </div>
          <p className="text-brand-500 mt-1">Manage administrative access and roles.</p>
        </div>
        <div className="w-full sm:w-64">
          <DebouncedInput
            value={searchTerm}
            onChange={handleSearch}
            placeholder="Search by email..."
          />
        </div>
      </div>

      <div className="bg-white shadow-premium rounded-2xl overflow-hidden border border-brand-100 relative">
        <div className={`overflow-x-auto transition-opacity duration-200 ${loading && users.length > 0 ? 'opacity-50' : 'opacity-100'}`}>
          <table className="min-w-full divide-y divide-brand-100">
            <thead className="bg-brand-50">
              <tr>
                <SortableHeader
                  label="Email"
                  field="email"
                  currentSortBy={sortBy}
                  currentSortOrder={sortOrder}
                  onSort={handleSort}
                />
                <th className="px-8 py-4 text-left text-xs font-bold text-brand-500 uppercase tracking-widest">Roles</th>
                <th className="px-8 py-4 text-right text-xs font-bold text-brand-500 uppercase tracking-widest">Actions</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-brand-100">
              <AnimatePresence mode="popLayout">
                {loading && users.length === 0 ? (
                  <tr>
                    <td colSpan={3} className="px-8 py-12 text-center">
                      <div className="flex flex-col items-center">
                        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mb-4"></div>
                        <span className="text-brand-500 font-medium">Loading users...</span>
                      </div>
                    </td>
                  </tr>
                ) : users.length === 0 ? (
                  <tr>
                    <td colSpan={3} className="px-8 py-12 text-center text-brand-500">
                      No users found matching your search.
                    </td>
                  </tr>
                ) : (
                  users.map((user) => (
                    <motion.tr
                      key={user.id}
                      initial={{ opacity: 0 }}
                      animate={{ opacity: 1 }}
                      exit={{ opacity: 0 }}
                      layout
                      className="hover:bg-brand-50/50 transition-colors"
                    >
                      <td className="px-8 py-5 whitespace-nowrap text-sm font-semibold text-brand-900">{user.email}</td>
                      <td className="px-8 py-5 whitespace-nowrap text-sm text-brand-500">
                        <div className="flex flex-wrap gap-2">
                          {user.roles.map(role => (
                            <span key={role} className="px-3 py-1 inline-flex text-xs leading-5 font-bold rounded-full bg-primary-50 text-primary-700">
                              {role}
                            </span>
                          ))}
                        </div>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap text-right text-sm font-medium">
                        <button
                          onClick={() => handleDeleteClick(user)}
                          disabled={user.id === currentUserId || loading}
                          className={`p-2 rounded-lg transition-all ${
                            user.id === currentUserId || loading
                              ? 'text-brand-200 cursor-not-allowed'
                              : 'text-brand-400 hover:text-red-600 hover:bg-red-50 cursor-pointer'
                          }`}
                          title={user.id === currentUserId ? 'You cannot delete yourself' : loading ? 'Please wait...' : 'Delete user'}
                        >
                          <Trash2 className="h-5 w-5" />
                        </button>
                      </td>
                    </motion.tr>
                  ))
                )}
              </AnimatePresence>
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <Pagination
          currentPage={page}
          totalPages={totalPages}
          onPageChange={setPage}
          isLoading={loading}
        />
      </div>

      <ConfirmationDialog
        isOpen={!!userToDelete}
        title="Delete User"
        message={`Are you sure you want to delete ${userToDelete?.email}? This action cannot be undone.`}
        isDestructive={true}
        onConfirm={confirmDelete}
        onCancel={() => setUserToDelete(null)}
        isLoading={isDeleting}
        confirmLabel="Delete User"
      />
    </div>
  );
};

export default Users;
