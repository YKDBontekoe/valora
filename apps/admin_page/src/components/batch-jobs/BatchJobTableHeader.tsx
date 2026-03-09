import React from 'react';
import { ArrowUp, ArrowDown } from 'lucide-react';

interface BatchJobTableHeaderProps {
  sortBy: string | undefined;
  toggleSort: (field: string) => void;
}

export const BatchJobTableHeader: React.FC<BatchJobTableHeaderProps> = ({ sortBy, toggleSort }) => {
  const getAriaSort = (field: string) => {
    if (sortBy === `${field}_asc`) return 'ascending';
    if (sortBy === `${field}_desc`) return 'descending';
    return 'none';
  };

  const isSortedBy = (field: string) => sortBy === `${field}_asc` || sortBy === `${field}_desc`;

  return (
    <thead className="bg-brand-50/50 backdrop-blur-md">
      <tr>
        <th
          className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] group transition-all duration-500 border-b border-brand-100"
          aria-sort={getAriaSort('type')}
        >
          <button
             className="flex items-center gap-3 w-full text-left font-inherit uppercase tracking-inherit cursor-pointer hover:bg-brand-100/50 focus:outline-none focus:ring-4 focus:ring-primary-500/10 rounded-md py-1"
             onClick={() => toggleSort('type')}
             aria-label="Sort by Definition"
          >
            Definition
            <div className={`flex flex-col gap-0.5 transition-opacity duration-500 ${isSortedBy('type') ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'}`}>
              <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'type_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
              <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'type_desc' ? 'text-primary-600' : 'text-brand-200'}`} />
            </div>
          </button>
        </th>
        <th
          className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] group transition-all duration-500 border-b border-brand-100"
          aria-sort={getAriaSort('target')}
        >
          <button
             className="flex items-center gap-3 w-full text-left font-inherit uppercase tracking-inherit cursor-pointer hover:bg-brand-100/50 focus:outline-none focus:ring-4 focus:ring-primary-500/10 rounded-md py-1"
             onClick={() => toggleSort('target')}
             aria-label="Sort by Target"
          >
            Target
            <div className={`flex flex-col gap-0.5 transition-opacity duration-500 ${isSortedBy('target') ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'}`}>
              <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'target_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
              <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'target_desc' ? 'text-primary-600' : 'text-brand-200'}`} />
            </div>
          </button>
        </th>
        <th
          className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] group transition-all duration-500 border-b border-brand-100"
          aria-sort={getAriaSort('status')}
        >
          <button
             className="flex items-center gap-3 w-full text-left font-inherit uppercase tracking-inherit cursor-pointer hover:bg-brand-100/50 focus:outline-none focus:ring-4 focus:ring-primary-500/10 rounded-md py-1"
             onClick={() => toggleSort('status')}
             aria-label="Sort by Status"
          >
            Status
            <div className={`flex flex-col gap-0.5 transition-opacity duration-500 ${isSortedBy('status') ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'}`}>
              <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'status_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
              <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'status_desc' ? 'text-primary-600' : 'text-brand-200'}`} />
            </div>
          </button>
        </th>
        <th className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] border-b border-brand-100">Progress</th>
        <th className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] border-b border-brand-100">Context</th>
        <th
          className="px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] group transition-all duration-500 border-b border-brand-100"
          aria-sort={getAriaSort('createdAt')}
        >
          <button
             className="flex items-center gap-3 w-full text-left font-inherit uppercase tracking-inherit cursor-pointer hover:bg-brand-100/50 focus:outline-none focus:ring-4 focus:ring-primary-500/10 rounded-md py-1"
             onClick={() => toggleSort('createdAt')}
             aria-label="Sort by Timestamp"
          >
            Timestamp
            <div className={`flex flex-col gap-0.5 transition-opacity duration-500 ${isSortedBy('createdAt') ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'}`}>
              <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === 'createdAt_asc' ? 'text-primary-600' : 'text-brand-200'}`} />
              <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === 'createdAt_desc' ? 'text-primary-600' : 'text-brand-200'}`} />
            </div>
          </button>
        </th>
        <th className="px-12 py-8 text-right text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] border-b border-brand-100">Action</th>
      </tr>
    </thead>
  );
};
