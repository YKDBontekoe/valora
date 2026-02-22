import Skeleton from './Skeleton';

interface LoadingStateProps {
  rows?: number;
}

const LoadingState = ({ rows = 3 }: LoadingStateProps) => {
  return (
    <div className="space-y-4">
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="bg-white p-6 rounded-2xl border border-brand-100 shadow-sm flex items-center gap-4">
          <Skeleton variant="circular" width={40} height={40} />
          <div className="space-y-2 flex-1">
            <Skeleton variant="text" width="30%" height={16} />
            <Skeleton variant="text" width="60%" height={12} />
          </div>
        </div>
      ))}
    </div>
  );
};

export default LoadingState;
