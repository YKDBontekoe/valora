import { useState, useEffect, useRef } from 'react';
import { listingService } from '../services/api';
import type { Listing, ListingFilter } from '../types';
import { motion, AnimatePresence } from 'framer-motion';
import { MapPin, Euro, ChevronLeft, ChevronRight, X, ArrowUpDown } from 'lucide-react';

const Listings = () => {
  const [listings, setListings] = useState<Listing[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const lastSuccessPage = useRef(1);
  const pageSize = 10;

  // Filter State
  const [searchTerm, setSearchTerm] = useState('');
  const [minPrice, setMinPrice] = useState<number | ''>('');
  const [maxPrice, setMaxPrice] = useState<number | ''>('');
  const [city, setCity] = useState('');
  const [sortBy, setSortBy] = useState<ListingFilter['sortBy']>('Date');
  const [sortOrder, setSortOrder] = useState<ListingFilter['sortOrder']>('desc');

  // Debounce search term
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState(searchTerm);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearchTerm(searchTerm);
    }, 500);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // Reset page when filters change
  useEffect(() => {
    setPage(1);
  }, [debouncedSearchTerm, minPrice, maxPrice, city, sortBy, sortOrder]);

  useEffect(() => {
    const fetchListings = async () => {
      setLoading(true);
      try {
        const filter: ListingFilter = {
          page,
          pageSize,
          searchTerm: debouncedSearchTerm || undefined,
          minPrice: minPrice !== '' ? Number(minPrice) : undefined,
          maxPrice: maxPrice !== '' ? Number(maxPrice) : undefined,
          city: city || undefined,
          sortBy,
          sortOrder,
        };

        const data = await listingService.getListings(filter);
        setListings(data.items || []);
        setTotalPages(data.totalPages || 1);
        lastSuccessPage.current = page;
      } catch {
        console.error('Failed to fetch listings');
        // Revert page on failure to keep UI in sync, but only if filters didn't change
        // Here we might just want to show an error state instead of reverting page
        setPage(lastSuccessPage.current);
      } finally {
        setLoading(false);
      }
    };
    fetchListings();
  }, [page, debouncedSearchTerm, minPrice, maxPrice, city, sortBy, sortOrder]);

  const handlePrevPage = () => {
    if (page > 1) setPage(p => p - 1);
  };

  const handleNextPage = () => {
    if (page < totalPages) setPage(p => p + 1);
  };

  // Helper to clear all filters
  const clearFilters = () => {
    setSearchTerm('');
    setMinPrice('');
    setMaxPrice('');
    setCity('');
    setSortBy('Date');
    setSortOrder('desc');
    setPage(1);
  };

  const hasActiveFilters = searchTerm || minPrice !== '' || maxPrice !== '' || city;

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-brand-900">Listing Management</h1>
        <p className="text-brand-500 mt-1">Browse and monitor platform listings.</p>
      </div>

      {/* Filters Section */}
      <div className="bg-white p-6 rounded-2xl shadow-sm border border-brand-100 mb-6">
        <div className="flex flex-col space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {/* Search */}
            <div>
                <label className="block text-xs font-bold text-brand-500 uppercase tracking-widest mb-2">Search</label>
                <input
                    type="text"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    placeholder="Address, City..."
                    className="w-full px-4 py-2 rounded-xl border border-brand-200 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all text-sm"
                />
            </div>
             {/* City */}
             <div>
                <label className="block text-xs font-bold text-brand-500 uppercase tracking-widest mb-2">City</label>
                <input
                    type="text"
                    value={city}
                    onChange={(e) => setCity(e.target.value)}
                    placeholder="Filter by city..."
                    className="w-full px-4 py-2 rounded-xl border border-brand-200 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all text-sm"
                />
            </div>
            {/* Price Range */}
            <div className="flex space-x-2">
                <div className="flex-1">
                    <label className="block text-xs font-bold text-brand-500 uppercase tracking-widest mb-2">Min Price</label>
                    <input
                        type="number"
                        value={minPrice}
                        onChange={(e) => setMinPrice(e.target.value ? Number(e.target.value) : '')}
                        placeholder="€ 0"
                        className="w-full px-4 py-2 rounded-xl border border-brand-200 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all text-sm"
                    />
                </div>
                <div className="flex-1">
                    <label className="block text-xs font-bold text-brand-500 uppercase tracking-widest mb-2">Max Price</label>
                    <input
                        type="number"
                        value={maxPrice}
                        onChange={(e) => setMaxPrice(e.target.value ? Number(e.target.value) : '')}
                        placeholder="€ Any"
                        className="w-full px-4 py-2 rounded-xl border border-brand-200 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all text-sm"
                    />
                </div>
            </div>
             {/* Sort */}
             <div className="flex space-x-2">
                <div className="flex-1">
                    <label className="block text-xs font-bold text-brand-500 uppercase tracking-widest mb-2">Sort By</label>
                    <select
                        value={sortBy}
                        onChange={(e) => setSortBy(e.target.value as ListingFilter['sortBy'])}
                        className="w-full px-4 py-2 rounded-xl border border-brand-200 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all text-sm bg-white"
                    >
                        <option value="Date">Date Added</option>
                        <option value="Price">Price</option>
                        <option value="City">City</option>
                    </select>
                </div>
                 <div className="w-1/3">
                    <label className="block text-xs font-bold text-brand-500 uppercase tracking-widest mb-2">Order</label>
                     <button
                        onClick={() => setSortOrder(prev => prev === 'asc' ? 'desc' : 'asc')}
                        className="w-full px-4 py-2 rounded-xl border border-brand-200 hover:bg-brand-50 flex items-center justify-center transition-all h-[38px] bg-white"
                     >
                        <ArrowUpDown className={`h-4 w-4 text-brand-500 transition-transform ${sortOrder === 'desc' ? 'rotate-180' : ''}`} />
                     </button>
                </div>
            </div>
          </div>

          {hasActiveFilters && (
              <div className="flex justify-end pt-2">
                  <button
                      onClick={clearFilters}
                      className="flex items-center text-xs font-bold text-brand-400 hover:text-error-500 uppercase tracking-widest transition-colors"
                  >
                      <X className="h-4 w-4 mr-1" />
                      Clear Filters
                  </button>
              </div>
          )}
        </div>
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
                        {hasActiveFilters ? (
                            <div className="flex flex-col items-center">
                                <span className="mb-2">No listings match your search criteria.</span>
                                <button onClick={clearFilters} className="text-primary-600 hover:text-primary-700 font-bold text-sm">Clear filters</button>
                            </div>
                        ) : (
                            "No listings found."
                        )}
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
