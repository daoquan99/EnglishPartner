import { LearnerNavbar } from '@/shared/components/LearnerNavbar'
import { FloatingChatbot } from '@/features/ai/components/chat/FloatingChatbot'
import { FloatingLiveAvatar } from '@/features/ai/components/live/FloatingLiveAvatar'

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-[var(--color-background)] transition-colors duration-300 flex flex-col">
      <LearnerNavbar />
      <main className="flex-1 mx-auto w-full max-w-7xl px-4 py-8">
        {children}
      </main>
      <FloatingChatbot />
      <FloatingLiveAvatar />
    </div>
  )
}
