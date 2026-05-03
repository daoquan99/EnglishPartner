export const EXERCISE_TYPES = [
  // ── Core types
  { value: 0, label: 'Multiple Choice' },
  { value: 1, label: 'Fill In The Blank' },
  { value: 2, label: 'True / False' },
  { value: 3, label: 'Matching' },
  { value: 4, label: 'Translation' },
  { value: 5, label: 'Sentence Completion' },
  { value: 6, label: 'Word Form' },
  { value: 7, label: 'Verb Tense' },
  { value: 8, label: 'True / False / Not Given' },
  { value: 9, label: 'Paraphrasing' },
  { value: 10, label: 'Reordering Words' },
  { value: 11, label: 'Rewriting Sentences' },
  { value: 12, label: 'Dialogue Completion' },
  { value: 13, label: 'Short Answer Questions' },
  { value: 14, label: 'Question Formation' },
  
  // ── Vocabulary-focused
  { value: 15, label: 'Synonym / Antonym' },
  { value: 16, label: 'Collocation' },
  { value: 17, label: 'Odd One Out' },
  { value: 18, label: 'Spelling Correction' },
  { value: 19, label: 'Word Stress' },

  // ── Advanced / multi-answer
  { value: 20, label: 'Multiple Response' },
  { value: 21, label: 'Sentence Correction' },
  { value: 22, label: 'Categorization' },

  // ── Media-based
  { value: 23, label: 'Listening Comprehension' },
  { value: 24, label: 'Listening Gap Fill' },
  { value: 25, label: 'Speaking' },
  { value: 26, label: 'Reading Comprehension' },
  { value: 27, label: 'Writing' },
  { value: 28, label: 'Dictation' },
  { value: 29, label: 'Picture Description' },
  { value: 30, label: 'Topic Response' },
]

export const getExerciseTypeLabel = (valueOrName: number | string) => {
  if (typeof valueOrName === 'string') {
    // Attempt to map string name (e.g. "MultipleChoice") back to human readable
    const match = EXERCISE_TYPES.find(t => t.label.replace(/\s+/g, '').replace(/\//g, '') === valueOrName)
    if (match) return match.label
    // Fallback: Add spaces before capitals
    return valueOrName.replace(/([A-Z])/g, ' $1').trim()
  }
  const match = EXERCISE_TYPES.find(t => t.value === valueOrName)
  return match ? match.label : 'Unknown'
}
