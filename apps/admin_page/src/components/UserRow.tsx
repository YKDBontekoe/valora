import { motion } from 'framer-motion';
import { Trash2, ShieldCheck, Shield, ChevronRight, User as UserIcon } from 'lucide-react';
import { forwardRef } from 'react';
import type { User } from '../types';

interface UserRowProps {
  user: User;
  currentUserId: string | null;
  loading: boolean;
  onDeleteClick: (user: User) => void;
}

const rowVariants = {
  hidden: { opacity: 0, x: -15, scale: 0.98 },
  visible: {
    opacity: 1,
    x: 0,
    scale: 1,
    transition: { duration: 0.5, ease: [0.22, 1, 0.36, 1] as const }
  }
} as const;

const UserRow = forwardRef<HTMLTableRowElement, UserRowProps>(({
  user,
  currentUserId,
  loading,
  onDeleteClick
}, ref) => {
  const isSelf = user.id === currentUserId;

  return (
    <motion.tr
      ref={ref}
      variants={rowVariants}
      exit={{ opacity: 0, scale: 0.95, x: 20, transition: { duration: 0.3 } }}
      whileHover={{ x: 10, backgroundColor: 'var(--color-brand-50)' }}
      layout
      className="group cursor-default relative overflow-hidden transition-colors duration-500"
    >
      <td className="px-12 py-8 whitespace-nowrap">
        <div className="flex items-center gap-8">
            <div className="relative group/avatar">
                <div className={`w-14 h-14 rounded-[1.25rem] flex items-center justify-center font-black text-xl shadow-premium border-2 transition-all duration-700 group-hover/avatar:rotate-12 group-hover/avatar:scale-110 ${isSelf ? 'bg-linear-to-br from-primary-500 to-primary-700 text-white border-primary-400 shadow-glow-primary' : 'bg-white text-brand-600 border-brand-100 group-hover:border-primary-200 group-hover:text-primary-600 group-hover:shadow-glow-primary shadow-sm'}`}>
                    {user.email.charAt(0).toUpperCase()}
                </div>
                {isSelf && (
                    <div className="absolute -top-2 -right-2 p-1.5 bg-success-500 rounded-lg shadow-glow-success border-2 border-white">
                        <ShieldCheck size={12} className="text-white" />
                    </div>
                )}
            </div>
            <div className="flex flex-col">
                <div className="flex items-center gap-3">
                    <span className={`text-base font-black transition-colors duration-300 ${isSelf ? 'text-primary-700' : 'text-brand-900 group-hover:text-primary-700'}`}>
                        {user.email}
                    </span>
                    {!isSelf && (
                        <div className="opacity-0 group-hover:opacity-100 transition-opacity duration-500">
                            <ChevronRight size={14} className="text-brand-200" />
                        </div>
                    )}
                </div>
                <div className="flex items-center gap-2 mt-1.5 opacity-60">
                    <UserIcon size={12} className="text-brand-300" />
                    <span className="text-[10px] font-black text-brand-400 uppercase tracking-ultra-wide">Operator Entity</span>
                    {isSelf && (
                         <span className="text-[10px] font-black text-primary-500 uppercase tracking-ultra-wide ml-2">• Current Active Identity</span>
                    )}
                </div>
            </div>
        </div>
      </td>
      <td className="px-12 py-8 whitespace-nowrap">
        <div className="flex flex-wrap gap-4">
          {user.roles.map(role => (
            <span key={role} className={`px-5 py-2 inline-flex items-center gap-2.5 text-[11px] font-black uppercase tracking-ultra-wide rounded-xl border transition-all duration-500 ${role === 'Admin' ? 'bg-primary-50 text-primary-700 border-primary-100 shadow-sm' : 'bg-white text-brand-400 border-brand-100'} group-hover:shadow-md group-hover:translate-y-[-2px] group-hover:bg-white`}>
              {role === 'Admin' ? <ShieldCheck size={16} className="text-primary-500" /> : <Shield size={16} />}
              {role}
            </span>
          ))}
        </div>
      </td>
      <td className="px-12 py-8 whitespace-nowrap text-right">
        <div className="flex items-center justify-end gap-6">
            <motion.button
              whileHover={isSelf || loading ? {} : { scale: 1.15, rotate: 5 }}
              whileTap={isSelf || loading ? {} : { scale: 0.9, rotate: -5 }}
              onClick={() => onDeleteClick(user)}
              disabled={isSelf || loading}
              className={`p-4 rounded-2xl transition-all duration-500 ${
                isSelf || loading
                  ? 'text-brand-100 bg-transparent'
                  : 'text-brand-300 hover:text-error-600 hover:bg-error-50 hover:shadow-glow-error border border-transparent hover:border-error-100 cursor-pointer shadow-sm hover:rotate-2'
              }`}
              title={isSelf ? 'Self-Protection Active' : loading ? 'Requesting...' : 'Deauthorize User'}
            >
              <Trash2 className="h-6 w-6" />
            </motion.button>
        </div>
      </td>
    </motion.tr>
  );
});

export default UserRow;
