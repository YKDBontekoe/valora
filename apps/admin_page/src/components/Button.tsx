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
    primary: 'bg-primary-600 text-white hover:bg-primary-700 shadow-premium-lg shadow-primary-200/30 hover:shadow-primary-400/40',
    secondary: 'bg-brand-900 text-white hover:bg-brand-800 shadow-premium-lg shadow-brand-200/30 hover:shadow-brand-400/40',
    outline: 'bg-transparent border-2 border-brand-200 text-brand-700 hover:bg-brand-50 hover:border-brand-300 hover:text-brand-900',
    ghost: 'bg-transparent text-brand-500 hover:bg-brand-50 hover:text-brand-900',
    danger: 'bg-error-50 text-error-700 border border-error-100 hover:bg-error-100 hover:text-error-800 hover:shadow-error-100/50'
  };

  const sizes = {
    sm: 'px-4 py-2 text-[10px] uppercase tracking-widest rounded-lg',
    md: 'px-6 py-3.5 text-sm rounded-xl',
    lg: 'px-8 py-5 text-base rounded-2xl'
  };

  return (
    <motion.button
      whileTap={{ scale: 0.97, y: 0 }}
      whileHover={{
        y: -3,
        transition: { type: 'spring', stiffness: 400, damping: 25 }
      }}
      disabled={disabled || isLoading}
      className={`${baseStyles} ${variants[variant]} ${sizes[size]} ${className}`}
      {...props}
    >
      {/* Subtle Shine/Glow effect on hover for primary/secondary */}
      {(variant === 'primary' || variant === 'secondary') && (
        <motion.div
            className="absolute inset-0 bg-linear-to-tr from-white/0 via-white/10 to-white/0"
            initial={{ x: '-100%', skewX: -45 }}
            whileHover={{ x: '100%' }}
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
        <div className="flex items-center justify-center gap-2 relative z-10">
          {leftIcon && <span className="flex-shrink-0 transition-transform group-hover:scale-110">{leftIcon}</span>}
          <span className="truncate">{children}</span>
          {rightIcon && <span className="flex-shrink-0 transition-transform group-hover:translate-x-0.5">{rightIcon}</span>}
        </div>
      )}
    </motion.button>
  );
};

export default Button;
