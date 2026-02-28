import { useState } from 'react';
import type { User } from '../types';
import { ArrowUp, ArrowDown, ChevronLeft, ChevronRight, Loader2, Search, UserPlus, AlertCircle, X } from 'lucide-react';
import { showToast } from '../services/toast';
import { motion, AnimatePresence } from 'framer-motion';
import { useUsers } from '../hooks/useUsers';
import ConfirmationDialog from '../components/ConfirmationDialog';
import UserRow from '../components/UserRow';
import Skeleton from '../components/Skeleton';
import Button from '../components/Button';

const listVariants = {
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
    error: fetchError,
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

  const handleDeleteClick = (user: User) => {
    if (user.id === currentUserId) return;
    setDeleteConfirmation({ isOpen: true, user });
  };

  const confirmDelete = async () => {
    if (!deleteConfirmation.user) return;
    try {
      await deleteUser(deleteConfirmation.user);
      setDeleteConfirmation({ isOpen: false, user: null });
      showToast('User access revoked successfully.', 'success');
    } catch {
      showToast('Failed to delete user. It might be protected or you might have lost permissions.', 'error');
      setDeleteConfirmation({ isOpen: false, user: null });
    }
  };

  return (
    <div className="space-y-12">
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-8">
        <div>
          <div className="flex items-center gap-4 mb-4">
            <h1 className="text-5xl lg:text-6xl font-black text-brand-900 tracking-tightest">Users</h1>
            {loading && users.length > 0 && (
              <motion.div
                initial={{ opacity: 0, scale: 0.8 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.8 }}
              >
                <Loader2 className="h-8 w-8 text-primary-600 animate-spin" />
              </motion.div>
            )}
          </div>
          <p className="text-brand-400 font-bold text-lg">Manage administrative access and platform roles.</p>
        </div>
        <Button variant="secondary" leftIcon={<UserPlus size={20} />} disabled className="px-8 py-4 shadow-glow">
            Provision New Admin
        </Button>
      </div>

      {/* Fetch Error Alert */}
      <AnimatePresence mode="wait">
        {fetchError && (
            <motion.div
                initial={{ opacity: 0, y: -20, scale: 0.95 }}
                animate={{ opacity: 1, y: 0, scale: 1 }}
                exit={{ opacity: 0, y: -20, scale: 0.95 }}
                className="p-8 bg-error-50 border border-error-100 rounded-4xl text-error-700 font-black flex flex-col sm:flex-row items-center justify-between gap-6 shadow-premium shadow-error-100/20 relative overflow-hidden"
            >
                <div className="absolute top-0 right-0 p-4 opacity-10 text-error-500">
                    <AlertCircle size={80} />
                </div>
                <div className="flex items-center gap-6 relative z-10">
                    <div className="p-4 bg-white rounded-2xl shadow-sm text-error-600 border border-error-100">
                        <AlertCircle className="w-8 h-8" />
                    </div>
                    <div className="flex flex-col">
                        <span className="text-[10px] uppercase tracking-[0.2em] text-error-500 mb-1">Service Interruption</span>
                        <span className="text-xl tracking-tight">{fetchError}</span>
                    </div>
                </div>
                <Button onClick={refresh} variant="outline" size="sm" className="border-error-200 text-error-700 hover:bg-error-100 bg-white shadow-sm relative z-10 font-black">
                    Restart Session Sync
                </Button>
            </motion.div>
        )}
      </AnimatePresence>

      <div className="flex flex-col sm:flex-row items-center gap-6 justify-between">
          <div className="relative w-full max-w-xl group">
            <Search className="absolute left-5 top-1/2 -translate-y-1/2 h-6 w-6 text-brand-300 group-focus-within:text-primary-500 transition-all duration-300 group-focus-within:scale-110" />
            <input
                type="text"
                placeholder="Search enterprise users by email..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-14 pr-12 py-5 bg-white border border-brand-100 rounded-3xl focus:outline-none focus:ring-8 focus:ring-primary-500/5 focus:border-primary-500 transition-all shadow-premium hover:shadow-premium-lg font-black text-brand-900 placeholder:text-brand-200"
            />
            {searchQuery && (
                <button
                    onClick={() => setSearchQuery('')}
                    className="absolute right-5 top-1/2 -translate-y-1/2 p-1 hover:bg-brand-50 rounded-full text-brand-300 hover:text-brand-600 transition-colors"
                >
                    <X size={18} />
                </button>
            )}
          </div>

          <div className="flex items-center gap-2 px-4 py-2 bg-brand-100/50 rounded-2xl border border-brand-200/50">
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">Displaying</span>
              <span className="text-xs font-black text-brand-900">{users.length} Records</span>
          </div>
      </div>

      <div className="bg-white shadow-premium rounded-5xl overflow-hidden border border-brand-100 relative">
        <div className={`overflow-x-auto transition-opacity duration-500 ${loading && users.length > 0 ? 'opacity-50' : 'opacity-100'}`}>
          <table className="min-w-full divide-y divide-brand-100">
            <thead className="bg-brand-50/50">
              <tr>
                <th
                    className="px-10 py-6 text-left text-[10px] font-black text-brand-400 uppercase tracking-[0.25em] cursor-pointer group hover:bg-brand-100/50 transition-colors select-none"
                    onClick={() => toggleSort('email')}
                >
                    <div className="flex items-center gap-2">
                        Identity
                        <div className="flex flex-col">
                            <ArrowUp className={`w-3 h-3 -mb-1 transition-colors ${sortBy === 'email_asc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                            <ArrowDown className={`w-3 h-3 transition-colors ${sortBy === 'email_desc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                        </div>
                    </div>
                </th>
                <th className="px-10 py-6 text-left text-[10px] font-black text-brand-400 uppercase tracking-[0.25em]">Authorized Roles</th>
                <th className="px-10 py-6 text-right text-[10px] font-black text-brand-400 uppercase tracking-[0.25em]">Management</th>
              </tr>
            </thead>
            <motion.tbody
              initial="hidden"
              animate="visible"
              variants={listVariants}
              className="bg-white divide-y divide-brand-100"
            >
              <AnimatePresence mode="popLayout">
                {loading && users.length === 0 ? (
                  [...Array(5)].map((_, i) => (
                    <tr key={`skeleton-${i}`}>
                      <td className="px-10 py-8"><Skeleton variant="text" width="60%" height={20} /></td>
                      <td className="px-10 py-8"><Skeleton variant="rectangular" width={100} height={28} className="rounded-xl" /></td>
                      <td className="px-10 py-8 text-right"><Skeleton variant="circular" width={40} height={40} className="ml-auto" /></td>
                    </tr>
                  ))
                ) : users.length === 0 ? (
                    <motion.tr
                        key="empty"
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        exit={{ opacity: 0 }}
                    >
                        <td colSpan={3} className="px-10 py-40 text-center">
                            <div className="flex flex-col items-center gap-8">
                                <div className="p-10 bg-brand-50 rounded-4xl text-brand-100 border border-brand-100 shadow-inner relative">
                                    <Search className="h-20 w-20" />
                                    <div className="absolute -bottom-2 -right-2 p-3 bg-white rounded-2xl shadow-premium text-brand-300 border border-brand-100">
                                        <X size={24} />
                                    </div>
                                </div>
                                <div className="flex flex-col gap-2">
                                    <span className="text-brand-900 font-black text-2xl tracking-tight uppercase tracking-widest">
                                        {fetchError ? 'Sync Failed' : (searchQuery ? 'No results found' : 'No records exist')}
                                    </span>
                                    <p className="text-brand-300 font-bold max-w-sm mx-auto">
                                        We couldn't find any user matching your criteria. Try adjusting your search or filters.
                                    </p>
                                </div>
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
      <div className="flex items-center justify-between px-8 py-4 bg-white/50 rounded-3xl border border-brand-100/50">
        <div className="text-[11px] font-black text-brand-400 uppercase tracking-[0.3em]">
          Record Group <span className="text-brand-900">{page}</span> <span className="mx-4 text-brand-200">/</span> <span className="text-brand-900">{totalPages}</span>
        </div>
        <div className="flex gap-4">
          <Button
            variant="outline"
            size="sm"
            onClick={prevPage}
            disabled={page === 1 || loading}
            leftIcon={<ChevronLeft size={18} />}
            className="font-black bg-white"
          >
            Prev
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={nextPage}
            disabled={page === totalPages || loading}
            rightIcon={<ChevronRight size={18} />}
            className="font-black bg-white"
          >
            Next
          </Button>
        </div>
      </div>

      <ConfirmationDialog
        isOpen={deleteConfirmation.isOpen}
        onClose={() => setDeleteConfirmation({ isOpen: false, user: null })}
        onConfirm={confirmDelete}
        title="Revoke Administrative Access"
        message={`This action will immediately terminate all active sessions and permanent access for ${deleteConfirmation.user?.email}. This cannot be undone.`}
        confirmLabel="Deauthorize & Delete"
        isDestructive
      />
    </div>
  );
};

export default Users;
