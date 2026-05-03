import { NextResponse } from 'next/server'
import type { NextRequest } from 'next/server'

/**
 * Auth Middleware
 *
 * Uses the `__Secure-ep-refresh` httpOnly cookie as a proxy for "is logged in".
 * Uses a lightweight `ep-role` cookie (set by FE after login) for admin check.
 *
 * Route protection:
 *   /login, /register     → Public. Redirect away if already logged in.
 *   /dashboard             → Protected. Learner home.
 *   /vocabulary, /grammar, /conversations, /exercises, /review, /study, /curriculum, /ai-chat → Protected.
 *   /admin/*              → Protected + Admin only. Redirect if not admin.
 *   /onboarding           → Protected. Only accessible if logged in.
 */
export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl

  // The __Secure-ep-refresh cookie is scoped to Path=/api/v1/auth by the backend,
  // so the Next.js page router will NEVER receive it in page requests.
  // We use the lightweight `ep-role` cookie (set by FE on login) as an auth indicator.
  const role = request.cookies.get('ep-role')?.value
  const isAuthenticated = !!role
  const isAdmin = role === 'admin'

  // Check onboarding status
  const onboardingComplete = request.cookies.get('ep-onboarding')?.value === '1'

  // ── Public auth pages & Root ───────────────────────────────────────────
  if (pathname === '/' || pathname.startsWith('/login') || pathname.startsWith('/register')) {
    if (isAuthenticated) {
      // Already logged in → redirect based on role
      if (!onboardingComplete) {
        return NextResponse.redirect(new URL('/onboarding', request.url))
      }
      const destination = isAdmin ? '/admin' : '/dashboard'
      return NextResponse.redirect(new URL(destination, request.url))
    }
    // Not logged in → allow access to auth pages, but if it's root, go to login
    if (pathname === '/') {
      return NextResponse.redirect(new URL('/login', request.url))
    }
    return NextResponse.next()
  }

  // ── Protected routes ───────────────────────────────────────────────────
  if (!isAuthenticated) {
    const loginUrl = new URL('/login', request.url)
    loginUrl.searchParams.set('redirect', pathname)
    return NextResponse.redirect(loginUrl)
  }

  // ── Onboarding check ──────────────────────────────────────────────────
  if (!onboardingComplete && pathname !== '/onboarding') {
    return NextResponse.redirect(new URL('/onboarding', request.url))
  }
  if (onboardingComplete && pathname === '/onboarding') {
    const destination = isAdmin ? '/admin' : '/dashboard'
    return NextResponse.redirect(new URL(destination, request.url))
  }

  // ── Admin routes ───────────────────────────────────────────────────────
  if (pathname.startsWith('/admin') && !isAdmin) {
    return NextResponse.redirect(new URL('/dashboard', request.url))
  }

  return NextResponse.next()
}

export const config = {
  matcher: [
    /*
     * Match all routes EXCEPT:
     * - api routes (handled by backend proxy)
     * - _next (Next.js internals)
     * - static files (images, fonts, etc.)
     */
    '/((?!api|_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)',
  ],
}
