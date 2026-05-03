import { Metadata } from 'next'
import { LanguageManagement } from '@/features/curriculum/components/LanguageManagement'
import { Globe } from 'lucide-react'
import { RequirePermission } from '@/shared/components/RequirePermission'

export const metadata: Metadata = {
  title: 'Languages | English Partner',
  description: 'Manage supported languages for the curriculum',
}

export default function LanguagesPage() {
  return (
    <RequirePermission permission="curriculum.topics.write">
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-[var(--color-text-primary)] flex items-center gap-3">
            <Globe className="h-8 w-8 text-[var(--color-primary)]" />
            Languages
          </h1>
          <p className="mt-1 text-[var(--color-text-secondary)]">
            Manage supported languages for the system.
          </p>
        </div>

        <LanguageManagement />
      </div>
    </RequirePermission>
  )
}
