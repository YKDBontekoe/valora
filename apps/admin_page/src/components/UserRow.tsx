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
  hidden: { opacity: 0, y: 15 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { duration: 0.4, ease: [0.22, 1, 0.36, 1] }
  }
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
      exit={{ opacity: 0, scale: 0.98, x: 20 }}
      layout
      className="hover:bg-brand-50/50 transition-all duration-300 group cursor-default relative overflow-hidden"
    >
      <td className="px-10 py-6 whitespace-nowrap">
        <div className="flex items-center gap-4">
            <div className={`w-10 h-10 rounded-2xl flex items-center justify-center font-black text-sm shadow-sm border transition-transform duration-500 group-hover:scale-110 group-hover:rotate-3 ${isSelf ? 'bg-primary-600 text-white border-primary-500 shadow-primary-200/50' : 'bg-white text-brand-600 border-brand-100 group-hover:border-primary-200'}`}>
                {user.email.charAt(0).toUpperCase()}
            </div>
            <div className="flex flex-col">
                <span className={`text-sm font-black transition-colors duration-300 ${isSelf ? 'text-primary-700' : 'text-brand-900 group-hover:text-primary-700'}`}>
                    {user.email}
                </span>
                {isSelf && <span className="text-[10px] font-black text-primary-500 uppercase tracking-widest mt-0.5">Primary Session</span>}
            </div>
        </div>
      </td>
      <td className="px-10 py-6 whitespace-nowrap">
        <div className="flex flex-wrap gap-2.5">
          {user.roles.map(role => (
            <span key={role} className="px-3.5 py-1.5 inline-flex items-center gap-2 text-[10px] font-black uppercase tracking-widest rounded-xl bg-white text-brand-600 border border-brand-100 shadow-sm group-hover:border-primary-100 group-hover:text-primary-700 transition-all duration-300">
              {role === 'Admin' ? <ShieldCheck size={14} className="text-primary-500" /> : <Shield size={14} />}
              {role}
            </span>
          ))}
        </div>
      </td>
      <td className="px-10 py-6 whitespace-nowrap text-right">
        <motion.button
          whileHover={isSelf || loading ? {} : { scale: 1.1, rotate: 5 }}
          whileTap={isSelf || loading ? {} : { scale: 0.9, rotate: -5 }}
          onClick={() => onDeleteClick(user)}
          disabled={isSelf || loading}
          className={`p-3 rounded-2xl transition-all duration-300 ${
            isSelf || loading
              ? 'text-brand-100 cursor-not-allowed'
              : 'text-brand-300 hover:text-error-600 hover:bg-error-50 hover:shadow-premium shadow-error-100/50 border border-transparent hover:border-error-100 cursor-pointer'
          }`}
          title={isSelf ? 'Access Protection Enabled' : loading ? 'Processing...' : 'Revoke Access'}
        >
          <Trash2 className="h-5.5 w-5.5" />
        </motion.button>
      </td>
    </motion.tr>
  );
};

export default UserRow;
