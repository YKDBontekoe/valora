import React from 'react';
import { Search, X, Filter } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import Button from '../Button';

interface BatchJobFiltersProps {
  searchQuery: string;
  setSearchQuery: (query: string) => void;
  statusFilter: string;
  setStatusFilter: (status: string) => void;
  typeFilter: string;
  setTypeFilter: (type: string) => void;
  hasActiveFilters: boolean;
  clearFilters: () => void;
  setPage: (page: number) => void;
}

export const BatchJobFilters: React.FC<BatchJobFiltersProps> = ({
  searchQuery,
  setSearchQuery,
  statusFilter,
  setStatusFilter,
  typeFilter,
  setTypeFilter,
  hasActiveFilters,
  clearFilters,
  setPage,
}) => {
  return (
    <div className="px-10 py-8 border-b border-brand-100 bg-brand-50/50 flex flex-col xl:flex-row xl:items-center justify-between gap-8">
      <div className="flex items-center gap-8 flex-1">
        <div className="flex items-center gap-3">
            <div className="p-2 bg-white rounded-lg border border-brand-100 shadow-sm">
                <Filter size={16} className="text-primary-600" />
            </div>
            <h2 className="font-black text-brand-900 uppercase tracking-[0.25em] text-xs whitespace-nowrap">
              Pipeline History
            </h2>
        </div>
        <div className="relative max-w-sm w-full group">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-brand-300 group-focus-within:text-primary-500 transition-all duration-300 group-focus-within:scale-110" />
          <input
            type="text"
            placeholder="Search by target..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-12 pr-10 py-3.5 bg-white border border-brand-100 rounded-2xl text-sm font-black text-brand-900 outline-none focus:ring-8 focus:ring-primary-500/5 focus:border-primary-500 transition-all placeholder:font-bold placeholder:text-brand-200 shadow-sm"
          />
          {searchQuery && (
              <button
                onClick={() => setSearchQuery('')}
                className="absolute right-3 top-1/2 -translate-y-1/2 p-1 hover:bg-brand-50 rounded-full text-brand-300 transition-colors"
              >
                  <X size={14} />
              </button>
          )}
        </div>
      </div>
      <div className="flex flex-col sm:flex-row sm:items-center gap-6">
        <div className="flex items-center gap-4">
          <AnimatePresence mode="wait">
            {hasActiveFilters && (
              <motion.div
                initial={{ opacity: 0, x: 10 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: 10 }}
              >
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={clearFilters}
                  leftIcon={<X size={16} />}
                  className="text-brand-400 hover:text-error-600 hover:bg-error-50 font-black"
                >
                  Clear Filters
                </Button>
              </motion.div>
            )}
          </AnimatePresence>

          <div className="relative">
              <select
                value={statusFilter}
                onChange={(e) => {
                  setStatusFilter(e.target.value);
                  setPage(1);
                }}
                className="pl-5 pr-10 py-3 bg-white border border-brand-100 rounded-2xl text-[10px] font-black text-brand-600 uppercase tracking-widest outline-none focus:ring-8 focus:ring-primary-500/5 focus:border-primary-500 cursor-pointer hover:border-brand-200 transition-all appearance-none shadow-sm"
              >
                <option value="All">All Statuses</option>
                <option value="Pending">Pending</option>
                <option value="Processing">Processing</option>
                <option value="Completed">Completed</option>
                <option value="Failed">Failed</option>
              </select>
              <div className="absolute right-4 top-1/2 -translate-y-1/2 pointer-events-none text-brand-300">
                  <Filter size={12} />
              </div>
          </div>

          <div className="relative">
              <select
                value={typeFilter}
                onChange={(e) => {
                  setTypeFilter(e.target.value);
                  setPage(1);
                }}
                className="pl-5 pr-10 py-3 bg-white border border-brand-100 rounded-2xl text-[10px] font-black text-brand-600 uppercase tracking-widest outline-none focus:ring-8 focus:ring-primary-500/5 focus:border-primary-500 cursor-pointer hover:border-brand-200 transition-all appearance-none shadow-sm"
              >
                <option value="All">All Types</option>
                <option value="CityIngestion">CityIngestion</option>
                <option value="MapGeneration">MapGeneration</option>
                <option value="AllCitiesIngestion">AllCitiesIngestion</option>
              </select>
              <div className="absolute right-4 top-1/2 -translate-y-1/2 pointer-events-none text-brand-300">
                  <Filter size={12} />
              </div>
          </div>
        </div>
        <div className="flex items-center gap-3 bg-primary-50 px-4 py-2 rounded-2xl border border-primary-100/50 shadow-sm ml-2">
          <span className="w-2.5 h-2.5 rounded-full bg-primary-500 animate-pulse" />
          <span className="text-[10px] font-black text-primary-700 uppercase tracking-widest hidden sm:inline">
            Realtime Sync
          </span>
        </div>
      </div>
    </div>
  );
};
