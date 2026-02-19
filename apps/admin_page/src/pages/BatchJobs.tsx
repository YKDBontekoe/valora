import { useState, useEffect } from 'react';
import { adminService } from '../services/api';
import type { BatchJob } from '../types';
import { Play, RotateCcw, CheckCircle2, XCircle, Loader2 } from 'lucide-react';
import { motion } from 'framer-motion';

const BatchJobs = () => {
  const [jobs, setJobs] = useState<BatchJob[]>([]);
  const [loading, setLoading] = useState(true);
  const [targetCity, setTargetCity] = useState('');
  const [isStarting, setIsStarting] = useState(false);

  const fetchJobs = async () => {
    try {
      const data = await adminService.getJobs();
      setJobs(data);
    } catch {
      console.error('Failed to fetch jobs');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchJobs();
    const interval = setInterval(fetchJobs, 5000);
    return () => clearInterval(interval);
  }, []);

  const handleStartJob = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!targetCity) return;

    if (targetCity.length < 3 || targetCity.length > 100) {
      alert('City name must be between 3 and 100 characters.');
      return;
    }

    setIsStarting(true);
    try {
      await adminService.startJob('CityIngestion', targetCity);
      setTargetCity('');
      fetchJobs();
    } catch {
      console.error('Failed to start job');
    } finally {
      setIsStarting(false);
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'Completed': return <CheckCircle2 className="h-5 w-5 text-success-500" />;
      case 'Failed': return <XCircle className="h-5 w-5 text-error-500" />;
      case 'Processing': return <Loader2 className="h-5 w-5 text-primary-500 animate-spin" />;
      default: return <RotateCcw className="h-5 w-5 text-brand-400" />;
    }
  };

  const getStatusBadge = (status: string) => {
    const base = "px-2.5 py-0.5 rounded-full text-xs font-medium";
    switch (status) {
      case 'Completed': return `${base} bg-success-50 text-success-700 border border-success-200`;
      case 'Failed': return `${base} bg-error-50 text-error-700 border border-error-200`;
      case 'Processing': return `${base} bg-primary-50 text-primary-700 border border-primary-200`;
      default: return `${base} bg-brand-50 text-brand-700 border border-brand-200`;
    }
  };

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold text-brand-900">Batch Jobs</h1>
        <p className="text-brand-500 mt-1">Manage and monitor data ingestion processes.</p>
      </div>

      <div className="bg-white p-6 rounded-2xl border border-brand-100 shadow-premium">
        <h2 className="text-lg font-bold text-brand-900 mb-4">Start New Job</h2>
        <form onSubmit={handleStartJob} className="flex gap-4">
          <div className="flex-1">
            <input
              type="text"
              placeholder="City Name (e.g. Amsterdam)"
              value={targetCity}
              onChange={(e) => setTargetCity(e.target.value)}
              className="w-full px-4 py-2 rounded-xl border border-brand-200 focus:ring-2 focus:ring-primary-500 focus:border-transparent outline-none transition-all"
              disabled={isStarting}
            />
          </div>
          <button
            type="submit"
            disabled={isStarting || !targetCity}
            className="flex items-center gap-2 bg-brand-900 text-white px-6 py-2 rounded-xl font-bold hover:bg-brand-800 disabled:opacity-50 disabled:cursor-not-allowed transition-all"
          >
            {isStarting ? <Loader2 className="h-5 w-5 animate-spin" /> : <Play className="h-5 w-5" />}
            Start City Ingestion
          </button>
        </form>
      </div>

      <div className="bg-white rounded-2xl border border-brand-100 shadow-premium overflow-hidden">
        <div className="px-6 py-4 border-b border-brand-100 bg-brand-50/50">
          <h2 className="font-bold text-brand-900">Recent Jobs</h2>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-brand-100">
            <thead>
              <tr className="bg-brand-50/30">
                <th className="px-6 py-3 text-left text-xs font-bold text-brand-400 uppercase tracking-widest">Job Type</th>
                <th className="px-6 py-3 text-left text-xs font-bold text-brand-400 uppercase tracking-widest">Target</th>
                <th className="px-6 py-3 text-left text-xs font-bold text-brand-400 uppercase tracking-widest">Status</th>
                <th className="px-6 py-3 text-left text-xs font-bold text-brand-400 uppercase tracking-widest">Progress</th>
                <th className="px-6 py-3 text-left text-xs font-bold text-brand-400 uppercase tracking-widest">Created At</th>
                <th className="px-6 py-3 text-left text-xs font-bold text-brand-400 uppercase tracking-widest">Details</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-brand-100">
              {loading && jobs.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-8 text-center">
                    <Loader2 className="h-8 w-8 animate-spin mx-auto text-primary-500" />
                  </td>
                </tr>
              ) : jobs.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-8 text-center text-brand-500">
                    No jobs found.
                  </td>
                </tr>
              ) : (
                jobs.map((job) => (
                  <motion.tr
                    key={job.id}
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    className="hover:bg-brand-50/30 transition-colors"
                  >
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-bold text-brand-900">{job.type}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-brand-600">{job.target}</td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center gap-2">
                        {getStatusIcon(job.status)}
                        <span className={getStatusBadge(job.status)}>{job.status}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="w-full bg-brand-100 rounded-full h-2 min-w-[100px]">
                        <motion.div
                          className="bg-primary-600 h-2 rounded-full"
                          initial={{ width: 0 }}
                          animate={{ width: `${job.progress}%` }}
                          transition={{ duration: 0.5 }}
                        />
                      </div>
                      <span className="text-[10px] text-brand-400 mt-1 block font-bold">{job.progress}%</span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-brand-500">
                      {new Date(job.createdAt).toLocaleString()}
                    </td>
                    <td className="px-6 py-4 text-sm text-brand-600 max-w-xs truncate">
                      {job.error || job.resultSummary || '-'}
                    </td>
                  </motion.tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default BatchJobs;
