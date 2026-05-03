import { ThemeToggle } from '@/shared/components/ThemeToggle'

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[var(--color-background)] p-6 transition-colors duration-300">
      {/* Theme toggle */}
      <div className="fixed right-5 top-5 z-10">
        <ThemeToggle />
      </div>

      {/* Ambient glows */}
      <div className="pointer-events-none fixed -left-[8%] -top-[15%] h-[550px] w-[550px] rounded-full bg-[radial-gradient(circle,rgba(0,212,176,0.12)_0%,transparent_70%)]" />
      <div className="pointer-events-none fixed -bottom-[20%] -right-[8%] h-[480px] w-[480px] rounded-full bg-[radial-gradient(circle,rgba(245,158,11,0.06)_0%,transparent_70%)]" />
      <div className="pointer-events-none fixed left-1/2 top-1/2 h-[700px] w-[700px] -translate-x-1/2 -translate-y-1/2 rounded-full bg-[radial-gradient(circle,rgba(96,165,250,0.04)_0%,transparent_65%)]" />

      {/* Content */}
      <div className="relative z-[1] w-full max-w-[440px]">
        {children}
      </div>
    </div>
  )
}
