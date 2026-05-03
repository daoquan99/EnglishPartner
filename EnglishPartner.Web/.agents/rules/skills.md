# EnglishPartner Web — Agent Skills

## Skill: Create a New Feature Module

**When:** Adding a brand-new feature (e.g., `gamification`, `leaderboard`)

**Steps:**
1. Create folder: `src/features/<feature-name>/`
2. Create sub-folders: `api/`, `components/`, `hooks/`, `types/`
3. Create `src/features/<feature-name>/index.ts` as public barrel export
4. Define types in `types/<feature>.types.ts` first
5. Create API hooks in `api/use<Feature>.ts` using React Query
6. Build components in `components/`
7. Wire into the appropriate `app/(dashboard)/` page

**Checklist:**
- [ ] Types defined before implementation?
- [ ] API hook wraps axios call, not raw axios in component?
- [ ] Query key uses factory pattern?
- [ ] Barrel export added to `index.ts`?

---

## Skill: Add a New API Integration

**When:** Connecting to a new backend endpoint

**Steps:**
1. Define the response type in `features/<feature>/types/`
2. Add the API function in `features/<feature>/api/<feature>.api.ts`:
   ```ts
   export const getXxx = async (): Promise<XxxResponse> => {
     const { data } = await apiClient.get<XxxResponse>('/api/v1/...')
     return data
   }
   ```
3. Create a React Query hook in `features/<feature>/api/useXxx.ts`:
   ```ts
   export const useXxx = () => useQuery({
     queryKey: xxxKeys.xxx(),
     queryFn: getXxx,
   })
   ```
4. Use the hook in the component — never the raw API function directly

**Notes:**
- Mutations use `useMutation` + `onSuccess` → `queryClient.invalidateQueries()`
- Always handle loading and error states in the component

---

## Skill: Add a New Page

**When:** Creating a new route

**Steps:**
1. Determine if route is public `(auth)` or protected `(dashboard)`
2. Create file: `src/app/(group)/path/page.tsx`
3. Page component is a **Server Component** by default (no `'use client'`)
4. If data needed at page level → use React Query's `prefetchQuery` with Suspense
5. If page needs client interactivity → extract client parts into a separate `*Client.tsx` component
6. Add layout if needed: `src/app/(group)/path/layout.tsx`

---

## Skill: Add a shadcn Component

**When:** Need a new UI primitive (button, dialog, etc.)

**Steps:**
1. Run: `pnpm dlx shadcn@latest add <component-name>`
2. Component auto-generates in `src/ui/`
3. Import in your feature component: `import { Button } from '@/ui/button'`
4. **Never edit files in `src/ui/` directly** — extend via `className` prop or wrapper components

---

## Skill: Add a Zustand Store

**When:** Need new client-side global state

**Steps:**
1. Create file: `src/lib/zustand/<name>.store.ts`
2. Follow this pattern:
   ```ts
   interface XxxState {
     // state fields
   }
   interface XxxActions {
     // action methods
   }
   export const useXxxStore = create<XxxState & XxxActions>((set) => ({
     // initial state + actions
   }))
   ```
3. Import via: `import { useXxxStore } from '@/lib/zustand/<name>.store'`
4. **Rule:** Never put server-fetched data in Zustand — only client/UI state

---

## Skill: Add a Protected Route

**When:** New page should require authentication

**Steps:**
1. Place page inside `src/app/(dashboard)/`
2. Middleware (`src/middleware.ts`) already protects all `(dashboard)` routes
3. If more granular control needed (e.g., onboarding-completed check), add logic in middleware
4. For role-based protection: check user role from Zustand auth store in the layout

---

## Skill: Connect to SignalR

**When:** Component needs to receive realtime events

**Steps:**
1. Ensure `src/lib/signalr.ts` hub is connected (initialized by root layout/provider)
2. Create hook in `features/notifications/hooks/useXxxNotification.ts`:
   ```ts
   export const useXxxNotification = (callback: (data: XxxEvent) => void) => {
     useEffect(() => {
       hubConnection.on('EventName', callback)
       return () => hubConnection.off('EventName', callback)
     }, [callback])
   }
   ```
3. Use the hook in the component
4. Always cleanup listener on unmount (return in useEffect)

---

## Skill: Add a Form with Validation

**When:** Need a user input form

**Steps:**
1. Define Zod schema:
   ```ts
   const schema = z.object({ email: z.string().email(), ... })
   type FormData = z.infer<typeof schema>
   ```
2. Use `useForm` with zodResolver:
   ```ts
   const form = useForm<FormData>({ resolver: zodResolver(schema) })
   ```
3. Pair with `useMutation` for submission
4. Show errors via `form.formState.errors`
5. Use shadcn `Form`, `FormField`, `FormMessage` primitives

---

## Skill: Handle Auth Token Refresh

**Already implemented** in `src/lib/axios.ts` interceptor. No manual handling needed.

**How it works:**
1. Request interceptor → attaches `Authorization: Bearer <token>` from Zustand
2. Response interceptor → catches 401 → calls `POST /auth/refresh` → retries original request
3. If refresh fails → clears auth store → redirects to `/login`

**When adding a new API call:** Nothing extra needed — interceptor handles it automatically.

---

## Skill: Add X-Timezone-Offset Header

**Already handled globally** in `src/lib/axios.ts` request interceptor.

```ts
// Auto-computed on every request — no manual action needed
config.headers['X-Timezone-Offset'] = String(new Date().getTimezoneOffset() / -60)
```

No action needed per-feature. Just use the shared `apiClient`.

---

## Skill: Create a React Query Mutation with Cache Invalidation

**Pattern:**
```ts
export const useUpdatePreferences = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: updatePreferences,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: profileKeys.me() })
    },
  })
}
```

**Rule:** Always invalidate affected queries on success — never manually update cache unless performance demands it.
