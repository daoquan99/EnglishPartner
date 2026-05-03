'use client'

import { useMutation, useQuery, useQueryClient, useInfiniteQuery } from '@tanstack/react-query'
import { toast } from 'sonner'
import * as api from './curriculum.api'
import type {
  CreateTopicRequest,
  UpdateTopicRequest,
  CreateVocabularyRequest,
  UpdateVocabularyRequest,
  CreateGrammarRequest,
  UpdateGrammarRequest,
  CreateExerciseRequest,
  UpdateExerciseRequest,
  AddExerciseOptionRequest,
  CreateConversationRequest,
  UpdateConversationRequest,
  AddConversationLineRequest,
  UpdateConversationLineRequest,
  ReorderConversationLinesRequest,
  CreateExampleSentenceRequest,
  UpdateExampleSentenceRequest,
  GenerateVocabularyRequest,
  GenerateGrammarRequest,
  GenerateConversationRequest,
  GenerateExerciseRequest,
  GenerateExampleSentenceRequest,
  CreateLanguageRequest,
  UpdateLanguageRequest,
} from '../types/curriculum.types'

// ── Topics ───────────────────────────────────────────────────────────────────

export const useTopics = (params: { languageCode: string; searchTerm?: string }, options?: { enabled?: boolean }) => {
  return useQuery({
    queryKey: ['curriculum', 'topics', params],
    queryFn: () => api.getTopics(params),
    staleTime: 24 * 60 * 60 * 1000,
    ...options,
  })
}

export const useTopic = (id: string) => {
  return useQuery({
    queryKey: ['curriculum', 'topic', id],
    queryFn: () => api.getTopic(id),
    enabled: !!id,
  })
}

export const useCreateTopic = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.createTopic,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'topics'] })
      toast.success('Topic created successfully')
    },
    onError: () => toast.error('Failed to create topic'),
  })
}

export const useUpdateTopic = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string } & UpdateTopicRequest) => api.updateTopic(id, body),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'topics'] })
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'topic', vars.id] })
      toast.success('Topic updated successfully')
    },
    onError: () => toast.error('Failed to update topic'),
  })
}

export const useDeleteTopic = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.deleteTopic,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'topics'] })
      toast.success('Topic deleted successfully')
    },
    onError: () => toast.error('Failed to delete topic'),
  })
}

// ── Vocabularies ─────────────────────────────────────────────────────────────

export const useVocabularies = (params: {
  languageCode?: string
  page?: number
  pageSize?: number
  searchTerm?: string
  level?: string
  topicId?: string
}) => {
  return useQuery({
    queryKey: ['curriculum', 'vocabularies', params],
    queryFn: () => api.getVocabularies(params),
    staleTime: 60 * 1000,
  })
}

export const useInfiniteVocabularies = (params: {
  languageCode?: string
  pageSize?: number
  searchTerm?: string
  level?: string
  topicId?: string
}) => {
  return useInfiniteQuery({
    queryKey: ['curriculum', 'vocabularies', 'infinite', params],
    queryFn: ({ pageParam = 1 }) => api.getVocabularies({ ...params, page: pageParam }),
    initialPageParam: 1,
    getNextPageParam: (lastPage: any) => {
      if (!lastPage || Array.isArray(lastPage) || !lastPage.pageSize || !lastPage.totalCount) return undefined;
      const { page, pageSize, totalCount } = lastPage
      const totalPages = Math.ceil(totalCount / pageSize)
      return page < totalPages ? page + 1 : undefined
    },
    staleTime: 60 * 1000,
  })
}

export const useVocabulary = (id: string) => {
  return useQuery({
    queryKey: ['curriculum', 'vocabulary', id],
    queryFn: () => api.getVocabulary(id),
    enabled: !!id,
  })
}

export const useCreateVocabulary = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.createVocabulary,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'vocabularies'] })
      toast.success('Vocabulary created successfully')
    },
    onError: () => toast.error('Failed to create vocabulary'),
  })
}

export const useUpdateVocabulary = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string } & UpdateVocabularyRequest) => api.updateVocabulary(id, body),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'vocabularies'] })
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'vocabulary', vars.id] })
      toast.success('Vocabulary updated successfully')
    },
    onError: () => toast.error('Failed to update vocabulary'),
  })
}

export const useDeleteVocabulary = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.deleteVocabulary,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'vocabularies'] })
      toast.success('Vocabulary deleted successfully')
    },
    onError: () => toast.error('Failed to delete vocabulary'),
  })
}

// ── Grammars ─────────────────────────────────────────────────────────────────

export const useGrammars = (params: { languageCode?: string; page?: number; pageSize?: number; searchTerm?: string }) => {
  return useQuery({
    queryKey: ['curriculum', 'grammars', params],
    queryFn: () => api.getGrammars(params),
    staleTime: 60 * 1000,
  })
}

export const useInfiniteGrammars = (params: { languageCode?: string; pageSize?: number; searchTerm?: string }) => {
  return useInfiniteQuery({
    queryKey: ['curriculum', 'grammars', 'infinite', params],
    queryFn: ({ pageParam = 1 }) => api.getGrammars({ ...params, page: pageParam }),
    initialPageParam: 1,
    getNextPageParam: (lastPage: any) => {
      if (!lastPage || Array.isArray(lastPage) || !lastPage.pageSize || !lastPage.totalCount) return undefined;
      const { page, pageSize, totalCount } = lastPage
      const totalPages = Math.ceil(totalCount / pageSize)
      return page < totalPages ? page + 1 : undefined
    },
    staleTime: 60 * 1000,
  })
}

export const useGrammar = (id: string) => {
  return useQuery({
    queryKey: ['curriculum', 'grammar', id],
    queryFn: () => api.getGrammar(id),
    enabled: !!id,
  })
}

export const useCreateGrammar = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.createGrammar,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'grammars'] })
      toast.success('Grammar created successfully')
    },
    onError: () => toast.error('Failed to create grammar'),
  })
}

export const useUpdateGrammar = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string } & UpdateGrammarRequest) => api.updateGrammar(id, body),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'grammars'] })
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'grammar', vars.id] })
      toast.success('Grammar updated successfully')
    },
    onError: () => toast.error('Failed to update grammar'),
  })
}

export const useDeleteGrammar = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.deleteGrammar,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'grammars'] })
      toast.success('Grammar deleted successfully')
    },
    onError: () => toast.error('Failed to delete grammar'),
  })
}

// ── Exercises ────────────────────────────────────────────────────────────────

export const useExercises = (params: {
  languageCode: string
  page?: number
  pageSize?: number
  level?: string
  topicId?: string
  vocabularyId?: string
  grammarId?: string
  exerciseType?: number
}) => {
  return useQuery({
    queryKey: ['curriculum', 'exercises', params],
    queryFn: () => api.getExercises(params),
    staleTime: 6 * 60 * 60 * 1000,
  })
}

export const useExercise = (id: string) => {
  return useQuery({
    queryKey: ['curriculum', 'exercise', id],
    queryFn: () => api.getExercise(id),
    enabled: !!id,
  })
}

export const useSubmitStandaloneExercise = () => {
  return useMutation({
    mutationFn: ({ id, isCorrect }: { id: string; isCorrect: boolean }) => api.submitStandaloneExercise(id, isCorrect),
    onError: () => toast.error('Failed to record exercise progress'),
  })
}

export const useCreateExercise = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.createExercise,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'exercises'] })
      toast.success('Exercise created successfully')
    },
    onError: () => toast.error('Failed to create exercise'),
  })
}

export const useAddExerciseOption = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ exerciseId, ...body }: { exerciseId: string } & AddExerciseOptionRequest) =>
      api.addExerciseOption(exerciseId, body),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'exercise', vars.exerciseId] })
      toast.success('Option added successfully')
    },
    onError: () => toast.error('Failed to add option'),
  })
}

export const useUpdateExercise = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string } & UpdateExerciseRequest) => api.updateExercise(id, body),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'exercises'] })
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'exercise', vars.id] })
      toast.success('Exercise updated successfully')
    },
    onError: () => toast.error('Failed to update exercise'),
  })
}

export const useDeleteExercise = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.deleteExercise,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'exercises'] })
      toast.success('Exercise deleted successfully')
    },
    onError: () => toast.error('Failed to delete exercise'),
  })
}

// ── Conversations ────────────────────────────────────────────────────────────

export const useConversations = (params: { languageCode: string; page?: number; pageSize?: number; topicId?: string; level?: string }) => {
  return useQuery({
    queryKey: ['curriculum', 'conversations', params],
    queryFn: () => api.getConversations(params),
    staleTime: 24 * 60 * 60 * 1000,
  })
}

export const useInfiniteConversations = (params: { languageCode: string; pageSize?: number; topicId?: string; level?: string }) => {
  return useInfiniteQuery({
    queryKey: ['curriculum', 'conversations', 'infinite', params],
    queryFn: ({ pageParam = 1 }) => api.getConversations({ ...params, page: pageParam }),
    initialPageParam: 1,
    getNextPageParam: (lastPage: any) => {
      if (!lastPage || Array.isArray(lastPage) || !lastPage.pageSize || !lastPage.totalCount) return undefined;
      const { page, pageSize, totalCount } = lastPage
      const totalPages = Math.ceil(totalCount / pageSize)
      return page < totalPages ? page + 1 : undefined
    },
    staleTime: 24 * 60 * 60 * 1000,
  })
}

export const useConversation = (id: string) => {
  return useQuery({
    queryKey: ['curriculum', 'conversation', id],
    queryFn: () => api.getConversation(id),
    enabled: !!id,
  })
}

export const useCreateConversation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.createConversation,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'conversations'] })
      toast.success('Conversation created successfully')
    },
    onError: () => toast.error('Failed to create conversation'),
  })
}

export const useAddConversationLine = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ conversationId, ...body }: { conversationId: string } & AddConversationLineRequest) =>
      api.addConversationLine(conversationId, body),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'conversation', vars.conversationId] })
      toast.success('Line added successfully')
    },
    onError: () => toast.error('Failed to add line'),
  })
}

export const useUpdateConversationLine = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ lineId, conversationId, ...body }: { lineId: string; conversationId: string } & UpdateConversationLineRequest) =>
      api.updateConversationLine(conversationId, lineId, body),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'conversation', vars.conversationId] })
      toast.success('Line updated successfully')
    },
    onError: () => toast.error('Failed to update line'),
  })
}

export const useDeleteConversationLine = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ lineId, conversationId }: { lineId: string; conversationId: string }) =>
      api.deleteConversationLine(conversationId, lineId),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'conversation', vars.conversationId] })
      toast.success('Line deleted successfully')
    },
    onError: () => toast.error('Failed to delete line'),
  })
}

export const useReorderConversationLines = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ conversationId, orderedLineIds }: { conversationId: string; orderedLineIds: string[] }) =>
      api.reorderConversationLines(conversationId, { orderedLineIds }),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'conversation', vars.conversationId] })
      toast.success('Lines reordered successfully')
    },
    onError: () => toast.error('Failed to reorder lines'),
  })
}

export const useUpdateConversation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string } & UpdateConversationRequest) => api.updateConversation(id, body),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'conversations'] })
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'conversation', vars.id] })
      toast.success('Conversation updated successfully')
    },
    onError: () => toast.error('Failed to update conversation'),
  })
}

export const useDeleteConversation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.deleteConversation,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'conversations'] })
      toast.success('Conversation deleted successfully')
    },
    onError: () => toast.error('Failed to delete conversation'),
  })
}

// ── Example Sentences ────────────────────────────────────────────────────────

export const useExampleSentences = (params: {
  languageCode?: string
  page?: number
  pageSize?: number
  searchTerm?: string
  vocabularyId?: string
  grammarId?: string
  topicId?: string
}) => {
  return useQuery({
    queryKey: ['curriculum', 'example-sentences', params],
    queryFn: () => api.getExampleSentences(params),
    staleTime: 60 * 60 * 1000,
  })
}

export const useCreateExampleSentence = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.createExampleSentence,
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'example-sentences'] })
      if (vars.vocabularyId) queryClient.invalidateQueries({ queryKey: ['curriculum', 'vocabulary', vars.vocabularyId] })
      if (vars.grammarId) queryClient.invalidateQueries({ queryKey: ['curriculum', 'grammar', vars.grammarId] })
      toast.success('Example sentence created successfully')
    },
    onError: () => toast.error('Failed to create example sentence'),
  })
}

export const useUpdateExampleSentence = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string } & UpdateExampleSentenceRequest) => api.updateExampleSentence(id, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'example-sentences'] })
      // Notice: invalidate grammar/vocab details might be needed if they embed sentences.
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'grammar'] })
      toast.success('Example sentence updated successfully')
    },
    onError: () => toast.error('Failed to update example sentence'),
  })
}

export const useDeleteExampleSentence = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.deleteExampleSentence,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'example-sentences'] })
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'grammar'] })
      toast.success('Example sentence deleted successfully')
    },
    onError: () => toast.error('Failed to delete example sentence'),
  })
}

// ── Languages ────────────────────────────────────────────────────────────────

export const useLanguages = () => {
  return useQuery({
    queryKey: ['curriculum', 'languages'],
    queryFn: api.getLanguages,
    staleTime: 24 * 60 * 60 * 1000,
  })
}

export const useCreateLanguage = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.createLanguage,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'languages'] })
      toast.success('Language created successfully')
    },
    onError: () => toast.error('Failed to create language'),
  })
}

export const useUpdateLanguage = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, ...body }: { id: string } & UpdateLanguageRequest) => api.updateLanguage(id, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'languages'] })
      toast.success('Language updated successfully')
    },
    onError: () => toast.error('Failed to update language'),
  })
}

export const useDeleteLanguage = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.deleteLanguage,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'languages'] })
      toast.success('Language deleted successfully')
    },
    onError: () => toast.error('Failed to delete language'),
  })
}

// ── AI Generate ──────────────────────────────────────────────────────────────

export const useGenerateVocabulary = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.generateVocabulary,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'vocabularies'] })
      toast.success('Vocabularies generated successfully')
    },
    onError: () => toast.error('Failed to generate vocabularies'),
  })
}

export const useGenerateGrammar = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.generateGrammar,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'grammars'] })
      toast.success('Grammar generated successfully')
    },
    onError: () => toast.error('Failed to generate grammar'),
  })
}

export const useGenerateConversation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.generateConversation,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'conversations'] })
      toast.success('Conversations generated successfully')
    },
    onError: () => toast.error('Failed to generate conversations'),
  })
}

export const useGenerateExercise = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.generateExercise,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'exercises'] })
      toast.success('Exercises generated successfully')
    },
    onError: () => toast.error('Failed to generate exercises'),
  })
}

export const useGenerateExampleSentences = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: api.generateExampleSentences,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'example-sentences'] })
      toast.success('Example sentences generated successfully')
    },
    onError: () => toast.error('Failed to generate example sentences'),
  })
}

// ── Admin Audio Generate ─────────────────────────────────────────────────────

export const useGenerateLineAudio = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ conversationId, lineId, voice }: { conversationId: string; lineId: string; voice?: string }) =>
      api.generateLineAudio(conversationId, lineId, voice ? { voice } : undefined),
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'conversation', vars.conversationId] })
      toast.success('Audio generated successfully')
    },
    onError: () => toast.error('Failed to generate audio'),
  })
}

export const useGenerateAllAudio = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ conversationId, voice }: { conversationId: string; voice?: string }) =>
      api.generateAllAudio(conversationId, voice ? { voice } : undefined),
    onSuccess: (result, vars) => {
      queryClient.invalidateQueries({ queryKey: ['curriculum', 'conversation', vars.conversationId] })
      toast.success(`Generated audio for ${result.generatedCount} / ${result.totalLines} lines`)
    },
    onError: () => toast.error('Failed to generate audio'),
  })
}

