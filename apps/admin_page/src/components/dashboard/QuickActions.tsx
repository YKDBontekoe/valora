import { useState } from 'react';
import { RefreshCw, PlayCircle, Users, Activity, ChevronRight, Sparkles } from 'lucide-react';
import { adminService } from '../../services/api';
import { showToast } from '../../services/toast';
import { useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';

interface QuickActionsProps {
  onRefreshStats: () => void;
}

const QuickActions = ({ onRefreshStats }: QuickActionsProps) => {
  const [retrying, setRetrying] = useState(false);
  const navigate = useNavigate();

  const handleRetryFailedJobs = async () => {
    setRetrying(true);
    try {
      const response = await adminService.getJobs(1, 100, 'Failed');
      const failedJobs = response.items;

      if (failedJobs.length === 0) {
        showToast('No failed jobs found in the cluster.', 'info');
        setRetrying(false);
        return;
      }

      let successCount = 0;
      for (const job of failedJobs) {
        try {
          await adminService.retryJob(job.id);
          successCount++;
        } catch (e) {
          console.error(`Failed to retry job ${job.id}`, e);
        }
      }

      showToast(`Successfully re-queued ${successCount} failed ${successCount === 1 ? 'job' : 'jobs'}.`, 'success');
      onRefreshStats();
    } catch (error) {
        console.error(error);
        showToast('System failed to re-queue jobs.', 'error');
    } finally {
      setRetrying(false);
    }
  };

  const actionCards = [
      {
          title: 'Sync Cluster',
          description: 'Force a full refresh of system metrics.',
          icon: RefreshCw,
          onClick: onRefreshStats,
          color: 'text-primary-600',
          bg: 'bg-primary-50',
          accent: 'bg-primary-500'
      },
      {
          title: 'Retry Pipeline',
          description: 'Re-queue all currently failed batch jobs.',
          icon: PlayCircle,
          onClick: handleRetryFailedJobs,
          isLoading: retrying,
          color: 'text-warning-600',
          bg: 'bg-warning-50',
          accent: 'bg-warning-500'
      },
      {
          title: 'User Control',
          description: 'Navigate to enterprise user management.',
          icon: Users,
          onClick: () => navigate('/users'),
          color: 'text-info-600',
          bg: 'bg-info-50',
          accent: 'bg-info-500'
      }
  ];

  return (
    <div className="h-full flex flex-col gap-10">
        <div className="flex items-center gap-5">
            <div className="w-14 h-14 bg-primary-50 rounded-2xl flex items-center justify-center border border-primary-100 shadow-sm transition-all duration-500 hover:rotate-6 hover:scale-110">
                <Activity className="text-primary-600" size={28} />
            </div>
            <div className="flex flex-col">
                <h2 className="text-3xl font-black text-brand-900 tracking-tight">Control Center</h2>
                <div className="flex items-center gap-2 mt-1 opacity-50">
                    <Sparkles size={12} className="text-brand-300" />
                    <span className="text-[10px] font-black uppercase tracking-ultra-wide text-brand-400">Executive Shortcuts</span>
                </div>
            </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 xl:grid-cols-1 gap-8">
            {actionCards.map((action) => (
                <motion.button
                    key={action.title}
                    whileHover={{
                      x: 12,
                      scale: 1.02,
                      transition: { type: 'spring', stiffness: 260, damping: 20 }
                    }}
                    whileTap={{ scale: 0.98 }}
                    onClick={action.onClick}
                    disabled={action.isLoading}
                    className="flex items-center gap-8 p-10 bg-white/70 backdrop-blur-xl rounded-[2.5rem] border border-brand-100 shadow-premium hover:shadow-premium-xl transition-all duration-500 text-left group cursor-pointer relative overflow-hidden hover-border-gradient"
                >
                    {/* Active highlight line */}
                    <div className={`absolute left-0 top-1/2 -translate-y-1/2 w-2 h-2/3 ${action.accent} rounded-r-full opacity-0 group-hover:opacity-100 transition-all duration-700 scale-y-0 group-hover:scale-y-100 shadow-glow shadow-current`} style={{ color: `var(--color-${action.accent.split('-')[1]}-500)` }} />

                    <div className={`relative z-10 w-20 h-20 ${action.bg} rounded-[1.5rem] flex items-center justify-center transition-all duration-700 group-hover:scale-110 group-hover:rotate-6 group-hover:shadow-premium group-hover:shadow-brand-100/50 border border-brand-100/30`}>
                        <action.icon className={`w-10 h-10 ${action.color} ${action.isLoading ? 'animate-spin' : ''}`} />
                    </div>
                    <div className="relative z-10 flex-1">
                        <h3 className="text-2xl font-black text-brand-900 leading-tight tracking-tight">{action.title}</h3>
                        <p className="text-[11px] font-bold text-brand-400 mt-2 uppercase tracking-ultra-wide leading-relaxed max-w-[200px]">{action.description}</p>
                    </div>
                    <div className={`relative z-10 opacity-0 group-hover:opacity-100 transition-all duration-700 translate-x-8 group-hover:translate-x-0 p-3 rounded-2xl ${action.bg}`}>
                        <ChevronRight className={`w-6 h-6 ${action.color}`} />
                    </div>
                </motion.button>
            ))}
        </div>
    </div>
  );
};

export default QuickActions;
