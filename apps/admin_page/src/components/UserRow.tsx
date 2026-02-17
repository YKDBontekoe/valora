import { motion } from 'framer-motion';
import { Trash2 } from 'lucide-react';
import type { User } from '../types';

interface UserRowProps {
  user: User;
  currentUserId: string | null;
  loading: boolean;
  onDeleteClick: (user: User) => void;
}

const UserRow = ({
  user,
  currentUserId,
  loading,
  onDeleteClick
}: UserRowProps) => (
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
          <span key={role} className="px-3 py-1 inline-flex text-[10px] leading-4 font-black uppercase tracking-widest rounded-lg bg-primary-50 text-primary-700 border border-primary-100/50">
            {role}
          </span>
        ))}
      </div>
    </td>
    <td className="px-8 py-5 whitespace-nowrap text-right text-sm font-medium">
      <motion.button
        whileHover={user.id === currentUserId || loading ? {} : { scale: 1.1, rotate: 5 }}
        whileTap={user.id === currentUserId || loading ? {} : { scale: 0.9 }}
        onClick={() => onDeleteClick(user)}
        disabled={user.id === currentUserId || loading}
        className={`p-2.5 rounded-xl transition-all ${
          user.id === currentUserId || loading
            ? 'text-brand-200 cursor-not-allowed'
            : 'text-brand-400 hover:text-error-600 hover:bg-error-50 cursor-pointer'
        }`}
        title={user.id === currentUserId ? 'You cannot delete yourself' : loading ? 'Please wait...' : 'Delete user'}
      >
        <Trash2 className="h-5 w-5" />
      </motion.button>
    </td>
  </motion.tr>
);

export default UserRow;
