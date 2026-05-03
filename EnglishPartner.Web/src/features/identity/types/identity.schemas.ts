import { z } from 'zod'

export const onboardingSchema = z.object({
  nativeLanguage: z.string().min(1, 'Please select your native language'),
  targetLanguage: z.string().min(1, 'Please select a language to learn'),
  proficiencyLevel: z.enum([
    'Beginner',
    'Elementary',
    'Intermediate',
    'UpperIntermediate',
    'Advanced',
    'Mastery',
  ], { required_error: 'Please select your current level' }),
  learningGoal: z.string().min(1, 'Please select a primary goal'),
  dailyGoalMinutes: z.number().min(5).max(120),
  morningReminderHour: z.number().min(0).max(23).optional(),
  eveningReminderHour: z.number().min(0).max(23).optional(),
})

export type OnboardingFormData = z.infer<typeof onboardingSchema>
