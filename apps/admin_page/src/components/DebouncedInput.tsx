import { useState, useEffect } from 'react';
import { Search } from 'lucide-react';

interface DebouncedInputProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'onChange'> {
  value: string;
  onChange: (value: string) => void;
  debounce?: number;
}

const DebouncedInput = ({
  value: initialValue,
  onChange,
  debounce = 500,
  ...props
}: DebouncedInputProps) => {
  const [value, setValue] = useState(initialValue);

  useEffect(() => {
    setValue(initialValue);
  }, [initialValue]);

  useEffect(() => {
    const timeout = setTimeout(() => {
      onChange(value);
    }, debounce);

    return () => clearTimeout(timeout);
  }, [value, debounce, onChange]);

  return (
    <div className="relative">
      <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
        <Search className="h-5 w-5 text-brand-400" />
      </div>
      <input
        {...props}
        value={value}
        onChange={(e) => setValue(e.target.value)}
        className={`block w-full pl-10 pr-3 py-2 border border-brand-200 rounded-xl leading-5 bg-white placeholder-brand-400 focus:outline-none focus:placeholder-brand-300 focus:border-primary-500 focus:ring-1 focus:ring-primary-500 sm:text-sm transition-all ${props.className || ''}`}
      />
    </div>
  );
};

export default DebouncedInput;
