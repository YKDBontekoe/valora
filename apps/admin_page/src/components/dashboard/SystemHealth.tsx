import { motion } from 'framer-motion';
import { TrendingUp } from 'lucide-react';

const item = {
  hidden: { opacity: 0, y: 20 },
  show: { opacity: 1, y: 0 }
};

const SystemHealth = () => {
  return (
    <motion.div
      variants={item}
      initial="hidden"
      animate="show"
      transition={{ delay: 0.4 }}
      className="mt-12 p-8 bg-white rounded-3xl border border-brand-100 shadow-premium relative overflow-hidden"
    >
      <div className="absolute top-0 right-0 p-8 opacity-5">
          <TrendingUp size={160} />
      </div>
      <h2 className="text-xl font-black text-brand-900 mb-6">System Health</h2>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          <div className="space-y-2">
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">Database Status</span>
              <div className="flex items-center gap-2">
                  <div className="w-2 h-2 rounded-full bg-success-500 animate-pulse" />
                  <span className="font-bold text-brand-700">Healthy & Connected</span>
              </div>
          </div>
          <div className="space-y-2">
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">API Latency</span>
              <div className="flex items-center gap-2">
                  <span className="font-bold text-brand-700">42ms</span>
                  <span className="text-xs text-success-600 font-bold bg-success-50 px-2 py-0.5 rounded-md">Optimal</span>
              </div>
          </div>
          <div className="space-y-2">
              <span className="text-[10px] font-black text-brand-400 uppercase tracking-widest">Active Jobs</span>
              <div className="flex items-center gap-2">
                  <span className="font-bold text-brand-700">0</span>
                  <span className="text-xs text-brand-400 font-bold italic">No background tasks</span>
              </div>
          </div>
      </div>
    </motion.div>
  );
};

export default SystemHealth;
