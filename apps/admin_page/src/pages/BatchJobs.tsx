import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Activity, Play, AlertCircle, Database, Sparkles, Info, ChevronLeft, ChevronRight, Globe, Layers, ArrowUp, ArrowDown, Search, X } from 'lucide-react';
import { adminService } from '../services/api';
// Removed unused BatchJob import
import Button from '../components/Button';
import { showToast } from '../services/toast';
import Skeleton from '../components/Skeleton';
import JobDetailsModal from './JobDetailsModal';
import DatasetStatusModal from './DatasetStatusModal';
import ConfirmationDialog from '../components/ConfirmationDialog';
import { useBatchJobsPolling } from '../hooks/useBatchJobsPolling';

const listVariants = {
  visible: {
    transition: {
      staggerChildren: 0.05
    }
  }
};

const rowVariants = {
  hidden: { opacity: 0, x: -10 },
  visible: {
    opacity: 1,
    x: 0,
    transition: { duration: 0.4, ease: [0.22, 1, 0.36, 1] as const }
  },
  exit: { opacity: 0, x: 10, transition: { duration: 0.2 } }
} as const;

const BatchJobs: React.FC = () => {
  // Pagination & Filtering
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('All');
  const [typeFilter, setTypeFilter] = useState('All');
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [sortBy, setSortBy] = useState<string | undefined>(undefined);
  const pageSize = 10;

  // Use Custom Hook for Data & Polling
  const {
    jobs,
    health,
    loading,
    error,
    totalPages,
    refresh
  } = useBatchJobsPolling({
    page,
    pageSize,
    statusFilter,
    typeFilter,
    searchQuery: debouncedSearch,
    sortBy
  });

  const [isStarting, setIsStarting] = useState(false);
  const [targetCity, setTargetCity] = useState('');

  // Modal State
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isDatasetModalOpen, setIsDatasetModalOpen] = useState(false);
  const [confirmation, setConfirmation] = useState<{
    isOpen: boolean;
    title: string;
    message: string;
    confirmLabel: string;
    onConfirm: () => Promise<void>;
    isDestructive?: boolean;
  }>({
    isOpen: false,
    title: '',
    message: '',
    confirmLabel: '',
    onConfirm: async () => {},
  });

  const hasActiveFilters = statusFilter !== 'All' || typeFilter !== 'All' || searchQuery !== '';

  const clearFilters = () => {
    setStatusFilter('All');
    setTypeFilter('All');
    setSearchQuery('');
    setDebouncedSearch('');
    setSortBy(undefined);
    setPage(1);
  };

  // Debounce search query
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchQuery);
      setPage(1);
    }, 500);
    return () => clearTimeout(handler);
  }, [searchQuery]);

  const toggleSort = (field: string) => {
    setSortBy(current => {
      if (current === `${field}_asc`) return `${field}_desc`;
      if (current === `${field}_desc`) return undefined;
      return `${field}_asc`;
    });
    setPage(1);
  };

  const handleStartJob = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!targetCity) return;

    setIsStarting(true);
    try {
      await adminService.startJob('CityIngestion', targetCity);
      showToast('Pipeline initiated successfully', 'success');
      setTargetCity('');
      refresh();
    } catch {
      // Toast handled by api interceptor
    } finally {
      setIsStarting(false);
    }
  };

  const handleIngestAll = async () => {
    setConfirmation({
        isOpen: true,
        title: 'Start Full Ingestion?',
        message: 'Are you sure you want to trigger ingestion for ALL municipalities? This will queue hundreds of jobs and may impact system performance.',
        confirmLabel: 'Start Ingestion',
        isDestructive: false,
        onConfirm: async () => {
            try {
              await adminService.startJob('AllCitiesIngestion', 'Netherlands');
              showToast('Full dataset ingestion pipeline initiated', 'success');
              refresh();
            } catch {
              showToast('Failed to start ingestion pipeline. Please try again.', 'error');
            } finally {
              setConfirmation(prev => ({ ...prev, isOpen: false }));
            }
        }
     });
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
    <div className="space-y-12">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div>
          <h1 className="text-5xl font-black text-brand-900 tracking-tightest">Batch Jobs</h1>
          <p className="text-brand-400 mt-3 font-bold text-lg">Automated data ingestion and synchronization pipelines.</p>
        </div>
        <div className="flex items-center gap-4 px-6 py-4 bg-white rounded-2xl border border-brand-100 shadow-premium group cursor-default">
            <div className="flex items-center gap-3">
                <div className={`w-2.5 h-2.5 rounded-full ${health?.status === 'Healthy' ? 'bg-success-500 animate-pulse' : 'bg-error-500'}`} />
                <span className="text-[10px] font-black text-brand-900 uppercase tracking-widest">Cluster: Primary-A</span>
            </div>
            <div className="w-px h-6 bg-brand-100" />
            <div className="flex items-center gap-3">
                <Activity size={16} className={health?.status === 'Healthy' ? "text-success-500" : "text-error-500"} />
                <span className="text-sm font-black text-brand-900">{health?.status || 'Connecting...'}</span>
            </div>
        </div>
      </div>

      <div className="bg-white p-10 rounded-[2.5rem] border border-brand-100 shadow-premium relative overflow-hidden group">
        <div className="absolute top-0 right-0 p-10 text-brand-50 transition-transform duration-1000 group-hover:scale-125 group-hover:rotate-12 opacity-50">
            <Database size={200} />
        </div>
        <div className="relative z-10">
            <h2 className="text-2xl font-black text-brand-900 mb-8 flex items-center gap-3">
                <Sparkles className="text-primary-500" size={24} />
                Start Ingestion Pipeline
            </h2>
            <form onSubmit={handleStartJob} className="flex flex-col sm:flex-row gap-5">
              <div className="flex-1 relative group/input">
                <input
                  type="text"
                  placeholder="Target City (e.g. Rotterdam)"
                  value={targetCity}
                  onChange={(e) => setTargetCity(e.target.value)}
                  className="w-full px-6 py-5 bg-brand-50/50 rounded-2xl border border-brand-100 focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 focus:bg-white outline-none transition-all font-black text-brand-900 placeholder:text-brand-300 placeholder:font-bold"
                  disabled={isStarting}
                />
              </div>
              <Button
                type="submit"
                variant="secondary"
                disabled={isStarting || !targetCity}
                isLoading={isStarting}
                leftIcon={!isStarting && <Play size={20} fill="currentColor" />}
                className="px-10"
              >
                Execute Pipeline
              </Button>
            </form>

            <div className="flex flex-col sm:flex-row items-center gap-6 mt-10 pt-8 border-t border-brand-100/50">
                <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setIsDatasetModalOpen(true)}
                    leftIcon={<Layers size={18} />}
                    className="text-brand-500 hover:bg-brand-50 font-black"
                >
                    View Dataset Status
                </Button>
                <div className="hidden sm:block flex-1" />
                <Button
                    variant="outline"
                    size="sm"
                    onClick={handleIngestAll}
                    disabled={isStarting}
                    leftIcon={<Globe size={18} />}
                    className="w-full sm:w-auto border-brand-200 text-brand-500 hover:bg-brand-50 font-black"
                >
                    Ingest All Netherlands
                </Button>
            </div>

            <p className="text-[10px] text-brand-300 mt-6 font-black uppercase tracking-[0.2em]">
                Warning: Ingestion jobs are resource-intensive. Avoid overlapping same-city jobs.
            </p>
        </div>
      </div>

      <div className="bg-white rounded-[2.5rem] border border-brand-100 shadow-premium overflow-hidden">
        <div className="px-10 py-8 border-b border-brand-100 bg-brand-50/30 flex flex-col xl:flex-row xl:items-center justify-between gap-6">
          <div className="flex items-center gap-6 flex-1">
            <h2 className="font-black text-brand-900 uppercase tracking-[0.2em] text-xs whitespace-nowrap">Pipeline History</h2>
             <div className="relative max-w-sm w-full group">
                <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-brand-300 group-focus-within:text-primary-500 transition-colors" />
                <input
                    type="text"
                    placeholder="Search by target..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="w-full pl-11 pr-4 py-3 bg-white border border-brand-100 rounded-xl text-sm font-black text-brand-900 outline-none focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 transition-all placeholder:font-bold placeholder:text-brand-200"
                />
            </div>
          </div>
          <div className="flex flex-col sm:flex-row sm:items-center gap-4">
            <div className="flex items-center gap-3">
              <AnimatePresence>
                {hasActiveFilters && (
                    <motion.div
                        initial={{ opacity: 0, scale: 0.9 }}
                        animate={{ opacity: 1, scale: 1 }}
                        exit={{ opacity: 0, scale: 0.9 }}
                    >
                        <Button
                            variant="ghost"
                            size="sm"
                            onClick={clearFilters}
                            leftIcon={<X size={16} />}
                            className="text-brand-400 hover:text-brand-600 hover:bg-brand-50 mr-2"
                        >
                            Clear
                        </Button>
                    </motion.div>
                )}
              </AnimatePresence>
              <select
                  value={statusFilter}
                  onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
                  className="px-5 py-2.5 bg-white border border-brand-100 rounded-xl text-[10px] font-black text-brand-600 uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 cursor-pointer hover:border-brand-200 transition-colors appearance-none"
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
                  className="px-5 py-2.5 bg-white border border-brand-100 rounded-xl text-[10px] font-black text-brand-600 uppercase tracking-widest outline-none focus:ring-4 focus:ring-primary-500/10 cursor-pointer hover:border-brand-200 transition-colors appearance-none"
              >
                  <option value="All">All Types</option>
                  <option value="CityIngestion">CityIngestion</option>
                  <option value="MapGeneration">MapGeneration</option>
                  <option value="AllCitiesIngestion">AllCitiesIngestion</option>
              </select>
            </div>
            <div className="flex items-center gap-2 bg-primary-50 px-3 py-1.5 rounded-full border border-primary-100/50 ml-2">
                <span className="w-2 h-2 rounded-full bg-primary-500 animate-pulse" />
                <span className="text-[10px] font-black text-primary-700 uppercase tracking-wider hidden sm:inline">Live</span>
            </div>
          </div>
        </div>
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-brand-100">
            <thead>
              <tr className="bg-brand-50/10">
                <th
                    className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest cursor-pointer group hover:bg-brand-100/30 transition-colors select-none"
                    onClick={() => toggleSort('type')}
                >
                    <div className="flex items-center gap-2">
                        Definition
                        <div className="flex flex-col">
                            <ArrowUp className={`w-3 h-3 -mb-1 transition-colors ${sortBy === 'type_asc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                            <ArrowDown className={`w-3 h-3 transition-colors ${sortBy === 'type_desc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                        </div>
                    </div>
                </th>
                <th
                    className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest cursor-pointer group hover:bg-brand-100/30 transition-colors select-none"
                    onClick={() => toggleSort('target')}
                >
                    <div className="flex items-center gap-2">
                        Target
                        <div className="flex flex-col">
                            <ArrowUp className={`w-3 h-3 -mb-1 transition-colors ${sortBy === 'target_asc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                            <ArrowDown className={`w-3 h-3 transition-colors ${sortBy === 'target_desc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                        </div>
                    </div>
                </th>
                <th
                    className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest cursor-pointer group hover:bg-brand-100/30 transition-colors select-none"
                    onClick={() => toggleSort('status')}
                >
                    <div className="flex items-center gap-2">
                        Status
                        <div className="flex flex-col">
                            <ArrowUp className={`w-3 h-3 -mb-1 transition-colors ${sortBy === 'status_asc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                            <ArrowDown className={`w-3 h-3 transition-colors ${sortBy === 'status_desc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                        </div>
                    </div>
                </th>
                <th className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Progress</th>
                <th className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest">Context</th>
                <th
                    className="px-10 py-5 text-left text-[10px] font-black text-brand-400 uppercase tracking-widest cursor-pointer group hover:bg-brand-100/30 transition-colors select-none"
                    onClick={() => toggleSort('createdAt')}
                >
                    <div className="flex items-center gap-2">
                        Timestamp
                        <div className="flex flex-col">
                            <ArrowUp className={`w-3 h-3 -mb-1 transition-colors ${sortBy === 'createdAt_asc' ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                            <ArrowDown className={`w-3 h-3 transition-colors ${sortBy === 'createdAt_desc' || (!sortBy) ? 'text-primary-600' : 'text-brand-200 group-hover:text-brand-300'}`} />
                        </div>
                    </div>
                </th>
                <th className="px-10 py-5 text-right text-[10px] font-black text-brand-400 uppercase tracking-widest">Action</th>
              </tr>
            </thead>
            <motion.tbody
                initial="hidden"
                animate="visible"
                variants={listVariants}
                className="divide-y divide-brand-100"
            >
              {loading && jobs.length === 0 ? (
                [...Array(5)].map((_, i) => (
                    <tr key={i}>
                        <td className="px-10 py-6"><Skeleton variant="text" width="40%" /></td>
                        <td className="px-10 py-6"><Skeleton variant="text" width="60%" /></td>
                        <td className="px-10 py-6"><Skeleton variant="rectangular" width={80} height={24} className="rounded-lg" /></td>
                        <td className="px-10 py-6"><Skeleton variant="rectangular" width="100%" height={8} className="rounded-full" /></td>
                        <td className="px-10 py-6"><Skeleton variant="text" width="50%" /></td>
                        <td className="px-10 py-6"><Skeleton variant="text" width="80%" /></td>
                        <td className="px-10 py-6"></td>
                    </tr>
                ))
              ) : error ? (
                <tr>
                  <td colSpan={7} className="px-10 py-20 text-center">
                    <div className="flex flex-col items-center gap-6 text-error-500">
                        <AlertCircle size={48} className="opacity-20" />
                        <span className="font-black text-xl">{error}</span>
                        <Button onClick={refresh} variant="outline" size="sm" className="mt-4 border-error-200 text-error-700">Retry Pipeline Sync</Button>
                    </div>
                  </td>
                </tr>
              ) : jobs.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-10 py-24 text-center">
                    <div className="flex flex-col items-center gap-4 text-brand-200">
                        <Activity size={64} className="opacity-10 mb-2" />
                        <span className="font-black text-xl uppercase tracking-widest">Empty Pipeline History</span>
                    </div>
                  </td>
                </tr>
              ) : (
                <AnimatePresence mode="popLayout">
                  {jobs.map((job) => (
                    <motion.tr
                      key={job.id}
                      variants={rowVariants}
                      whileHover={{ scale: 1.005, backgroundColor: 'var(--color-brand-50)', transition: { duration: 0.2 } }}
                      className="group cursor-pointer relative"
                      onClick={() => openDetails(job.id)}
                    >
                      <td className="px-10 py-6 whitespace-nowrap">
                        <div className="flex flex-col">
                            <span className="text-sm font-black text-brand-900 group-hover:text-primary-700 transition-colors">{job.type}</span>
                            <span className="text-[10px] text-brand-300 font-black uppercase tracking-tighter mt-0.5">ID: {job.id.slice(0, 8)}</span>
                        </div>
                      </td>
                      <td className="px-10 py-6 whitespace-nowrap text-sm font-black text-brand-600">{job.target}</td>
                      <td className="px-10 py-6 whitespace-nowrap">
                        <div className="flex items-center gap-3">
                          <span className={getStatusBadge(job.status)}>{job.status}</span>
                        </div>
                      </td>
                      <td className="px-10 py-6 whitespace-nowrap">
                        <div className="flex flex-col gap-2.5">
                            <div className="w-full bg-brand-50 rounded-full h-2.5 min-w-[140px] overflow-hidden relative border border-brand-100/50">
                              <motion.div
                                className={`h-full rounded-full relative z-10 ${job.status === 'Failed' ? 'bg-error-500 shadow-[0_0_10px_rgba(239,68,68,0.4)]' : 'bg-linear-to-r from-primary-500 to-primary-600 shadow-[0_0_10px_rgba(124,58,237,0.3)]'}`}
                                initial={{ width: 0 }}
                                animate={{ width: `${job.progress}%` }}
                                transition={{ duration: 1.2, ease: "circOut" }}
                              >
                                {job.status === 'Processing' && (
                                  <motion.div
                                    className="absolute inset-0 bg-linear-to-r from-transparent via-white/40 to-transparent"
                                    animate={{ x: ['-100%', '200%'] }}
                                    transition={{ duration: 2, repeat: Infinity, ease: "linear" }}
                                  />
                                )}
                              </motion.div>
                            </div>
                            <span className="text-[10px] text-brand-400 font-black tracking-widest">{job.progress}% COMPLETE</span>
                        </div>
                      </td>
                      <td className="px-10 py-6 whitespace-nowrap">
                          <div className="flex items-center gap-2 text-brand-500 text-sm font-bold max-w-[220px] truncate">
                              {(job.error || job.resultSummary) && <Info size={14} className="text-brand-200 flex-shrink-0" />}
                              {job.error ? 'Pipeline Fault (check logs)' : (job.resultSummary || 'No summary available')}
                          </div>
                      </td>
                      <td className="px-10 py-6 whitespace-nowrap text-[11px] font-black text-brand-400">
                        {new Date(job.createdAt).toLocaleString()}
                      </td>
                       <td className="px-10 py-6 whitespace-nowrap text-right">
                         <Button
                            variant="ghost"
                            size="sm"
                            className="text-brand-300 hover:text-primary-600 hover:bg-primary-50"
                            onClick={(e) => { e.stopPropagation(); openDetails(job.id); }}
                         >
                            Details
                         </Button>
                       </td>
                    </motion.tr>
                  ))}
                </AnimatePresence>
              )}
            </motion.tbody>
          </table>
        </div>

        {/* Pagination */}
        <div className="px-10 py-8 border-t border-brand-100 bg-brand-50/10 flex items-center justify-between">
          <div className="text-[10px] font-black text-brand-400 uppercase tracking-[0.25em]">
            Sequence <span className="text-brand-900">{page}</span> <span className="mx-3 text-brand-200">/</span> <span className="text-brand-900">{totalPages}</span>
          </div>
          <div className="flex gap-4">
            <Button
              variant="outline"
              size="sm"
              onClick={prevPage}
              disabled={page === 1 || loading}
              leftIcon={<ChevronLeft size={16} />}
              className="font-black"
            >
              Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={nextPage}
              disabled={page === totalPages || loading}
              rightIcon={<ChevronRight size={16} />}
              className="font-black"
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
        onJobUpdated={refresh}
      />

      <DatasetStatusModal
        isOpen={isDatasetModalOpen}
        onClose={() => setIsDatasetModalOpen(false)}
      />

      <ConfirmationDialog
        isOpen={confirmation.isOpen}
        onClose={() => setConfirmation(prev => ({ ...prev, isOpen: false }))}
        onConfirm={confirmation.onConfirm}
        title={confirmation.title}
        message={confirmation.message}
        confirmLabel={confirmation.confirmLabel}
        isDestructive={confirmation.isDestructive}
      />
    </div>
  );
};

export default BatchJobs;
