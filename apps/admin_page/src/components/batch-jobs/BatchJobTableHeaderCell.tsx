import React from 'react';
import { ArrowUp, ArrowDown } from 'lucide-react';

interface BatchJobTableHeaderCellProps {
  label: string;
  field: string;
  sortBy: string | undefined;
  toggleSort: (field: string) => void;
}

export const BatchJobTableHeaderCell: React.FC<BatchJobTableHeaderCellProps> = ({
  label,
  field,
  sortBy,
  toggleSort,
}) => {
  const getAriaSort = (field: string) => {
    if (sortBy === `${field}_asc`) return 'ascending';
    if (sortBy === `${field}_desc`) return 'descending';
    return 'none';
  };

  const handleKeyDown = (e: React.KeyboardEvent, field: string) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggleSort(field);
    }
  };

  const isActive = sortBy === `${field}_asc` || sortBy === `${field}_desc`;

  return (
    <th
      className="p-0 border-b border-brand-100"
      aria-sort={getAriaSort(field)}
    >
      <button
        type="button"
        className="w-full flex items-center gap-3 px-12 py-8 text-left text-[11px] font-black text-brand-400 uppercase tracking-[0.3em] cursor-pointer group hover:bg-brand-100/50 transition-all duration-500 select-none focus:outline-none focus:ring-4 focus:ring-primary-500/10"
        onClick={() => toggleSort(field)}
        onKeyDown={(e) => handleKeyDown(e, field)}
        aria-label={`Sort by ${label}`}
        aria-sort={getAriaSort(field)}
      >
        {label}
        <div className={`flex flex-col gap-0.5 transition-opacity duration-500 ${isActive ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'}`}>
          <ArrowUp className={`w-3.5 h-3.5 transition-colors ${sortBy === `${field}_asc` ? 'text-primary-600' : 'text-brand-200'}`} />
          <ArrowDown className={`w-3.5 h-3.5 transition-colors ${sortBy === `${field}_desc` ? 'text-primary-600' : 'text-brand-200'}`} />
        </div>
      </button>
    </th>
  );
};
