import { useState } from 'react';
import type { User } from '../types';
import { ArrowUp, ArrowDown, ChevronLeft, ChevronRight, Loader2, Search } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { useUsers } from '../hooks/useUsers';
import ConfirmationDialog from '../components/ConfirmationDialog';
import UserRow from '../components/UserRow';

const tbodyVariants = {
  visible: {
    transition: {
      staggerChildren: 0.05
    }
  }
};

const Users = () => {
  const {
    users,
    loading,
    page,
    totalPages,
    currentUserId,
    deleteUser,
    nextPage,
    prevPage,
    searchQuery,
    setSearchQuery,
    sortBy,
    toggleSort
  } = useUsers();

  const [deleteConfirmation, setDeleteConfirmation] = useState<{ isOpen: boolean; user: User | null }>({
    isOpen: false,
    user: null,
  });

  const [error, setError] = useState<string | null>(null);

  const handleDeleteClick = (user: User) => {
    if (user.id === currentUserId) return;
    setDeleteConfirmation({ isOpen: true, user });
    setError(null);
  };

  const confirmDelete = async () => {
    if (!deleteConfirmation.user) return;
    try {
      await deleteUser(deleteConfirmation.user);
      setDeleteConfirmation({ isOpen: false, user: null });
    } catch {
      setError('Failed to delete user. It might be protected or you might have lost permissions.');
      // Keep dialog open or close it? Close it and show error banner or keep open with error inside?
      // Let's keep dialog open and show error inside confirmation dialog? No, ConfirmDialog is generic.
      // Let's close dialog and show page error.
      setDeleteConfirmation({ isOpen: false, user: null });
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
      </div>

      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-xl text-red-600 flex items-center justify-between">
          <span>{error}</span>
          <button onClick={() => setError(null)} className="text-red-400 hover:text-red-600 font-bold">✕</button>
        </div>
      )}

      <div className="mb-6 relative max-w-md">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-brand-400" />
        <input
            type="text"
            placeholder="Search users by email..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-brand-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent transition-all shadow-sm"
        />
      </div>

      <div className="bg-white shadow-premium rounded-2xl overflow-hidden border border-brand-100 relative">
        <div className={`overflow-x-auto transition-opacity duration-200 ${loading && users.length > 0 ? 'opacity-50' : 'opacity-100'}`}>
          <table className="min-w-full divide-y divide-brand-100">
            <thead className="bg-brand-50">
              <tr>
                <th
                    className="px-8 py-4 text-left text-xs font-bold text-brand-500 uppercase tracking-widest cursor-pointer group hover:bg-brand-100/50 transition-colors select-none"
                    onClick={() => toggleSort('email')}
                >
                    <div className="flex items-center gap-2">
                        Email
                        {sortBy === 'email_asc' && <ArrowUp className="w-3 h-3 text-brand-700" />}
                        {sortBy === 'email_desc' && <ArrowDown className="w-3 h-3 text-brand-700" />}
                        {!sortBy?.includes('email') && <ArrowUp className="w-3 h-3 opacity-0 group-hover:opacity-30 transition-opacity" />}
                    </div>
                </th>
                <th className="px-8 py-4 text-left text-xs font-bold text-brand-500 uppercase tracking-widest">Roles</th>
                <th className="px-8 py-4 text-right text-xs font-bold text-brand-500 uppercase tracking-widest">Actions</th>
              </tr>
            </thead>
            <motion.tbody
              initial="hidden"
              animate="visible"
              variants={tbodyVariants}
              className="bg-white divide-y divide-brand-100"
            >
              <AnimatePresence mode="popLayout">
                {loading && users.length === 0 ? (
                  <motion.tr
                    key="loading"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                  >
                    <td colSpan={3} className="px-8 py-12 text-center">
                      <div className="flex flex-col items-center">
                        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mb-4"></div>
                        <span className="text-brand-500 font-medium">Loading users...</span>
                      </div>
                    </td>
                  </motion.tr>
                ) : users.length === 0 ? (
                    <motion.tr
                        key="empty"
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        exit={{ opacity: 0 }}
                    >
                        <td colSpan={3} className="px-8 py-16 text-center text-brand-400">
                            {searchQuery ? 'No users found matching your search.' : 'No users found.'}
                        </td>
                    </motion.tr>
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
            </motion.tbody>
          </table>
        </div>
      </div>

      {/* Pagination */}
      <div className="mt-8 flex items-center justify-between px-2">
        <div className="text-sm font-bold text-brand-400 uppercase tracking-widest">
          Page <span className="text-brand-900">{page}</span> <span className="mx-1 text-brand-200">/</span> <span className="text-brand-900">{totalPages}</span>
        </div>
        <div className="flex space-x-3">
          <motion.button
            whileTap={{ scale: 0.95 }}
            onClick={prevPage}
            disabled={page === 1 || loading}
            className="flex items-center px-5 py-2.5 border border-brand-200 rounded-xl text-xs font-bold uppercase tracking-widest text-brand-700 bg-white shadow-sm hover:bg-brand-50 hover:border-brand-300 disabled:opacity-40 disabled:hover:bg-white disabled:shadow-none transition-all cursor-pointer"
          >
            <ChevronLeft className="mr-2 h-4 w-4" />
            Previous
          </motion.button>
          <motion.button
            whileTap={{ scale: 0.95 }}
            onClick={nextPage}
            disabled={page === totalPages || loading}
            className="flex items-center px-5 py-2.5 border border-brand-200 rounded-xl text-xs font-bold uppercase tracking-widest text-brand-700 bg-white shadow-sm hover:bg-brand-50 hover:border-brand-300 disabled:opacity-40 disabled:hover:bg-white disabled:shadow-none transition-all cursor-pointer"
          >
            Next
            <ChevronRight className="ml-2 h-4 w-4" />
          </motion.button>
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
