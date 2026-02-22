import { useState } from 'react';
import { RefreshCw, PlayCircle, Filter } from 'lucide-react';
import Button from '../Button';
import { adminService } from '../../services/api';
import { showToast } from '../../services/toast';
import { useNavigate } from 'react-router-dom';

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
        showToast('No failed jobs found.', 'info');
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

      showToast(`Retried ${successCount} failed jobs.`, 'success');
    } catch (error) { console.error(error);
        showToast('Failed to retry jobs.', 'error');
    } finally {
      setRetrying(false);
    }
  };

  return (
    <div className="mt-8 p-6 bg-white rounded-3xl border border-brand-100 shadow-sm">
      <h2 className="text-lg font-black text-brand-900 mb-4">Quick Actions</h2>
      <div className="flex flex-wrap gap-4">
        <Button
            onClick={onRefreshStats}
            variant="outline"
            leftIcon={<RefreshCw className="w-4 h-4" />}
        >
          Refresh Status
        </Button>
        <Button
            onClick={handleRetryFailedJobs}
            isLoading={retrying}
            variant="outline"
            className="border-warning-200 text-warning-700 hover:bg-warning-50"
            leftIcon={<PlayCircle className="w-4 h-4" />}
        >
          Retry Failed Jobs
        </Button>
        <Button
            onClick={() => navigate('/users')}
            variant="outline"
            leftIcon={<Filter className="w-4 h-4" />}
        >
          Manage Users
        </Button>
      </div>
    </div>
  );
};

export default QuickActions;
