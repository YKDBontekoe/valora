import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { X, UserPlus, Lock, Mail, Shield } from 'lucide-react';
import { AxiosError } from 'axios';
import Button from './Button';
import { adminService } from '../services/api';
import { showToast } from '../services/toast';

interface CreateUserModalProps {
  isOpen: boolean;
  onClose: () => void;
  onUserCreated: () => void;
}

const CreateUserModal = ({ isOpen, onClose, onUserCreated }: CreateUserModalProps) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState('User');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      await adminService.createUser({ email, password, roles: [role] });
      showToast('User created successfully.', 'success');
      onUserCreated();
      onClose();
      // State reset handled by next mount or not needed as we unmount
    } catch (err) {
       const axiosError = err as AxiosError<{ error?: string; title?: string; errors?: Record<string, string[]> }>;
       let msg = axiosError.response?.data?.error;

       if (!msg && axiosError.response?.data?.errors) {
         // Handle ValidationProblem format
         const errors = axiosError.response.data.errors;
         msg = Object.values(errors).flat().join(' ');
       } else if (!msg && axiosError.response?.data?.title) {
         msg = axiosError.response.data.title;
       }

       setError(msg || 'Failed to create user.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AnimatePresence>
      {isOpen && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4" role="dialog" aria-modal="true" aria-labelledby="create-user-title">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            onClick={onClose}
            className="absolute inset-0 bg-brand-900/40 backdrop-blur-sm"
          />
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 20 }}
            transition={{ type: "spring", damping: 25, stiffness: 300 }}
            className="relative w-full max-w-md bg-white rounded-[2rem] shadow-premium-xl overflow-hidden border border-brand-100"
          >
            <div className="p-8">
              <div className="flex items-center justify-between mb-6">
                <div className="p-3 rounded-2xl bg-primary-50 text-primary-600">
                  <UserPlus size={24} />
                </div>
                <button
                  onClick={onClose}
                  className="p-2 text-brand-400 hover:text-brand-900 transition-colors rounded-xl hover:bg-brand-50"
                  aria-label="Close modal"
                >
                  <X size={20} />
                </button>
              </div>

              <h3 id="create-user-title" className="text-2xl font-black text-brand-900 tracking-tight mb-2">
                Create New User
              </h3>
              <p className="text-brand-500 font-medium leading-relaxed mb-6">
                Add a new administrator or user to the platform.
              </p>

              {error && (
                <div className="mb-6 p-4 bg-error-50 border border-error-100 rounded-xl text-error-700 text-sm font-bold flex items-center gap-2">
                    <div className="w-1.5 h-1.5 rounded-full bg-error-500" />
                    {error}
                </div>
              )}

              <form onSubmit={handleSubmit} className="space-y-4">
                <div className="space-y-1">
                    <label htmlFor="email-input" className="text-xs font-black text-brand-400 uppercase tracking-wider ml-1">Email Address</label>
                    <div className="relative group">
                        <Mail className="absolute left-4 top-1/2 -translate-y-1/2 text-brand-400 group-focus-within:text-primary-500 transition-colors" size={18} />
                        <input
                            id="email-input"
                            type="email"
                            required
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            className="w-full pl-11 pr-4 py-3 bg-brand-50/50 border border-brand-100 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all font-semibold text-brand-900 placeholder:text-brand-300"
                            placeholder="admin@valora.com"
                        />
                    </div>
                </div>

                <div className="space-y-1">
                    <label htmlFor="password-input" className="text-xs font-black text-brand-400 uppercase tracking-wider ml-1">Password</label>
                    <div className="relative group">
                        <Lock className="absolute left-4 top-1/2 -translate-y-1/2 text-brand-400 group-focus-within:text-primary-500 transition-colors" size={18} />
                        <input
                            id="password-input"
                            type="password"
                            required
                            minLength={8}
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            className="w-full pl-11 pr-4 py-3 bg-brand-50/50 border border-brand-100 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all font-semibold text-brand-900 placeholder:text-brand-300"
                            placeholder="••••••••"
                        />
                    </div>
                </div>

                <div className="space-y-1">
                    <label htmlFor="role-select" className="text-xs font-black text-brand-400 uppercase tracking-wider ml-1">Role</label>
                    <div className="relative group">
                        <Shield className="absolute left-4 top-1/2 -translate-y-1/2 text-brand-400 group-focus-within:text-primary-500 transition-colors" size={18} />
                        <select
                            id="role-select"
                            value={role}
                            onChange={(e) => setRole(e.target.value)}
                            className="w-full pl-11 pr-4 py-3 bg-brand-50/50 border border-brand-100 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all font-semibold text-brand-900 appearance-none cursor-pointer"
                        >
                            <option value="Admin">Admin</option>
                            <option value="User">User</option>
                        </select>
                    </div>
                </div>

                <div className="pt-4 flex gap-3">
                    <Button type="button" variant="outline" onClick={onClose} className="flex-1">
                        Cancel
                    </Button>
                    <Button type="submit" variant="primary" isLoading={isLoading} className="flex-1">
                        Create User
                    </Button>
                </div>
              </form>
            </div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );
};

export default CreateUserModal;
