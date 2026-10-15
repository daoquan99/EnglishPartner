import React from 'react';

interface ToastIconProps extends React.SVGProps<SVGSVGElement> {
  size?: number;
}

export const SuccessToastIcon: React.FC<ToastIconProps> = ({ size = 28, className, ...props }) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 32 32"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
    className={className}
    {...props}
  >
    <defs>
      <linearGradient id="success-bg-grad" x1="4" y1="4" x2="28" y2="28" gradientUnits="userSpaceOnUse">
        <stop stopColor="#10B981" />
        <stop offset="1" stopColor="#047857" />
      </linearGradient>
      <filter id="success-glow" x="0" y="0" width="32" height="32" filterUnits="userSpaceOnUse">
        <feDropShadow dx="0" dy="2" stdDeviation="2.5" floodColor="#059669" floodOpacity="0.35" />
      </filter>
    </defs>
    <circle cx="16" cy="16" r="13" fill="url(#success-bg-grad)" filter="url(#success-glow)" />
    <circle cx="16" cy="16" r="11.5" stroke="#6EE7B7" strokeWidth="1" strokeOpacity="0.5" />
    <path
      d="M10.5 16.5L14 20L21.5 12"
      stroke="white"
      strokeWidth="2.5"
      strokeLinecap="round"
      strokeLinejoin="round"
    />
    <path
      d="M23.5 7L24 8.5L25.5 9L24 9.5L23.5 11L23 9.5L21.5 9L23 8.5L23.5 7Z"
      fill="#A7F3D0"
      opacity="0.9"
    />
  </svg>
);

export const ErrorToastIcon: React.FC<ToastIconProps> = ({ size = 28, className, ...props }) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 32 32"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
    className={className}
    {...props}
  >
    <defs>
      <linearGradient id="error-bg-grad" x1="4" y1="4" x2="28" y2="28" gradientUnits="userSpaceOnUse">
        <stop stopColor="#F43F5E" />
        <stop offset="1" stopColor="#BE123C" />
      </linearGradient>
      <filter id="error-glow" x="0" y="0" width="32" height="32" filterUnits="userSpaceOnUse">
        <feDropShadow dx="0" dy="2" stdDeviation="2.5" floodColor="#E11D48" floodOpacity="0.35" />
      </filter>
    </defs>
    <rect
      x="3.5"
      y="3.5"
      width="25"
      height="25"
      rx="8"
      fill="url(#error-bg-grad)"
      filter="url(#error-glow)"
    />
    <rect x="4.5" y="4.5" width="23" height="23" rx="7" stroke="#FDA4AF" strokeWidth="1" strokeOpacity="0.4" />
    <path
      d="M11.5 11.5L20.5 20.5M20.5 11.5L11.5 20.5"
      stroke="white"
      strokeWidth="2.5"
      strokeLinecap="round"
    />
  </svg>
);

export const WarningToastIcon: React.FC<ToastIconProps> = ({ size = 28, className, ...props }) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 32 32"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
    className={className}
    {...props}
  >
    <defs>
      <linearGradient id="warning-bg-grad" x1="4" y1="4" x2="28" y2="28" gradientUnits="userSpaceOnUse">
        <stop stopColor="#F59E0B" />
        <stop offset="1" stopColor="#B45309" />
      </linearGradient>
      <filter id="warning-glow" x="0" y="0" width="32" height="32" filterUnits="userSpaceOnUse">
        <feDropShadow dx="0" dy="2" stdDeviation="2.5" floodColor="#D97706" floodOpacity="0.35" />
      </filter>
    </defs>
    <path
      d="M13.6 4.8C14.7 3 17.3 3 18.4 4.8L28.8 21.8C29.9 23.6 28.6 26 26.4 26H5.6C3.4 26 2.1 23.6 3.2 21.8L13.6 4.8Z"
      fill="url(#warning-bg-grad)"
      filter="url(#warning-glow)"
    />
    <path
      d="M16 11V16.5"
      stroke="white"
      strokeWidth="2.5"
      strokeLinecap="round"
    />
    <circle cx="16" cy="20.5" r="1.5" fill="white" />
  </svg>
);

export const InfoToastIcon: React.FC<ToastIconProps> = ({ size = 28, className, ...props }) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 32 32"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
    className={className}
    {...props}
  >
    <defs>
      <linearGradient id="info-bg-grad" x1="4" y1="4" x2="28" y2="28" gradientUnits="userSpaceOnUse">
        <stop stopColor="#0284C7" />
        <stop offset="1" stopColor="#1D4ED8" />
      </linearGradient>
      <filter id="info-glow" x="0" y="0" width="32" height="32" filterUnits="userSpaceOnUse">
        <feDropShadow dx="0" dy="2" stdDeviation="2.5" floodColor="#0284C7" floodOpacity="0.35" />
      </filter>
    </defs>
    <circle cx="16" cy="16" r="13" fill="url(#info-bg-grad)" filter="url(#info-glow)" />
    <circle cx="16" cy="16" r="11.5" stroke="#7DD3FC" strokeWidth="1" strokeOpacity="0.5" />
    <circle cx="16" cy="11.5" r="1.5" fill="white" />
    <path
      d="M14.5 15.5H16V21.5H17.5"
      stroke="white"
      strokeWidth="2.5"
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </svg>
);

export const LoadingToastIcon: React.FC<ToastIconProps> = ({ size = 28, className, ...props }) => (
  <svg
    width={size}
    height={size}
    viewBox="0 0 32 32"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
    className={`animate-spin ${className || ''}`}
    {...props}
  >
    <circle
      cx="16"
      cy="16"
      r="12"
      stroke="#E2E8F0"
      strokeWidth="3"
    />
    <path
      d="M16 4C22.6274 4 28 9.37258 28 16"
      stroke="#10B981"
      strokeWidth="3"
      strokeLinecap="round"
    />
    <circle cx="16" cy="16" r="3.5" fill="#059669" className="animate-pulse" />
  </svg>
);
