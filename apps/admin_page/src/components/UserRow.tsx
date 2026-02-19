import { motion } from 'framer-motion';
import { Trash2, ShieldCheck, Shield } from 'lucide-react';
import type { User } from '../types';

interface UserRowProps {
  user: User;
  currentUserId: string | null;
  loading: boolean;
  onDeleteClick: (user: User) => void;
}

const rowVariants = {
  hidden: { opacity: 0, y: 10 },
  visible: { opacity: 1, y: 0 }
};

const UserRow = ({
  user,
  currentUserId,
  loading,
  onDeleteClick
}: UserRowProps) => {
  const isSelf = user.id === currentUserId;

  return (
    <motion.tr
      variants={rowVariants}
      exit={{ opacity: 0, scale: 0.98 }}
      layout
      className="hover:bg-brand-50/50 transition-all duration-200 group cursor-default hover:scale-[1.005] hover:shadow-sm"
    >
      <td className="px-8 py-5 whitespace-nowrap">
        <div className="flex items-center gap-3">
            <div className={`w-8 h-8 rounded-full flex items-center justify-center font-bold text-xs ${isSelf ? 'bg-primary-100 text-primary-700' : 'bg-brand-100 text-brand-600'}`}>
                {user.email.charAt(0).toUpperCase()}
            </div>
            <span className={`text-sm font-bold ${isSelf ? 'text-primary-700' : 'text-brand-900'}`}>
                {user.email}
                {isSelf && <span className="ml-2 text-[10px] bg-primary-50 px-2 py-0.5 rounded-md">You</span>}
            </span>
        </div>
      </td>
      <td className="px-8 py-5 whitespace-nowrap">
        <div className="flex flex-wrap gap-2">
          {user.roles.map(role => (
            <span key={role} className="px-3 py-1 inline-flex items-center gap-1.5 text-[10px] leading-4 font-black uppercase tracking-widest rounded-lg bg-primary-50 text-primary-700 border border-primary-100/50 shadow-sm">
              {role === 'Admin' ? <ShieldCheck size={12} /> : <Shield size={12} />}
              {role}
            </span>
          ))}
        </div>
      </td>
      <td className="px-8 py-5 whitespace-nowrap text-right">
        <motion.button
          whileHover={isSelf || loading ? {} : { scale: 1.1 }}
          whileTap={isSelf || loading ? {} : { scale: 0.9 }}
          onClick={() => onDeleteClick(user)}
          disabled={isSelf || loading}
          className={`p-2.5 rounded-xl transition-all ${
            isSelf || loading
              ? 'text-brand-200 cursor-not-allowed opacity-50'
              : 'text-brand-400 hover:text-error-600 hover:bg-error-50 hover:shadow-sm cursor-pointer'
          }`}
          title={isSelf ? 'You cannot delete yourself' : loading ? 'Please wait...' : 'Delete user'}
        >
          <Trash2 className="h-5 w-5" />
        </motion.button>
      </td>
    </motion.tr>
  );
};

export default UserRow;
