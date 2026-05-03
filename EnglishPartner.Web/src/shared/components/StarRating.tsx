import { Star } from 'lucide-react'

interface StarRatingProps {
  score: number  // 0-5
  maxStars?: number
  className?: string
}

export function StarRating({ score, maxStars = 5, className }: StarRatingProps) {
  return (
    <div className={`flex items-center gap-0.5 ${className ?? ''}`}>
      {Array.from({ length: maxStars }, (_, i) => (
        <Star
          key={i}
          className={`h-4 w-4 ${
            i < score
              ? 'text-amber-400 fill-amber-400'
              : 'text-[var(--color-text-muted)] opacity-30'
          }`}
        />
      ))}
    </div>
  )
}
