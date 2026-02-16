import { ChevronLeft, ChevronRight } from 'lucide-react';

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  isLoading?: boolean;
}

const Pagination = ({ currentPage, totalPages, onPageChange, isLoading }: PaginationProps) => {
  return (
    <div className="flex items-center justify-between px-2 py-4 border-t border-brand-100">
      <div className="text-sm font-medium text-brand-500">
        Page <span className="text-brand-900">{currentPage}</span> of <span className="text-brand-900">{totalPages}</span>
      </div>
      <div className="flex space-x-3">
        <button
          onClick={() => onPageChange(Math.max(1, currentPage - 1))}
          disabled={currentPage === 1 || isLoading}
          className="flex items-center px-4 py-2 border border-brand-200 rounded-xl text-sm font-semibold text-brand-700 bg-white hover:bg-brand-50 disabled:opacity-40 disabled:hover:bg-white transition-all cursor-pointer"
        >
          <ChevronLeft className="mr-1 h-4 w-4" />
          Previous
        </button>
        <button
          onClick={() => onPageChange(Math.min(totalPages, currentPage + 1))}
          disabled={currentPage === totalPages || isLoading}
          className="flex items-center px-4 py-2 border border-brand-200 rounded-xl text-sm font-semibold text-brand-700 bg-white hover:bg-brand-50 disabled:opacity-40 disabled:hover:bg-white transition-all cursor-pointer"
        >
          Next
          <ChevronRight className="ml-1 h-4 w-4" />
        </button>
      </div>
    </div>
  );
};

export default Pagination;
