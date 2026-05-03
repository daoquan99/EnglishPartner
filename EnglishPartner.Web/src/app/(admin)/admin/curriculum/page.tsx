import Link from 'next/link'
import { BookOpen, Languages, MessageSquare, PenTool, TextSelect } from 'lucide-react'
import { Card, CardDescription, CardHeader, CardTitle } from '@/ui/card'
import { RequirePermission } from '@/shared/components/RequirePermission'

export default function CurriculumPage() {
  return (
    <RequirePermission permission={[
      'curriculum.topics.write', 
      'curriculum.vocabulary.write',
      'curriculum.grammar.write',
      'curriculum.exercise.write',
      'curriculum.conversation.write'
    ]}>
      <div className="space-y-8">
        <div>
          <h1 className="text-3xl font-bold text-[var(--color-text-primary)]">Curriculum Management</h1>
          <p className="mt-1 text-[var(--color-text-secondary)]">
            Manage topics, vocabularies, grammars, exercises, and conversations.
          </p>
        </div>

        <div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <Link href="/admin/curriculum/topics">
              <Card className="border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-2)] transition-colors cursor-pointer group h-full">
                <CardHeader>
                  <div className="flex items-center gap-3">
                    <div className="p-2 rounded-lg bg-[var(--color-primary-dim)] text-[var(--color-primary)] group-hover:bg-[var(--color-primary)] group-hover:text-white transition-colors">
                      <BookOpen className="h-5 w-5" />
                    </div>
                    <div>
                      <CardTitle className="text-[var(--color-text-primary)] text-base">Topics</CardTitle>
                      <CardDescription className="text-[var(--color-text-secondary)] mt-1 line-clamp-2">
                        Manage main learning topics and themes.
                      </CardDescription>
                    </div>
                  </div>
                </CardHeader>
              </Card>
            </Link>

            <Link href="/admin/curriculum/vocabularies">
              <Card className="border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-2)] transition-colors cursor-pointer group h-full">
                <CardHeader>
                  <div className="flex items-center gap-3">
                    <div className="p-2 rounded-lg bg-[var(--color-primary-dim)] text-[var(--color-primary)] group-hover:bg-[var(--color-primary)] group-hover:text-white transition-colors">
                      <Languages className="h-5 w-5" />
                    </div>
                    <div>
                      <CardTitle className="text-[var(--color-text-primary)] text-base">Vocabularies</CardTitle>
                      <CardDescription className="text-[var(--color-text-secondary)] mt-1 line-clamp-2">
                        Manage dictionary words, definitions, and pronunciations.
                      </CardDescription>
                    </div>
                  </div>
                </CardHeader>
              </Card>
            </Link>

            <Link href="/admin/curriculum/grammars">
              <Card className="border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-2)] transition-colors cursor-pointer group h-full">
                <CardHeader>
                  <div className="flex items-center gap-3">
                    <div className="p-2 rounded-lg bg-[var(--color-primary-dim)] text-[var(--color-primary)] group-hover:bg-[var(--color-primary)] group-hover:text-white transition-colors">
                      <PenTool className="h-5 w-5" />
                    </div>
                    <div>
                      <CardTitle className="text-[var(--color-text-primary)] text-base">Grammars</CardTitle>
                      <CardDescription className="text-[var(--color-text-secondary)] mt-1 line-clamp-2">
                        Manage grammar rules, formulas, and explanations.
                      </CardDescription>
                    </div>
                  </div>
                </CardHeader>
              </Card>
            </Link>

            <Link href="/admin/curriculum/conversations">
              <Card className="border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-2)] transition-colors cursor-pointer group h-full">
                <CardHeader>
                  <div className="flex items-center gap-3">
                    <div className="p-2 rounded-lg bg-[var(--color-primary-dim)] text-[var(--color-primary)] group-hover:bg-[var(--color-primary)] group-hover:text-white transition-colors">
                      <MessageSquare className="h-5 w-5" />
                    </div>
                    <div>
                      <CardTitle className="text-[var(--color-text-primary)] text-base">Conversations</CardTitle>
                      <CardDescription className="text-[var(--color-text-secondary)] mt-1 line-clamp-2">
                        Manage dialogues, situational roleplays, and scripts.
                      </CardDescription>
                    </div>
                  </div>
                </CardHeader>
              </Card>
            </Link>

            <Link href="/admin/curriculum/exercises">
              <Card className="border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-2)] transition-colors cursor-pointer group h-full">
                <CardHeader>
                  <div className="flex items-center gap-3">
                    <div className="p-2 rounded-lg bg-[var(--color-primary-dim)] text-[var(--color-primary)] group-hover:bg-[var(--color-primary)] group-hover:text-white transition-colors">
                      <TextSelect className="h-5 w-5" />
                    </div>
                    <div>
                      <CardTitle className="text-[var(--color-text-primary)] text-base">Exercises</CardTitle>
                      <CardDescription className="text-[var(--color-text-secondary)] mt-1 line-clamp-2">
                        Manage quizzes, tests, and interactive learning modules.
                      </CardDescription>
                    </div>
                  </div>
                </CardHeader>
              </Card>
            </Link>
          </div>
        </div>
      </div>
    </RequirePermission>
  )
}
