import { useState } from 'react';
import type { User } from '../types';
import { Trash2, ChevronLeft, ChevronRight, Loader2 } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { useUsers } from '../hooks/useUsers';
import ConfirmationDialog from '../components/ConfirmationDialog';

const UserRow = ({
  user,
  currentUserId,
  loading,
  onDeleteClick
}: {
  user: User;
  currentUserId: string | null;
  loading: boolean;
  onDeleteClick: (user: User) => void;
}) => (
  <motion.tr
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
        onClick={() => onDeleteClick(user)}
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
);

const Users = () => {
  const {
    users,
    loading,
    page,
    totalPages,
    currentUserId,
    deleteUser,
    nextPage,
    prevPage
  } = useUsers();

  const [deleteConfirmation, setDeleteConfirmation] = useState<{ isOpen: boolean; user: User | null }>({
    isOpen: false,
    user: null,
  });

  const handleDeleteClick = (user: User) => {
    if (user.id === currentUserId) return;
    setDeleteConfirmation({ isOpen: true, user });
  };

  const confirmDelete = async () => {
    if (!deleteConfirmation.user) return;
    try {
      await deleteUser(deleteConfirmation.user);
    } catch {
      alert('Failed to delete user. It might be protected or you might have lost permissions.');
    }
    setDeleteConfirmation({ isOpen: false, user: null });
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
      </div>

      <div className="bg-white shadow-premium rounded-2xl overflow-hidden border border-brand-100 relative">
        <div className={`overflow-x-auto transition-opacity duration-200 ${loading && users.length > 0 ? 'opacity-50' : 'opacity-100'}`}>
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
                    <UserRow
                      key={user.id}
                      user={user}
                      currentUserId={currentUserId}
                      loading={loading}
                      onDeleteClick={handleDeleteClick}
                    />
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
            onClick={prevPage}
            disabled={page === 1 || loading}
            className="flex items-center px-4 py-2 border border-brand-200 rounded-xl text-sm font-semibold text-brand-700 bg-white hover:bg-brand-50 disabled:opacity-40 disabled:hover:bg-white transition-all cursor-pointer"
          >
            <ChevronLeft className="mr-1 h-4 w-4" />
            Previous
          </button>
          <button
            onClick={nextPage}
            disabled={page === totalPages || loading}
            className="flex items-center px-4 py-2 border border-brand-200 rounded-xl text-sm font-semibold text-brand-700 bg-white hover:bg-brand-50 disabled:opacity-40 disabled:hover:bg-white transition-all cursor-pointer"
          >
            Next
            <ChevronRight className="ml-1 h-4 w-4" />
          </button>
        </div>
      </div>

      <ConfirmationDialog
        isOpen={deleteConfirmation.isOpen}
        onClose={() => setDeleteConfirmation({ isOpen: false, user: null })}
        onConfirm={confirmDelete}
        title="Delete User"
        message={`Are you sure you want to delete user ${deleteConfirmation.user?.email}? This action cannot be undone.`}
        confirmLabel="Delete"
        isDestructive
      />
    </div>
  );
};

export default Users;
