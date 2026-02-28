import { motion } from 'framer-motion';
import type { HTMLMotionProps } from 'framer-motion';
import type { ReactNode } from 'react';

interface ButtonProps extends Omit<HTMLMotionProps<'button'>, 'children'> {
  children: ReactNode;
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
  leftIcon?: ReactNode;
  rightIcon?: ReactNode;
}

const Button = ({
  children,
  variant = 'primary',
  size = 'md',
  isLoading = false,
  leftIcon,
  rightIcon,
  className = '',
  disabled,
  ...props
}: ButtonProps) => {
  const baseStyles = 'inline-flex items-center justify-center font-black transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed outline-none select-none relative overflow-hidden';

  const variants = {
    primary: 'bg-primary-600 text-white hover:bg-primary-700 shadow-premium-lg shadow-primary-200/30 hover:shadow-primary-400/40 border border-primary-500',
    secondary: 'bg-brand-900 text-white hover:bg-brand-800 shadow-premium-lg shadow-brand-200/30 hover:shadow-brand-400/40 border border-brand-800',
    outline: 'bg-white border-2 border-brand-100 text-brand-700 hover:bg-brand-50 hover:border-brand-300 hover:text-brand-900 shadow-sm',
    ghost: 'bg-transparent text-brand-500 hover:bg-brand-50 hover:text-brand-900',
    danger: 'bg-error-50 text-error-700 border border-error-100 hover:bg-error-100 hover:text-error-800 hover:shadow-error-100/50'
  };

  const sizes = {
    sm: 'px-4 py-2.5 text-[10px] uppercase tracking-[0.2em] rounded-xl',
    md: 'px-6 py-4 text-sm rounded-2xl tracking-tight',
    lg: 'px-10 py-5 text-base rounded-3xl tracking-tight'
  };

  return (
    <motion.button
      whileTap={{ scale: 0.96, y: 0 }}
      whileHover={{
        y: -2,
        transition: { type: 'spring', stiffness: 400, damping: 12 }
      }}
      disabled={disabled || isLoading}
      className={`${baseStyles} ${variants[variant]} ${sizes[size]} ${className}`}
      {...props}
    >
      {/* Premium Shine Effect for primary/secondary */}
      {(variant === 'primary' || variant === 'secondary') && (
        <motion.div
            className="absolute inset-0 bg-linear-to-r from-transparent via-white/20 to-transparent -skew-x-45"
            initial={{ x: '-150%' }}
            whileHover={{ x: '150%' }}
            transition={{ duration: 0.8, ease: "easeInOut" }}
        />
      )}

      {isLoading ? (
        <div className="flex items-center justify-center">
          <svg className="animate-spin h-5 w-5 text-current" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
        </div>
      ) : (
        <div className="flex items-center justify-center gap-3 relative z-10">
          {leftIcon && <span className="flex-shrink-0 transition-transform group-hover:scale-110 group-hover:-rotate-3">{leftIcon}</span>}
          <span className="truncate">{children}</span>
          {rightIcon && <span className="flex-shrink-0 transition-transform group-hover:translate-x-1">{rightIcon}</span>}
        </div>
      )}
    </motion.button>
  );
};

export default Button;
