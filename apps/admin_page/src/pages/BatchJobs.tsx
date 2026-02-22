import React, { useState, useEffect, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Activity, Play, AlertCircle, Database, Sparkles, Info, ChevronLeft, ChevronRight, Globe, Layers } from 'lucide-react';
import { adminService } from '../services/api';
import type { BatchJob } from '../types';
import Button from '../components/Button';
import { showToast } from '../services/toast';
import Skeleton from '../components/Skeleton';
import JobDetailsModal from './JobDetailsModal';
import DatasetStatusModal from './DatasetStatusModal';

const BatchJobs: React.FC = () => {
  const [jobs, setJobs] = useState<BatchJob[]>([]);
  const [loading, setLoading] = useState(true);
  const [isStarting, setIsStarting] = useState(false);
  const [targetCity, setTargetCity] = useState('');
  const [error, setError] = useState<string | null>(null);

  // Pagination & Filtering
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [statusFilter, setStatusFilter] = useState('All');
  const [typeFilter, setTypeFilter] = useState('All');
  const pageSize = 10;

  // Modal State
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isDatasetModalOpen, setIsDatasetModalOpen] = useState(false);

  const fetchJobs = useCallback(async () => {
    try {
      const data = await adminService.getJobs(page, pageSize, statusFilter, typeFilter);
      setJobs(data.items);
      setTotalPages(data.totalPages);
      setError(null);
    } catch {
      setError('Unable to load job history.');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, statusFilter, typeFilter]);

  useEffect(() => {
    fetchJobs();
    const interval = setInterval(fetchJobs, 5000); // Live poll every 5s
    return () => clearInterval(interval);
  }, [fetchJobs]);

  const handleStartJob = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!targetCity) return;

    setIsStarting(true);
    try {
      await adminService.startJob('CityIngestion', targetCity);
      showToast('Pipeline initiated successfully', 'success');
      setTargetCity('');
      fetchJobs();
    } catch {
      // Toast handled by api interceptor
    } finally {
      setIsStarting(false);
    }
  };

  const handleIngestAll = async () => {
    if (!window.confirm('Are you sure you want to trigger ingestion for ALL municipalities? This will queue hundreds of jobs.')) return;

    setIsStarting(true);
    try {
      await adminService.startJob('AllCitiesIngestion', 'Netherlands');
      showToast('Full dataset ingestion pipeline initiated', 'success');
      fetchJobs();
    } catch {
       // handled
    } finally {
      setIsStarting(false);
    }
  };

  const openDetails = (jobId: string) => {
    setSelectedJobId(jobId);
    setIsModalOpen(true);
  };

  const closeDetails = () => {
    setIsModalOpen(false);
    setSelectedJobId(null);
  };

  const nextPage = () => {
    if (page < totalPages) setPage(p => p + 1);
  };

  const prevPage = () => {
    if (page > 1) setPage(p => p - 1);
  };

  const getStatusBadge = (status: string) => {
    const base = "px-3 py-1 rounded-full text-[10px] font-black uppercase tracking-wider border flex items-center gap-1.5";
    switch (status) {
      case 'Completed': return `${base} bg-success-50 text-success-700 border-success-200 shadow-sm shadow-success-100/50`;
      case 'Failed': return `${base} bg-error-50 text-error-700 border-error-200 shadow-sm shadow-error-100/50`;
      case 'Processing': return `${base} bg-primary-50 text-primary-700 border-primary-100 shadow-sm shadow-primary-100/50`;
      default: return `${base} bg-brand-50 text-brand-700 border-brand-200`;
    }
  };

  return (
    <div className="space-y-10">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-4xl font-black text-brand-900 tracking-tight">Batch Jobs</h1>
          <p className="text-brand-500 mt-2 font-medium">Automated data ingestion and synchronization pipelines.</p>
        </div>
        <div className="hidden md:flex items-center gap-4 px-6 py-3 bg-white rounded-2xl border border-brand-100 shadow-premium">
            <div className="flex items-center gap-2">
                <div className="w-2 h-2 rounded-full bg-success-500 animate-pulse" />
                <span className="text-xs font-bold text-brand-700">Cluster: Primary-A</span>
            </div>
            <div className="w-px h-4 bg-brand-100" />
            <div className="flex items-center gap-2">
                <Activity size={14} className="text-primary-500" />
                <span className="text-xs font-bold text-brand-700">Healthy</span>
            </div>
        </div>
      </div>

      <div className="bg-white p-8 rounded-[2rem] border border-brand-100 shadow-premium relative overflow-hidden group">
        <div className="absolute top-0 right-0 p-8 text-brand-50 transition-transform duration-700 group-hover:scale-110 group-hover:rotate-6 opacity-50">
            <Database size={160} />
        </div>
        <div className="relative z-10">
            <h2 className="text-xl font-black text-brand-900 mb-6 flex items-center gap-2">
                <Sparkles className="text-primary-500" size={20} />
                Start Ingestion Pipeline
            </h2>
            <form onSubmit={handleStartJob} className="flex flex-col sm:flex-row gap-4">
              <div className="flex-1 group">
                <input
                  type="text"
                  placeholder="Target City (e.g. Rotterdam)"
                  value={targetCity}
                  onChange={(e) => setTargetCity(e.target.value)}
                  className="w-full px-5 py-4 bg-brand-50/50 rounded-[1.25rem] border border-brand-100 focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-bold text-brand-900"
                  disabled={isStarting}
                />
              </div>
              <Button
                type="submit"
                variant="secondary"
                disabled={isStarting || !targetCity}
                isLoading={isStarting}
                leftIcon={!isStarting && <Play size={18} fill="currentColor" />}
                className="px-8"
              >
                Execute Pipeline
              </Button>
            </form>

            <div className="flex flex-col sm:flex-row items-center gap-4 mt-8 pt-6 border-t border-brand-100/50">
                <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setIsDatasetModalOpen(true)}
                    leftIcon={<Layers size={16} />}
                    className="text-brand-600 hover:bg-brand-50 font-bold"
                >
                    View Dataset Status
                </Button>
                <div className="hidden sm:block flex-1" />
                <Button
                    variant="outline"
                    size="sm"
                    onClick={handleIngestAll}
                    disabled={isStarting}
                    leftIcon={<Globe size={16} />}
                    className="w-full sm:w-auto border-brand-200 text-brand-600 hover:bg-brand-50 font-bold"
                >
                    Ingest All Netherlands
                </Button>
            </div>

            <p className="text-[10px] text-brand-400 mt-4 font-bold uppercase tracking-wider">
                Note: Ingestion jobs are resource-intensive. Avoid overlapping same-city jobs.
            </p>
        </div>
      </div>

      <div className="bg-white rounded-[2rem] border border-brand-100 shadow-premium overflow-hidden">
        <div className="px-8 py-6 border-b border-brand-100 bg-brand-50/30 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <h2 className="font-black text-brand-900 uppercase tracking-wider text-xs">Pipeline Execution History</h2>
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <select
                  value={statusFilter}
                  onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
                  className="px-4 py-2 bg-white border border-brand-100 rounded-xl text-xs font-bold text-brand-700 outline-none focus:ring-2 focus:ring-primary-500/20 cursor-pointer hover:border-brand-200 transition-colors"
              >
                  <option value="All">All Statuses</option>
                  <option value="Pending">Pending</option>
                  <option value="Processing">Processing</option>
                  <option value="Completed">Completed</option>
                  <option value="Failed">Failed</option>
              </select>
               <select
                  value={typeFilter}
                  onChange={(e) => { setTypeFilter(e.target.value); setPage(1); }}
                  className="px-4 py-2 bg-white border border-brand-100 rounded-xl text-xs font-bold text-brand-700 outline-none focus:ring-2 focus:ring-primary-500/20 cursor-pointer hover:border-brand-200 transition-colors"
              >
                  <option value="All">All Types</option>
                  <option value="CityIngestion">CityIngestion</option>
                  <option value="MapGeneration">MapGeneration</option>
                  <option value="AllCitiesIngestion">AllCitiesIngestion</option>
              </select>
            </div>
            <div className="flex items-center gap-2">
                <span className="w-2 h-2 rounded-full bg-primary-500 animate-pulse" />
                <span className="text-[10px] font-black text-primary-700 uppercase tracking-wider hidden sm:inline">Live</span>
            </div>
          </div>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-brand-100">
            <thead>
              <tr className="bg-brand-50/10">
                <th className="px-8 py-4 text-left text-[10px] font-black text-brand-400 uppercase tracking-wider">Job Definition</th>
                <th className="px-8 py-4 text-left text-[10px] font-black text-brand-400 uppercase tracking-wider">Target</th>
                <th className="px-8 py-4 text-left text-[10px] font-black text-brand-400 uppercase tracking-wider">Status</th>
                <th className="px-8 py-4 text-left text-[10px] font-black text-brand-400 uppercase tracking-wider">Internal Progress</th>
                <th className="px-8 py-4 text-left text-[10px] font-black text-brand-400 uppercase tracking-wider">Details</th>
                <th className="px-8 py-4 text-left text-[10px] font-black text-brand-400 uppercase tracking-wider">Timestamp</th>
                <th className="px-8 py-4 text-right text-[10px] font-black text-brand-400 uppercase tracking-wider">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-brand-100">
              {loading && jobs.length === 0 ? (
                [...Array(5)].map((_, i) => (
                    <tr key={i}>
                        <td className="px-8 py-6"><Skeleton variant="text" width="40%" /></td>
                        <td className="px-8 py-6"><Skeleton variant="text" width="60%" /></td>
                        <td className="px-8 py-6"><Skeleton variant="rectangular" width={80} height={24} className="rounded-lg" /></td>
                        <td className="px-8 py-6"><Skeleton variant="rectangular" width="100%" height={8} className="rounded-full" /></td>
                        <td className="px-8 py-6"><Skeleton variant="text" width="50%" /></td>
                        <td className="px-8 py-6"><Skeleton variant="text" width="80%" /></td>
                        <td className="px-8 py-6"></td>
                    </tr>
                ))
              ) : error ? (
                <tr>
                  <td colSpan={7} className="px-8 py-16 text-center">
                    <div className="flex flex-col items-center gap-4 text-error-500">
                        <AlertCircle size={32} className="opacity-50" />
                        <span className="font-bold">{error}</span>
                        <Button onClick={fetchJobs} variant="outline" size="sm" className="mt-2 border-error-200 text-error-700">Retry</Button>
                    </div>
                  </td>
                </tr>
              ) : jobs.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-8 py-16 text-center">
                    <div className="flex flex-col items-center gap-2 text-brand-400">
                        <Activity size={32} className="opacity-20 mb-2" />
                        <span className="font-bold">No pipeline history available.</span>
                    </div>
                  </td>
                </tr>
              ) : (
                <AnimatePresence mode="popLayout">
                  {jobs.map((job) => (
                    <motion.tr
                      key={job.id}
                      initial={{ opacity: 0 }}
                      animate={{ opacity: 1 }}
                      className="hover:bg-brand-50/50 transition-colors group cursor-pointer"
                      onClick={() => openDetails(job.id)}
                    >
                      <td className="px-8 py-5 whitespace-nowrap">
                        <div className="flex flex-col">
                            <span className="text-sm font-black text-brand-900">{job.type}</span>
                            <span className="text-[10px] text-brand-400 uppercase tracking-tighter">Job ID: {job.id.slice(0, 8)}...</span>
                        </div>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap text-sm font-bold text-brand-600">{job.target}</td>
                      <td className="px-8 py-5 whitespace-nowrap">
                        <div className="flex items-center gap-3">
                          <span className={getStatusBadge(job.status)}>{job.status}</span>
                        </div>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap">
                        <div className="flex flex-col gap-2">
                            <div className="w-full bg-brand-100 rounded-full h-2 min-w-[120px] overflow-hidden relative">
                              <motion.div
                                className={`h-2 rounded-full relative z-10 ${job.status === 'Failed' ? 'bg-error-500' : 'bg-primary-600'}`}
                                initial={{ width: 0 }}
                                animate={{ width: `${job.progress}%` }}
                                transition={{ duration: 1, ease: "easeOut" }}
                              >
                                {job.status === 'Processing' && (
                                  <motion.div
                                    className="absolute inset-0 bg-white/30"
                                    animate={{ x: ['-100%', '100%'] }}
                                    transition={{ duration: 1.5, repeat: Infinity, ease: "linear" }}
                                  />
                                )}
                              </motion.div>
                            </div>
                            <span className="text-[10px] text-brand-400 font-black tracking-wider">{job.progress}% COMPLETE</span>
                        </div>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap">
                          <div className="flex items-center gap-2 text-brand-600 text-sm font-medium max-w-[200px] truncate">
                              {(job.error || job.resultSummary) && <Info size={14} className="text-brand-300 flex-shrink-0" />}
                              {job.error ? 'Pipeline Error (see logs)' : (job.resultSummary || '-')}
                          </div>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap text-[11px] font-bold text-brand-500">
                        {new Date(job.createdAt).toLocaleString()}
                      </td>
                       <td className="px-8 py-5 whitespace-nowrap text-right">
                         <Button
                            variant="ghost"
                            size="sm"
                            className="text-brand-400 hover:text-brand-600 hover:bg-brand-100"
                            onClick={(e) => { e.stopPropagation(); openDetails(job.id); }}
                         >
                            View
                         </Button>
                       </td>
                    </motion.tr>
                  ))}
                </AnimatePresence>
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <div className="px-8 py-6 border-t border-brand-100 bg-brand-50/10 flex items-center justify-between">
          <div className="text-[10px] font-black text-brand-400 uppercase tracking-[0.1em]">
            Page <span className="text-brand-900">{page}</span> <span className="mx-2 text-brand-200">/</span> <span className="text-brand-900">{totalPages}</span>
          </div>
          <div className="flex gap-3">
            <Button
              variant="outline"
              size="sm"
              onClick={prevPage}
              disabled={page === 1 || loading}
              leftIcon={<ChevronLeft size={14} />}
            >
              Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={nextPage}
              disabled={page === totalPages || loading}
              rightIcon={<ChevronRight size={14} />}
            >
              Next
            </Button>
          </div>
        </div>
      </div>

      <JobDetailsModal
        isOpen={isModalOpen}
        onClose={closeDetails}
        jobId={selectedJobId}
        onJobUpdated={fetchJobs}
      />

      <DatasetStatusModal
        isOpen={isDatasetModalOpen}
        onClose={() => setIsDatasetModalOpen(false)}
      />
    </div>
  );
};

export default BatchJobs;
