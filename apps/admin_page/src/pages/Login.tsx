import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/api';
import { motion } from 'framer-motion';
import { Lock, Mail, Loader2 } from 'lucide-react';

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

  const containerVariants = {
    hidden: { opacity: 0, y: 20 },
    visible: {
      opacity: 1,
      y: 0,
      transition: {
        duration: 0.6,
        staggerChildren: 0.1,
        ease: [0.22, 1, 0.36, 1]
      }
    } as const
  };

  const itemVariants = {
    hidden: { opacity: 0, y: 10 },
    visible: { opacity: 1, y: 0 }
  };

  return (
    <div className="login-page min-h-screen w-full flex items-center justify-center p-4">
      <motion.div
        variants={containerVariants}
        initial="hidden"
        animate="visible"
        className="w-full max-w-md"
      >
        <div className="bg-white rounded-3xl shadow-premium-xl p-10 border border-brand-100 relative overflow-hidden">
          <div className="absolute top-0 left-0 w-full h-1 bg-gradient-to-r from-primary-400 via-primary-600 to-primary-400" />

          <motion.div variants={itemVariants} className="flex flex-col items-center mb-10">
            <motion.div
              whileHover={{ scale: 1.05, rotate: -2 }}
              whileTap={{ scale: 0.95 }}
              className="w-16 h-16 bg-primary-600 rounded-2xl flex items-center justify-center shadow-premium-lg shadow-primary-200/20 mb-6 cursor-default"
            >
              <span className="text-white font-bold text-3xl">V</span>
            </motion.div>
            <h2 className="text-3xl font-black text-brand-900 tracking-tight">Valora Admin</h2>
            <p className="text-brand-500 mt-2 font-medium text-center">Please sign in to your account</p>
          </motion.div>

          {error && (
            <motion.div
              variants={itemVariants}
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0 }}
              className="p-4 mb-8 text-sm font-bold text-error-700 bg-error-50 rounded-2xl border border-error-100/50 flex items-center gap-3"
            >
              <div className="w-1.5 h-1.5 rounded-full bg-error-500" />
              {error}
            </motion.div>
          )}

          <form onSubmit={handleSubmit} className="space-y-6">
            <motion.div variants={itemVariants}>
              <label htmlFor="email" className="block text-[10px] font-black text-brand-400 mb-2 ml-1 uppercase tracking-[0.2em]">Email Address</label>
              <div className="relative group">
                <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none text-brand-300 group-focus-within:text-primary-500 transition-colors">
                  <Mail className="h-5 w-5" />
                </div>
                <input
                  id="email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="name@company.com"
                  className="w-full pl-11 pr-4 py-4 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white transition-all placeholder:text-brand-300 font-semibold text-brand-900 outline-none"
                  required
                />
              </div>
            </motion.div>

            <motion.div variants={itemVariants}>
              <label htmlFor="password" className="block text-[10px] font-black text-brand-400 mb-2 ml-1 uppercase tracking-[0.2em]">Password</label>
              <div className="relative group">
                <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none text-brand-300 group-focus-within:text-primary-500 transition-colors">
                  <Lock className="h-5 w-5" />
                </div>
                <input
                  id="password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="w-full pl-11 pr-4 py-4 bg-brand-50/50 border border-brand-100 rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white transition-all placeholder:text-brand-300 font-semibold text-brand-900 outline-none"
                  required
                />
              </div>
            </motion.div>

            <motion.button
              variants={itemVariants}
              whileHover={{ scale: 1.01, translateY: -1 }}
              whileTap={{ scale: 0.98 }}
              type="submit"
              disabled={loading}
              className="w-full py-4 px-6 rounded-2xl shadow-premium-lg shadow-primary-200/30 text-sm font-bold text-white bg-primary-600 hover:bg-primary-700 transition-all disabled:opacity-50 disabled:active:scale-100 cursor-pointer flex items-center justify-center group"
            >
              {loading ? (
                <Loader2 className="h-5 w-5 animate-spin" />
              ) : (
                <>
                  Sign In
                  <motion.span
                    className="ml-2"
                    initial={{ x: 0 }}
                    whileHover={{ x: 3 }}
                  >
                    →
                  </motion.span>
                </>
              )}
            </motion.button>
          </form>
        </div>
        <p className="text-center text-brand-400 mt-8 text-sm font-medium">
          Protected by enterprise-grade security.
        </p>
      </motion.div>
    </div>
  );
};

export default Login;
