import { ProfileSettings } from '@/features/identity/components/ProfileSettings'

export default function SettingsPage() {
  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-[var(--color-text-primary)]">Settings</h1>
        <p className="text-[var(--color-text-secondary)]">Manage your account settings and preferences.</p>
      </div>

      <ProfileSettings />
    </div>
  )
}
