import { useState, useEffect, useRef } from 'react';
import { listingService } from '../services/api';
import type { Listing } from '../types';
import { motion, AnimatePresence } from 'framer-motion';
import { MapPin, Euro, ChevronLeft, ChevronRight } from 'lucide-react';

const Listings = () => {
  const [listings, setListings] = useState<Listing[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const lastSuccessPage = useRef(1);
  const pageSize = 10;

  useEffect(() => {
    const fetchListings = async () => {
      setLoading(true);
      try {
        const data = await listingService.getListings(page, pageSize);
        setListings(data.items || []);
        setTotalPages(data.totalPages || 1);
        lastSuccessPage.current = page;
      } catch {
        console.error('Failed to fetch listings');
        // Revert page on failure to keep UI in sync
        setPage(lastSuccessPage.current);
      } finally {
        setLoading(false);
      }
    };
    fetchListings();
  }, [page]);

  const handlePrevPage = () => {
    if (page > 1) setPage(p => p - 1);
  };

  const handleNextPage = () => {
    if (page < totalPages) setPage(p => p + 1);
  };

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-brand-900">Listing Management</h1>
        <p className="text-brand-500 mt-1">Browse and monitor platform listings.</p>
      </div>

      <div className="bg-white shadow-premium rounded-2xl overflow-hidden border border-brand-100 flex flex-col min-h-[600px]">
        <div className="overflow-x-auto flex-grow">
          <table className="min-w-full divide-y divide-brand-100">
            <thead className="bg-brand-50">
              <tr>
                <th className="px-8 py-4 text-left text-xs font-bold text-brand-500 uppercase tracking-widest">Address</th>
                <th className="px-8 py-4 text-left text-xs font-bold text-brand-500 uppercase tracking-widest">Price</th>
                <th className="px-8 py-4 text-left text-xs font-bold text-brand-500 uppercase tracking-widest">City</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-brand-100">
              <AnimatePresence mode="popLayout">
                {loading ? (
                  <tr>
                    <td colSpan={3} className="px-8 py-12 text-center">
                      <div className="flex flex-col items-center">
                        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600 mb-4"></div>
                        <span className="text-brand-500 font-medium">Loading listings...</span>
                      </div>
                    </td>
                  </tr>
                ) : listings.length === 0 ? (
                  <tr>
                    <td colSpan={3} className="px-8 py-12 text-center text-brand-500 font-medium">
                      No listings found.
                    </td>
                  </tr>
                ) : (
                  listings.map((listing) => (
                    <motion.tr
                      key={listing.id}
                      initial={{ opacity: 0, y: 4 }}
                      animate={{ opacity: 1, y: 0 }}
                      exit={{ opacity: 0, y: -4 }}
                      layout
                      className="hover:bg-brand-50/50 transition-colors group"
                    >
                      <td className="px-8 py-5 whitespace-nowrap text-sm">
                        <div className="flex items-center">
                          <div className="w-10 h-10 rounded-xl bg-brand-50 flex items-center justify-center mr-4 group-hover:bg-primary-50 transition-colors">
                            <MapPin className="h-5 w-5 text-brand-400 group-hover:text-primary-600 transition-colors" />
                          </div>
                          <span className="font-bold text-brand-900">{listing.address}</span>
                        </div>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap text-sm">
                        <div className="flex items-center text-brand-700 font-bold">
                          <Euro className="h-4 w-4 mr-1 text-brand-400" />
                          {listing.price != null ? listing.price.toLocaleString() : '-'}
                        </div>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap text-sm">
                        <span className="px-3 py-1.5 rounded-lg bg-brand-100 text-brand-700 text-[10px] font-black uppercase tracking-widest border border-brand-200/50">
                          {listing.city}
                        </span>
                      </td>
                    </motion.tr>
                  ))
                )}
              </AnimatePresence>
            </tbody>
          </table>
        </div>

        {/* Pagination Controls */}
        <div className="bg-brand-50 px-8 py-6 border-t border-brand-100 flex items-center justify-between">
            <span className="text-sm font-bold text-brand-400 uppercase tracking-widest">
                Page <span className="text-brand-900">{page}</span> <span className="mx-1 text-brand-200">/</span> <span className="text-brand-900">{totalPages}</span>
            </span>
            <div className="flex space-x-3">
                <motion.button
                    whileTap={{ scale: 0.95 }}
                    onClick={handlePrevPage}
                    disabled={page === 1 || loading}
                    className="p-2.5 rounded-xl bg-white border border-brand-200 shadow-sm hover:bg-brand-50 hover:border-brand-300 disabled:opacity-40 disabled:hover:bg-white disabled:shadow-none transition-all text-brand-600 cursor-pointer"
                >
                    <ChevronLeft className="h-5 w-5" />
                </motion.button>
                <motion.button
                    whileTap={{ scale: 0.95 }}
                    onClick={handleNextPage}
                    disabled={page === totalPages || loading}
                    className="p-2.5 rounded-xl bg-white border border-brand-200 shadow-sm hover:bg-brand-50 hover:border-brand-300 disabled:opacity-40 disabled:hover:bg-white disabled:shadow-none transition-all text-brand-600 cursor-pointer"
                >
                    <ChevronRight className="h-5 w-5" />
                </motion.button>
            </div>
        </div>
      </div>
    </div>
  );
};

export default Listings;
