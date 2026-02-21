import { useState } from 'react';
import type { User } from '../types';
import { ArrowUp, ArrowDown, ChevronLeft, ChevronRight, Loader2, Search, UserPlus } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { useUsers } from '../hooks/useUsers';
import ConfirmationDialog from '../components/ConfirmationDialog';
import CreateUserModal from '../components/CreateUserModal';
import UserRow from '../components/UserRow';
import Skeleton from '../components/Skeleton';
import Button from '../components/Button';

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
    toggleSort,
    refresh
  } = useUsers();

  const [deleteConfirmation, setDeleteConfirmation] = useState<{ isOpen: boolean; user: User | null }>({
    isOpen: false,
    user: null,
  });

  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
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
      setDeleteConfirmation({ isOpen: false, user: null });
    }
  };

  return (
    <div className="space-y-8">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-4xl font-black text-brand-900 tracking-tight">User Management</h1>
            {loading && users.length > 0 && (
              <Loader2 className="h-6 w-6 text-primary-600 animate-spin" />
            )}
          </div>
          <p className="text-brand-500 mt-2 font-medium">Manage administrative access and platform roles.</p>
        </div>
        <Button
            variant="secondary"
            leftIcon={<UserPlus size={18} />}
            onClick={() => setIsCreateModalOpen(true)}
        >
            Add Admin User
        </Button>
      </div>

      {error && (
        <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            className="p-4 bg-error-50 border border-error-100 rounded-2xl text-error-700 font-bold flex items-center justify-between shadow-sm"
        >
          <div className="flex items-center gap-3">
              <div className="w-1.5 h-1.5 rounded-full bg-error-500" />
              <span>{error}</span>
          </div>
          <button onClick={() => setError(null)} className="text-error-400 hover:text-error-600 transition-colors">✕</button>
        </motion.div>
      )}

      <div className="relative max-w-md group">
        <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-brand-400 group-focus-within:text-primary-500 transition-colors" />
        <input
            type="text"
            placeholder="Search users by email..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-12 pr-4 py-3.5 bg-white border border-brand-200 rounded-2xl focus:outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:shadow-lg transition-all shadow-sm font-semibold text-brand-900"
        />
      </div>

      <div className="bg-white shadow-premium rounded-3xl overflow-hidden border border-brand-100 relative">
        <div className={`overflow-x-auto transition-opacity duration-200 ${loading && users.length > 0 ? 'opacity-50' : 'opacity-100'}`}>
          <table className="min-w-full divide-y divide-brand-100">
            <thead className="bg-brand-50/50">
              <tr>
                <th
                    className="px-8 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-[0.1em] cursor-pointer group hover:bg-brand-100/50 transition-colors select-none"
                    onClick={() => toggleSort('email')}
                >
                    <div className="flex items-center gap-2">
                        Email Address
                        <div className="flex flex-col">
                            <ArrowUp className={`w-3 h-3 -mb-1 transition-colors ${sortBy === 'email_asc' ? 'text-primary-600' : 'text-brand-300 group-hover:text-brand-400'}`} />
                            <ArrowDown className={`w-3 h-3 transition-colors ${sortBy === 'email_desc' ? 'text-primary-600' : 'text-brand-300 group-hover:text-brand-400'}`} />
                        </div>
                    </div>
                </th>
                <th className="px-8 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-[0.1em]">Roles & Permissions</th>
                <th className="px-8 py-5 text-right text-[10px] font-black text-brand-400 uppercase tracking-[0.1em]">Actions</th>
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
                  [...Array(5)].map((_, i) => (
                    <tr key={`skeleton-${i}`}>
                      <td className="px-8 py-6"><Skeleton variant="text" width="60%" height={16} /></td>
                      <td className="px-8 py-6"><Skeleton variant="rectangular" width={80} height={24} className="rounded-lg" /></td>
                      <td className="px-8 py-6 text-right"><Skeleton variant="circular" width={32} height={32} className="ml-auto" /></td>
                    </tr>
                  ))
                ) : users.length === 0 ? (
                    <motion.tr
                        key="empty"
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        exit={{ opacity: 0 }}
                    >
                        <td colSpan={3} className="px-8 py-20 text-center">
                            <div className="flex flex-col items-center gap-4">
                                <div className="p-4 bg-brand-50 rounded-full">
                                    <Search className="h-8 w-8 text-brand-200" />
                                </div>
                                <span className="text-brand-500 font-bold">
                                    {searchQuery ? 'No users found matching your search.' : 'No users found.'}
                                </span>
                            </div>
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
      <div className="flex items-center justify-between px-2">
        <div className="text-[10px] font-black text-brand-400 uppercase tracking-[0.1em]">
          Page <span className="text-brand-900">{page}</span> <span className="mx-2 text-brand-200">/</span> <span className="text-brand-900">{totalPages}</span>
        </div>
        <div className="flex gap-3">
          <Button
            variant="outline"
            size="sm"
            onClick={prevPage}
            disabled={page === 1 || loading}
            leftIcon={<ChevronLeft size={14} />}
          >
            Previous
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={nextPage}
            disabled={page === totalPages || loading}
            rightIcon={<ChevronRight size={14} />}
          >
            Next
          </Button>
        </div>
      </div>

      <ConfirmationDialog
        isOpen={deleteConfirmation.isOpen}
        onClose={() => setDeleteConfirmation({ isOpen: false, user: null })}
        onConfirm={confirmDelete}
        title="Delete Administrative User"
        message={`Are you sure you want to delete ${deleteConfirmation.user?.email}? They will immediately lose all access to the admin panel.`}
        confirmLabel="Confirm Deletion"
        isDestructive
      />

      <CreateUserModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onUserCreated={refresh}
      />
    </div>
  );
};

export default Users;
