'use client'

import { useMemo } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Loader2, Save } from 'lucide-react'
import { z } from 'zod'

import { useCurrentUser, useUpdateProfile, useOnboardingQuestions } from '../api/useIdentity'
import { Button } from '@/ui/button'
import { Input } from '@/ui/input'
import { Label } from '@/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/ui/select'

const profileSchema = z.object({
  displayName: z.string().min(1, 'Name is required'),
  nativeLanguage: z.string().optional(),
  targetLanguage: z.string().optional(),
  proficiencyLevel: z.string().optional(),
  learningGoal: z.string().optional(),
  dailyGoalMinutes: z.number().min(5).max(120).optional(),
  morningReminderHour: z.number().min(0).max(23).optional().nullable(),
  eveningReminderHour: z.number().min(0).max(23).optional().nullable(),
})

type ProfileFormData = z.infer<typeof profileSchema>

// Fallback options in case the API fails or doesn't return them
const FALLBACK_OPTIONS: Record<string, string[]> = {
  native_language: ['vi', 'en', 'ja', 'ko', 'zh', 'other'],
  target_language: ['en', 'ja', 'ko', 'zh', 'other'],
  proficiency_level: ['Beginner', 'Elementary', 'Intermediate', 'UpperIntermediate', 'Advanced', 'Mastery'],
  learning_goal: ['Communication', 'Academic', 'Business', 'Travel', 'ExamPrep', 'Other'],
  daily_goal: ['5', '10', '15', '30', '60'],
  morning_reminder_hour: ['none', '6', '7', '8', '9', '10', '11', '12'],
  evening_reminder_hour: ['none', '18', '19', '20', '21', '22', '23', '24'],
}

export function ProfileFormTab() {
  const { data: user, isLoading: isLoadingUser } = useCurrentUser()
  const { data: questions, isLoading: isLoadingQuestions } = useOnboardingQuestions()

  // Wait for BOTH user profile and questions to load completely
  if (isLoadingUser || isLoadingQuestions || !user) {
    return (
      <div className="py-8 flex justify-center">
        <Loader2 className="h-6 w-6 animate-spin text-[var(--color-primary)]" />
      </div>
    )
  }

  // Render form only when data is guaranteed to be available
  return <ProfileFormContent user={user} questions={questions ?? []} />
}

function ProfileFormContent({
  user,
  questions,
}: {
  user: NonNullable<ReturnType<typeof useCurrentUser>['data']>
  questions: NonNullable<ReturnType<typeof useOnboardingQuestions>['data']>
}) {
  const { mutate: updateProfile, isPending } = useUpdateProfile()

  // Extract options from questions API by key
  const optionsMap = useMemo(() => {
    const map: Record<string, string[]> = {}
    for (const q of questions) {
      map[q.key] = q.options
    }
    return map
  }, [questions])

  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<ProfileFormData>({
    resolver: zodResolver(profileSchema),
    // Initialize defaultValues EXACTLY with the loaded user data.
    // This avoids ANY async React Hook Form reset issues with Radix Select.
    defaultValues: {
      displayName: user.displayName,
      nativeLanguage: user.nativeLanguage || 'vi',
      targetLanguage: user.targetLanguage || 'en',
      proficiencyLevel: user.proficiencyLevel || 'Beginner',
      learningGoal: user.learningGoal || 'Communication',
      dailyGoalMinutes: user.dailyGoalMinutes || 15,
      morningReminderHour: user.morningReminderHour ?? null,
      eveningReminderHour: user.eveningReminderHour ?? null,
    },
  })

  const onSubmit = (data: ProfileFormData) => {
    updateProfile({
      ...data,
      utcOffsetHours: -(new Date().getTimezoneOffset() / 60),
    })
  }

  /** Renders a Select field using options from the questions API */
  const renderSelect = (
    name: keyof ProfileFormData,
    label: string,
    questionKey: string,
    formatLabel?: (val: string) => string
  ) => {
    const options = optionsMap[questionKey] ?? FALLBACK_OPTIONS[questionKey] ?? []
    return (
      <div className="space-y-3">
        <Label className="text-[var(--color-text-primary)]">{label}</Label>
        <Controller
          name={name}
          control={control}
          render={({ field }) => (
            <Select onValueChange={field.onChange} defaultValue={field.value?.toString()} value={field.value?.toString()}>
              <SelectTrigger className="w-full bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)]">
                <SelectValue placeholder={`Select ${label.toLowerCase()}`} />
              </SelectTrigger>
              <SelectContent className="w-[var(--radix-select-trigger-width)] bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)]">
                {options.map((opt) => (
                  <SelectItem key={opt} value={opt}>
                    {formatLabel ? formatLabel(opt) : opt}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        />
      </div>
    )
  }

  /** Renders a reminder hour Select with "none" → null mapping */
  const renderReminderSelect = (
    name: 'morningReminderHour' | 'eveningReminderHour',
    label: string,
    questionKey: string,
  ) => {
    const options = optionsMap[questionKey] ?? FALLBACK_OPTIONS[questionKey] ?? []
    return (
      <div className="space-y-3">
        <Label className="text-[var(--color-text-primary)]">{label}</Label>
        <Controller
          name={name}
          control={control}
          render={({ field }) => (
            <Select
              onValueChange={(val) => field.onChange(val === 'none' ? null : parseInt(val))}
              defaultValue={field.value?.toString() ?? 'none'}
              value={field.value?.toString() ?? 'none'}
            >
              <SelectTrigger className="w-full bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)]">
                <SelectValue placeholder="Disabled" />
              </SelectTrigger>
              <SelectContent className="w-[var(--radix-select-trigger-width)] bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)]">
                {options.map((opt) => (
                  <SelectItem key={opt} value={opt}>
                    {opt === 'none'
                      ? 'Disabled'
                      : `${parseInt(opt) <= 12 ? opt : parseInt(opt) - 12}:00 ${parseInt(opt) < 12 ? 'AM' : 'PM'}`}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        />
      </div>
    )
  }

  return (
    <form id="profile-form" onSubmit={handleSubmit(onSubmit)} className="space-y-6 max-w-2xl">
      {/* Display Name */}
      <div className="space-y-3">
        <Label className="text-[var(--color-text-primary)]">Display Name</Label>
        <Controller
          name="displayName"
          control={control}
          render={({ field }) => (
            <Input
              {...field}
              className="bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)]"
            />
          )}
        />
        {errors.displayName && (
          <p className="text-sm text-[var(--color-destructive)]">{errors.displayName.message}</p>
        )}
      </div>

      {/* Languages */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {renderSelect('nativeLanguage', 'Native Language', 'native_language')}
        {renderSelect('targetLanguage', 'Target Language', 'target_language')}
      </div>

      {/* Level & Goal */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {renderSelect('proficiencyLevel', 'Proficiency Level', 'proficiency_level')}
        {renderSelect('learningGoal', 'Learning Goal', 'learning_goal')}
      </div>

      {/* Daily Goal */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="space-y-3">
          <Label className="text-[var(--color-text-primary)]">Daily Goal (Minutes)</Label>
          <Controller
            name="dailyGoalMinutes"
            control={control}
            render={({ field }) => (
              <Select
                onValueChange={(val) => field.onChange(parseInt(val))}
                defaultValue={field.value?.toString()}
                value={field.value?.toString()}
              >
                <SelectTrigger className="w-full bg-[var(--color-input-bg)] border-[var(--color-border)] text-[var(--color-text-primary)]">
                  <SelectValue placeholder="Select daily goal" />
                </SelectTrigger>
                <SelectContent className="w-[var(--radix-select-trigger-width)] bg-[var(--color-surface)] border-[var(--color-border)] text-[var(--color-text-primary)]">
                  {(optionsMap['daily_goal'] ?? FALLBACK_OPTIONS['daily_goal'] ?? []).map((opt) => (
                    <SelectItem key={opt} value={opt}>
                      {opt} minutes
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
        </div>
        <div /> {/* spacer */}
      </div>

      {/* Reminders */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {renderReminderSelect('morningReminderHour', 'Morning Reminder', 'morning_reminder_hour')}
        {renderReminderSelect('eveningReminderHour', 'Evening Reminder', 'evening_reminder_hour')}
      </div>

      <div className="pt-4">
        <Button
          type="submit"
          disabled={isPending}
          className="bg-[var(--color-primary)] hover:bg-[var(--color-primary-hover)] text-[var(--color-btn-text)]"
        >
          {isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Save className="mr-2 h-4 w-4" />}
          Save Profile
        </Button>
      </div>
    </form>
  )
}
