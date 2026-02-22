import React, { useEffect, useState } from 'react';
import { aiService, type ExternalAiModel } from '../services/api';
import Button from './Button';
import { showToast } from '../services/toast';

interface ModelSelectorProps {
  onSelect: (modelId: string) => void;
  onClose: () => void;
}

const ModelSelector: React.FC<ModelSelectorProps> = ({ onSelect, onClose }) => {
  const [models, setModels] = useState<ExternalAiModel[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [sort, setSort] = useState<'free' | 'price_asc' | 'price_desc' | 'context'>('free');

  useEffect(() => {
    loadModels();
  }, []);

  const loadModels = async () => {
    try {
      const data = await aiService.getAvailableModels();
      setModels(data);
    } catch (error) {
      console.error(error);
      showToast('Failed to load models', 'error');
    } finally {
      setLoading(false);
    }
  };

  const filteredModels = models
    .filter(m => m.name.toLowerCase().includes(search.toLowerCase()) || m.id.toLowerCase().includes(search.toLowerCase()))
    .sort((a, b) => {
      if (sort === 'free') {
        if (a.isFree && !b.isFree) return -1;
        if (!a.isFree && b.isFree) return 1;
        // Secondary sort by price asc
        return (a.promptPrice + a.completionPrice) - (b.promptPrice + b.completionPrice);
      }
      if (sort === 'price_asc') {
         return (a.promptPrice + a.completionPrice) - (b.promptPrice + b.completionPrice);
      }
      if (sort === 'price_desc') {
         return (b.promptPrice + b.completionPrice) - (a.promptPrice + a.completionPrice);
      }
      if (sort === 'context') {
        return b.contextLength - a.contextLength;
      }
      return 0;
    });

  return (
    <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full flex justify-center items-center z-50">
      <div className="bg-white p-8 rounded-lg shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col">
        <div className="flex justify-between items-center mb-4">
            <h2 className="text-xl font-bold">Select AI Model</h2>
            <Button onClick={onClose} variant="secondary" size="sm">Close</Button>
        </div>

        <div className="flex gap-4 mb-4">
            <input
                type="text"
                className="shadow appearance-none border rounded w-full py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Search models..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
            />
            <select
                className="shadow border rounded py-2 px-3 text-gray-700 leading-tight focus:outline-none focus:ring-2 focus:ring-blue-500"
                value={sort}
                onChange={(e) => setSort(e.target.value as 'free' | 'price_asc' | 'price_desc' | 'context')}
            >
                <option value="free">Free First</option>
                <option value="price_asc">Price: Low to High</option>
                <option value="price_desc">Price: High to Low</option>
                <option value="context">Context Length</option>
            </select>
        </div>

        <div className="overflow-y-auto flex-1 border rounded">
            <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50 sticky top-0">
                    <tr>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Context</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Price (Prompt/Compl)</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Action</th>
                    </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                    {loading ? (
                        <tr><td colSpan={4} className="p-4 text-center">Loading models...</td></tr>
                    ) : filteredModels.length === 0 ? (
                        <tr><td colSpan={4} className="p-4 text-center">No models found</td></tr>
                    ) : (
                        filteredModels.map(model => (
                            <tr key={model.id} className="hover:bg-gray-50">
                                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                                    <div className="font-bold">{model.name}</div>
                                    <div className="text-xs text-gray-500">{model.id}</div>
                                </td>
                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                    {model.contextLength.toLocaleString()}
                                </td>
                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                    {model.isFree ? (
                                        <span className="text-green-600 font-bold">Free</span>
                                    ) : (
                                        <div className="text-xs">
                                            <div>P: ${model.promptPrice}</div>
                                            <div>C: ${model.completionPrice}</div>
                                        </div>
                                    )}
                                </td>
                                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                                    <Button size="sm" onClick={() => onSelect(model.id)}>Select</Button>
                                </td>
                            </tr>
                        ))
                    )}
                </tbody>
            </table>
        </div>
      </div>
    </div>
  );
};

export default ModelSelector;
