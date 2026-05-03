import { OnboardingWizard } from '@/features/identity/components/OnboardingWizard'

export default function OnboardingPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-[var(--color-background)] p-6">
      <div className="w-full">
        <OnboardingWizard />
      </div>
    </div>
  )
}
