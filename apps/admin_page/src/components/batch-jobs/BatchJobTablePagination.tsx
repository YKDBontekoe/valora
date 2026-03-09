import React from 'react';
import { Sparkles, ChevronLeft, ChevronRight } from 'lucide-react';
import Button from '../Button';

interface BatchJobTablePaginationProps {
  page: number;
  totalPages: number;
  loading: boolean;
  prevPage: () => void;
  nextPage: () => void;
}

export const BatchJobTablePagination: React.FC<BatchJobTablePaginationProps> = ({
  page,
  totalPages,
  loading,
  prevPage,
  nextPage,
}) => {
  const safeTotalPages = Math.max(1, totalPages);

  return (
    <div className="px-12 py-10 border-t border-brand-100 bg-brand-50/20 flex items-center justify-between backdrop-blur-md">
      <div className="flex items-center gap-4">
          <div className="w-10 h-10 bg-white rounded-xl flex items-center justify-center border border-brand-100 shadow-sm">
              <Sparkles size={18} className="text-primary-500" />
          </div>
          <div className="text-[12px] font-black text-brand-400 uppercase tracking-[0.3em]">
              Registry Page <span className="text-brand-900">{page}</span> <span className="mx-4 text-brand-200">/</span> <span className="text-brand-900">{safeTotalPages}</span>
          </div>
      </div>
      <div className="flex gap-6">
        <Button
          variant="outline"
          size="md"
          onClick={prevPage}
          disabled={page <= 1 || loading}
          leftIcon={<ChevronLeft size={20} />}
          className="font-black bg-white shadow-sm hover:shadow-md px-8"
        >
          Prev
        </Button>
        <Button
          variant="outline"
          size="md"
          onClick={nextPage}
          disabled={page >= safeTotalPages || loading}
          rightIcon={<ChevronRight size={20} />}
          className="font-black bg-white shadow-sm hover:shadow-md px-8"
        >
          Next
        </Button>
      </div>
    </div>
  );
};
