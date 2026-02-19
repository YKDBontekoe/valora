import { motion } from 'framer-motion';
import { MapPin, Euro, ArrowRight } from 'lucide-react';
import type { Listing } from '../types';

interface ListingRowProps {
  listing: Listing;
}

const rowVariants = {
  hidden: { opacity: 0, y: 10 },
  visible: { opacity: 1, y: 0 }
};

const ListingRow = ({ listing }: ListingRowProps) => (
  <motion.tr
    variants={rowVariants}
    exit={{ opacity: 0, scale: 0.98 }}
    layout
    className="hover:bg-brand-50/80 transition-all duration-200 group cursor-default hover:scale-[1.005] hover:shadow-sm"
  >
    <td className="px-8 py-5 whitespace-nowrap">
      <div className="flex items-center">
        <div className="w-12 h-12 rounded-2xl bg-brand-50 flex items-center justify-center mr-4 group-hover:bg-white group-hover:shadow-premium group-hover:scale-110 transition-all duration-300">
          <MapPin className="h-6 w-6 text-brand-400 group-hover:text-primary-600 transition-colors" />
        </div>
        <div className="flex flex-col">
            <span className="font-bold text-brand-900 group-hover:text-primary-700 transition-colors">{listing.address}</span>
            <span className="text-xs text-brand-400 font-medium">Netherlands</span>
        </div>
      </div>
    </td>
    <td className="px-8 py-5 whitespace-nowrap">
      <div className="flex items-center text-brand-900 font-black text-lg tracking-tight">
        <Euro className="h-4 w-4 mr-1 text-brand-300 group-hover:text-primary-500 transition-colors" />
        {listing.price != null ? listing.price.toLocaleString() : '-'}
      </div>
    </td>
    <td className="px-8 py-5 whitespace-nowrap">
      <div className="flex items-center justify-between">
          <span className="px-4 py-1.5 rounded-xl bg-brand-100 text-brand-700 text-[10px] font-black uppercase tracking-widest border border-brand-200/50 shadow-sm group-hover:bg-primary-50 group-hover:text-primary-700 group-hover:border-primary-100 transition-colors">
            {listing.city}
          </span>
          <ArrowRight className="w-4 h-4 text-brand-200 opacity-0 -translate-x-2 group-hover:opacity-100 group-hover:translate-x-0 transition-all duration-300" />
      </div>
    </td>
  </motion.tr>
);

export default ListingRow;
