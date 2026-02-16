import { useState, useEffect, useCallback } from 'react';
import { adminService } from '../services/api';
import type { Listing, ListingFilterDto } from '../types';
import { motion, AnimatePresence } from 'framer-motion';
import { MapPin, Euro } from 'lucide-react';
import Pagination from '../components/Pagination';
import DebouncedInput from '../components/DebouncedInput';
import SortableHeader from '../components/SortableHeader';

const Listings = () => {
  const [listings, setListings] = useState<Listing[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);

  // Filters
  const [searchTerm, setSearchTerm] = useState('');
  const [city, setCity] = useState('');
  const [minPrice, setMinPrice] = useState<number | undefined>();
  const [maxPrice, setMaxPrice] = useState<number | undefined>();
  const [sortBy, setSortBy] = useState<string>('');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc');

  const fetchListings = useCallback(async () => {
    setLoading(true);
    try {
      const filters: ListingFilterDto = {
        page,
        pageSize: 10,
        searchTerm,
        city: city || undefined,
        minPrice,
        maxPrice,
        sortBy: sortBy || undefined,
        sortOrder: sortOrder || undefined,
      };
      const data = await adminService.getListings(filters);
      setListings(data.items || []);
      setTotalPages(data.totalPages);
    } catch {
      console.error('Failed to fetch listings');
    } finally {
      setLoading(false);
    }
  }, [page, searchTerm, city, minPrice, maxPrice, sortBy, sortOrder]);

  useEffect(() => {
    fetchListings();
  }, [fetchListings]);

  const handleSort = (field: string) => {
    if (sortBy === field) {
      setSortOrder(prev => prev === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(field);
      setSortOrder('asc');
    }
    setPage(1);
  };

  const handleSearch = (term: string) => {
    setSearchTerm(term);
    setPage(1);
  };

  const handleCitySearch = (term: string) => {
    setCity(term);
    setPage(1);
  };

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-brand-900">Listing Management</h1>
        <p className="text-brand-500 mt-1">Browse and monitor platform listings.</p>
      </div>

      <div className="bg-white shadow-premium rounded-2xl overflow-hidden border border-brand-100 mb-6 p-6">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div className="md:col-span-2">
            <label className="block text-sm font-medium text-brand-700 mb-1">Search Address</label>
            <DebouncedInput
              value={searchTerm}
              onChange={handleSearch}
              placeholder="Search by address..."
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-brand-700 mb-1">Filter City</label>
            <DebouncedInput
              value={city}
              onChange={handleCitySearch}
              placeholder="Filter by city..."
            />
          </div>
          <div className="flex gap-2">
            <div className="w-1/2">
              <label className="block text-sm font-medium text-brand-700 mb-1">Min Price</label>
              <input
                type="number"
                placeholder="Min"
                value={minPrice || ''}
                onChange={(e) => { setMinPrice(e.target.value ? Number(e.target.value) : undefined); setPage(1); }}
                className="block w-full px-3 py-2 border border-brand-200 rounded-xl leading-5 bg-white placeholder-brand-400 focus:outline-none focus:border-primary-500 focus:ring-1 focus:ring-primary-500 sm:text-sm transition-all"
              />
            </div>
            <div className="w-1/2">
              <label className="block text-sm font-medium text-brand-700 mb-1">Max Price</label>
              <input
                type="number"
                placeholder="Max"
                value={maxPrice || ''}
                onChange={(e) => { setMaxPrice(e.target.value ? Number(e.target.value) : undefined); setPage(1); }}
                className="block w-full px-3 py-2 border border-brand-200 rounded-xl leading-5 bg-white placeholder-brand-400 focus:outline-none focus:border-primary-500 focus:ring-1 focus:ring-primary-500 sm:text-sm transition-all"
              />
            </div>
          </div>
        </div>
      </div>

      <div className="bg-white shadow-premium rounded-2xl overflow-hidden border border-brand-100">
        <div className={`overflow-x-auto transition-opacity duration-200 ${loading && listings.length > 0 ? 'opacity-50' : 'opacity-100'}`}>
          <table className="min-w-full divide-y divide-brand-100">
            <thead className="bg-brand-50">
              <tr>
                <th className="px-8 py-4 text-left text-xs font-bold text-brand-500 uppercase tracking-widest">Address</th>
                <SortableHeader
                  label="Price"
                  field="Price"
                  currentSortBy={sortBy}
                  currentSortOrder={sortOrder}
                  onSort={handleSort}
                />
                 <SortableHeader
                  label="City"
                  field="City"
                  currentSortBy={sortBy}
                  currentSortOrder={sortOrder}
                  onSort={handleSort}
                />
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-brand-100">
              <AnimatePresence mode="popLayout">
                {loading && listings.length === 0 ? (
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
                      No listings found matching your filters.
                    </td>
                  </tr>
                ) : (
                  listings.map((listing) => (
                    <motion.tr
                      key={listing.id}
                      initial={{ opacity: 0 }}
                      animate={{ opacity: 1 }}
                      exit={{ opacity: 0 }}
                      layout
                      className="hover:bg-brand-50/50 transition-colors"
                    >
                      <td className="px-8 py-5 whitespace-nowrap text-sm">
                        <div className="flex items-center">
                          <div className="w-8 h-8 rounded-lg bg-brand-50 flex items-center justify-center mr-3">
                            <MapPin className="h-4 w-4 text-brand-400" />
                          </div>
                          <span className="font-semibold text-brand-900">{listing.address}</span>
                        </div>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap text-sm">
                        <div className="flex items-center text-brand-700 font-medium">
                          <Euro className="h-4 w-4 mr-1 text-brand-400" />
                          {listing.price != null ? listing.price.toLocaleString() : '-'}
                        </div>
                      </td>
                      <td className="px-8 py-5 whitespace-nowrap text-sm">
                        <span className="px-3 py-1 rounded-lg bg-brand-100 text-brand-700 text-xs font-bold uppercase tracking-wider">
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

        {/* Pagination */}
        <Pagination
          currentPage={page}
          totalPages={totalPages}
          onPageChange={setPage}
          isLoading={loading}
        />
      </div>
    </div>
  );
};

export default Listings;
