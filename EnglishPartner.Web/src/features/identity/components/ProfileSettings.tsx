'use client'

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/ui/tabs'

import { ProfileFormTab } from './ProfileFormTab'
import { SecurityTab } from './SecurityTab'
import { ExternalLoginsTab } from './ExternalLoginsTab'
import { StreakTab } from './StreakTab'

export function ProfileSettings() {
  return (
    <Tabs defaultValue="profile" className="w-full">
      <TabsList className="grid w-full grid-cols-4 max-w-[600px] mb-8 bg-[var(--color-surface-2)] border border-[var(--color-border)] overflow-x-auto h-auto min-h-10">
        <TabsTrigger value="profile" className="data-[state=active]:bg-[var(--color-primary)] data-[state=active]:text-[var(--color-btn-text)] text-[var(--color-text-secondary)]">Profile</TabsTrigger>
        <TabsTrigger value="security" className="data-[state=active]:bg-[var(--color-primary)] data-[state=active]:text-[var(--color-btn-text)] text-[var(--color-text-secondary)]">Security</TabsTrigger>
        <TabsTrigger value="logins" className="data-[state=active]:bg-[var(--color-primary)] data-[state=active]:text-[var(--color-btn-text)] text-[var(--color-text-secondary)]">Linked Accounts</TabsTrigger>
        <TabsTrigger value="streak" className="data-[state=active]:bg-[var(--color-primary)] data-[state=active]:text-[var(--color-btn-text)] text-[var(--color-text-secondary)]">Streak & Stats</TabsTrigger>
      </TabsList>

      <TabsContent value="profile">
        <Card className="border-[var(--color-border)] bg-[var(--color-surface)] shadow-md">
          <CardHeader>
            <CardTitle className="text-xl text-[var(--color-text-primary)]">Profile Settings</CardTitle>
            <CardDescription className="text-[var(--color-text-secondary)]">Update your learning preferences and personal details.</CardDescription>
          </CardHeader>
          <CardContent>
            <ProfileFormTab />
          </CardContent>
        </Card>
      </TabsContent>

      <TabsContent value="security">
        <Card className="border-[var(--color-border)] bg-[var(--color-surface)] shadow-md">
          <CardHeader>
            <CardTitle className="text-xl text-[var(--color-text-primary)]">Security</CardTitle>
            <CardDescription className="text-[var(--color-text-secondary)]">Manage your password.</CardDescription>
          </CardHeader>
          <CardContent>
            <SecurityTab />
          </CardContent>
        </Card>
      </TabsContent>

      <TabsContent value="logins">
        <Card className="border-[var(--color-border)] bg-[var(--color-surface)] shadow-md">
          <CardHeader>
            <CardTitle className="text-xl text-[var(--color-text-primary)]">Linked Accounts</CardTitle>
            <CardDescription className="text-[var(--color-text-secondary)]">Manage your social logins.</CardDescription>
          </CardHeader>
          <CardContent>
            <ExternalLoginsTab />
          </CardContent>
        </Card>
      </TabsContent>

      <TabsContent value="streak">
        <Card className="border-[var(--color-border)] bg-[var(--color-surface)] shadow-md">
          <CardHeader>
            <CardTitle className="text-xl text-[var(--color-text-primary)]">Streak & Stats</CardTitle>
            <CardDescription className="text-[var(--color-text-secondary)]">View your learning progress and check in.</CardDescription>
          </CardHeader>
          <CardContent>
            <StreakTab />
          </CardContent>
        </Card>
      </TabsContent>
    </Tabs>
  )
}
