import { motion } from 'framer-motion';
import { Trash2, ShieldCheck, Shield, ChevronRight } from 'lucide-react';
import type { User } from '../types';

interface UserRowProps {
  user: User;
  currentUserId: string | null;
  loading: boolean;
  onDeleteClick: (user: User) => void;
}

const rowVariants = {
  hidden: { opacity: 0, x: -10 },
  visible: {
    opacity: 1,
    x: 0,
    transition: { duration: 0.4, ease: [0.22, 1, 0.36, 1] as const }
  }
} as const;

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
      exit={{ opacity: 0, scale: 0.98, x: 20 }}
      layout
      className="hover:bg-brand-50/30 transition-all duration-300 group cursor-default relative overflow-hidden"
    >
      <td className="px-10 py-6 whitespace-nowrap">
        <div className="flex items-center gap-6">
            <div className={`w-12 h-12 rounded-2xl flex items-center justify-center font-black text-lg shadow-sm border transition-all duration-500 group-hover:scale-110 group-hover:rotate-6 ${isSelf ? 'bg-linear-to-br from-primary-500 to-primary-700 text-white border-primary-400 shadow-primary-200/50' : 'bg-white text-brand-600 border-brand-100 group-hover:border-primary-200 group-hover:text-primary-600'}`}>
                {user.email.charAt(0).toUpperCase()}
            </div>
            <div className="flex flex-col">
                <span className={`text-sm font-black transition-colors duration-300 ${isSelf ? 'text-primary-700' : 'text-brand-900 group-hover:text-primary-700'}`}>
                    {user.email}
                </span>
                {isSelf && (
                    <div className="flex items-center gap-1.5 mt-1">
                        <div className="w-1.5 h-1.5 rounded-full bg-primary-500 animate-pulse" />
                        <span className="text-[10px] font-black text-primary-500 uppercase tracking-widest">Active Operator</span>
                    </div>
                )}
            </div>
        </div>
      </td>
      <td className="px-10 py-6 whitespace-nowrap">
        <div className="flex flex-wrap gap-3">
          {user.roles.map(role => (
            <span key={role} className={`px-4 py-1.5 inline-flex items-center gap-2 text-[10px] font-black uppercase tracking-widest rounded-xl border transition-all duration-300 ${role === 'Admin' ? 'bg-primary-50 text-primary-700 border-primary-100' : 'bg-white text-brand-500 border-brand-100'} group-hover:shadow-sm`}>
              {role === 'Admin' ? <ShieldCheck size={14} className="text-primary-500" /> : <Shield size={14} />}
              {role}
            </span>
          ))}
        </div>
      </td>
      <td className="px-10 py-6 whitespace-nowrap text-right">
        <div className="flex items-center justify-end gap-4">
            {!isSelf && (
                <div className="opacity-0 group-hover:opacity-100 transition-all duration-500 translate-x-4 group-hover:translate-x-0">
                    <ChevronRight size={18} className="text-brand-200" />
                </div>
            )}
            <motion.button
              whileHover={isSelf || loading ? {} : { scale: 1.1, rotate: 8 }}
              whileTap={isSelf || loading ? {} : { scale: 0.9, rotate: -8 }}
              onClick={() => onDeleteClick(user)}
              disabled={isSelf || loading}
              className={`p-3 rounded-2xl transition-all duration-300 ${
                isSelf || loading
                  ? 'text-brand-100 bg-transparent'
                  : 'text-brand-300 hover:text-error-600 hover:bg-error-50 hover:shadow-premium shadow-error-100/50 border border-transparent hover:border-error-100 cursor-pointer'
              }`}
              title={isSelf ? 'Identity Protection Enabled' : loading ? 'Processing...' : 'Revoke Session Access'}
            >
              <Trash2 className="h-5.5 w-5.5" />
            </motion.button>
        </div>
      </td>
    </motion.tr>
  );
};

export default UserRow;
