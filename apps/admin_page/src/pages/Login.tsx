import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/api';
import { motion } from 'framer-motion';
import { Lock, Mail, Loader2, ArrowRight, ShieldCheck } from 'lucide-react';
import Button from '../components/Button';

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      duration: 0.8,
      staggerChildren: 0.15,
      ease: [0.22, 1, 0.36, 1]
    }
  } as const
};

const itemVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: {
    opacity: 1,
    y: 0,
    transition: {
      duration: 0.6,
      ease: [0.22, 1, 0.36, 1]
    }
  }
};

const Login = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const data = await authService.login(email, password);

      if (!data.roles.includes('Admin')) {
        setError('Access denied. Admin role required.');
        setLoading(false);
        return;
      }

      localStorage.setItem('admin_token', data.token);
      localStorage.setItem('admin_refresh_token', data.refreshToken);
      localStorage.setItem('admin_email', data.email);
      localStorage.setItem('admin_userId', data.userId);
      navigate('/');
    } catch (err) {
      setError(((err as { response?: { data?: { error?: string } } }).response?.data?.error) || 'Login failed. Please check your credentials.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen w-full flex items-center justify-center p-6 relative overflow-hidden bg-brand-50">
      {/* Dynamic Background */}
      <div className="absolute inset-0 z-0">
          <div className="absolute top-[-10%] left-[-10%] w-[40%] h-[40%] rounded-full bg-primary-100/30 blur-[120px]" />
          <div className="absolute bottom-[-10%] right-[-10%] w-[40%] h-[40%] rounded-full bg-primary-200/20 blur-[120px]" />
          <div className="absolute top-[20%] right-[10%] w-[30%] h-[30%] rounded-full bg-info-100/20 blur-[100px]" />
      </div>

      <motion.div
        variants={containerVariants}
        initial="hidden"
        animate="visible"
        className="w-full max-w-md z-10"
      >
        <div className="bg-white/90 backdrop-blur-2xl rounded-[2.5rem] shadow-premium-xl p-10 md:p-12 border border-white/50 relative overflow-hidden">
          {/* Subtle top glow */}
          <div className="absolute top-0 left-0 w-full h-1.5 bg-linear-to-r from-primary-400 via-primary-600 to-primary-400" />

          <motion.div variants={itemVariants} className="flex flex-col items-center mb-10 text-center">
            <motion.div
              whileHover={{ scale: 1.05, rotate: -3 }}
              whileTap={{ scale: 0.95 }}
              className="w-20 h-20 bg-primary-600 rounded-[2rem] flex items-center justify-center shadow-premium-lg shadow-primary-200/40 mb-8 cursor-default"
            >
              <span className="text-white font-black text-4xl">V</span>
            </motion.div>
            <h2 className="text-4xl font-black text-brand-900 tracking-tight mb-2">Valora Admin</h2>
            <p className="text-brand-500 font-bold tracking-tight">Enterprise Console Access</p>
          </motion.div>

          {error && (
            <motion.div
              variants={itemVariants}
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              className="p-4 mb-8 text-sm font-bold text-error-700 bg-error-50 border border-error-100 rounded-2xl flex items-center gap-3 shadow-sm"
            >
              <div className="w-2 h-2 rounded-full bg-error-500 animate-pulse" />
              {error}
            </motion.div>
          )}

          <form onSubmit={handleSubmit} className="space-y-7">
            <motion.div variants={itemVariants}>
              <label htmlFor="email" className="block text-[10px] font-black text-brand-400 mb-2.5 ml-1 uppercase tracking-[0.25em]">Email Address</label>
              <div className="relative group">
                <div className="absolute inset-y-0 left-0 pl-5 flex items-center pointer-events-none text-brand-300 group-focus-within:text-primary-500 transition-colors">
                  <Mail className="h-5 w-5" />
                </div>
                <input
                  id="email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="admin@valora.com"
                  className="w-full pl-12 pr-4 py-4 bg-brand-50/50 border border-brand-100 rounded-[1.25rem] focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white transition-all placeholder:text-brand-300 font-bold text-brand-900 outline-none"
                  required
                />
              </div>
            </motion.div>

            <motion.div variants={itemVariants}>
              <label htmlFor="password" className="block text-[10px] font-black text-brand-400 mb-2.5 ml-1 uppercase tracking-[0.25em]">Secure Password</label>
              <div className="relative group">
                <div className="absolute inset-y-0 left-0 pl-5 flex items-center pointer-events-none text-brand-300 group-focus-within:text-primary-500 transition-colors">
                  <Lock className="h-5 w-5" />
                </div>
                <input
                  id="password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••••••"
                  className="w-full pl-12 pr-4 py-4 bg-brand-50/50 border border-brand-100 rounded-[1.25rem] focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white transition-all placeholder:text-brand-300 font-bold text-brand-900 outline-none"
                  required
                />
              </div>
            </motion.div>

            <motion.div variants={itemVariants} className="pt-2">
                <Button
                  type="submit"
                  isLoading={loading}
                  className="w-full py-4.5 rounded-[1.25rem] group"
                  rightIcon={<ArrowRight size={18} className="group-hover:translate-x-1 transition-transform" />}
                >
                  Authorize Session
                </Button>
            </motion.div>
          </form>
        </div>
        <motion.div
            variants={itemVariants}
            className="flex items-center justify-center gap-2 mt-10 text-brand-400 text-sm font-bold"
        >
          <ShieldCheck size={16} className="text-success-500" />
          <p>Protected by biometric-grade encryption.</p>
        </motion.div>
      </motion.div>
    </div>
  );
};

export default Login;
