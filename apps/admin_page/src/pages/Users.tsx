import { useState, useEffect } from 'react';
import { adminService } from '../services/api';
import type { User } from '../types';
import { Trash2, ChevronLeft, ChevronRight } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';

const Users = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const currentUserId = localStorage.getItem('admin_userId');

  useEffect(() => {
    fetchUsers(page);
  }, [page]);

  const fetchUsers = async (pageNumber: number) => {
    setLoading(true);
    try {
      const data = await adminService.getUsers(pageNumber);
      setUsers(data.items);
      setTotalPages(data.totalPages);
    } catch {
      console.error('Failed to fetch users');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (user: User) => {
    if (user.id === currentUserId) {
      alert('You cannot delete your own account.');
      return;
    }

    if (!window.confirm(`Are you sure you want to delete user ${user.email}?`)) return;

    try {
      await adminService.deleteUser(user.id);
      setUsers(users.filter(u => u.id !== user.id));
    } catch {
      alert('Failed to delete user. It might be protected or you might have lost permissions.');
    }
  };

  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-center justify-between mb-8 gap-4">
        <div>
          <h1 className="text-3xl font-bold text-brand-900">User Management</h1>
          <p className="text-brand-500 mt-1">Manage administrative access and roles.</p>
        </div>
      </div>

      <div className="bg-white shadow-premium rounded-2xl overflow-hidden border border-brand-100">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-brand-100">
            <thead className="bg-brand-50">
              <tr>
                <th className="px-8 py-4 text-left text-xs font-bold text-brand-500 uppercase tracking-widest">Email</th>
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
                          onClick={() => handleDelete(user)}
                          disabled={user.id === currentUserId}
                          className={`p-2 rounded-lg transition-all ${
                            user.id === currentUserId
                              ? 'text-brand-200 cursor-not-allowed'
                              : 'text-brand-400 hover:text-red-600 hover:bg-red-50 cursor-pointer'
                          }`}
                          title={user.id === currentUserId ? 'You cannot delete yourself' : 'Delete user'}
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
      </div>

      {/* Pagination */}
      <div className="mt-8 flex items-center justify-between px-2">
        <div className="text-sm font-medium text-brand-500">
          Page <span className="text-brand-900">{page}</span> of <span className="text-brand-900">{totalPages}</span>
        </div>
        <div className="flex space-x-3">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1 || loading}
            className="flex items-center px-4 py-2 border border-brand-200 rounded-xl text-sm font-semibold text-brand-700 bg-white hover:bg-brand-50 disabled:opacity-40 disabled:hover:bg-white transition-all cursor-pointer"
          >
            <ChevronLeft className="mr-1 h-4 w-4" />
            Previous
          </button>
          <button
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page === totalPages || loading}
            className="flex items-center px-4 py-2 border border-brand-200 rounded-xl text-sm font-semibold text-brand-700 bg-white hover:bg-brand-50 disabled:opacity-40 disabled:hover:bg-white transition-all cursor-pointer"
          >
            Next
            <ChevronRight className="ml-1 h-4 w-4" />
          </button>
        </div>
      </div>
    </div>
  );
};

export default Users;
