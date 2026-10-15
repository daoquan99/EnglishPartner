import React, { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Sparkles, Search, Check, Tag, X } from 'lucide-react';
import type { TopicDto } from '@/shared/api/openapi.types';
import { cn } from '@/shared/lib/cn';

interface TopicCreatablePickerProps {
  label: string;
  value: string;
  onChange: (newTopic: string) => void;
  topics: TopicDto[];
  placeholder?: string;
  disabled?: boolean;
  activeModeCode?: string;
  helperText?: string;
}

/**
 * Reusable Topic Selector with Database Suggestions + Free Custom User Input.
 * Used for both:
 * 1. Chủ đề cuộc trò chuyện (Live Tutor Topic)
 * 2. Chủ đề tạo thêm từ (AI Vocabulary Generator Topic)
 */
export const TopicCreatablePicker: React.FC<TopicCreatablePickerProps> = ({
  label,
  value,
  onChange,
  topics,
  placeholder,
  disabled = false,
  activeModeCode,
  helperText,
}) => {
  const { t } = useTranslation();
  const effectivePlaceholder = placeholder ?? t('common.topicPlaceholder');
  const [selectedCategory, setSelectedCategory] = useState<string>('ALL');
  const [showAllSuggestions, setShowAllSuggestions] = useState(false);

  const categories = useMemo(() => {
    const set = new Set<string>();
    topics.forEach((t) => {
      if (t.category) set.add(t.category);
    });
    return ['ALL', ...Array.from(set)];
  }, [topics]);

  const sortedTopics = useMemo(() => {
    return [...topics]
      .filter((t) => selectedCategory === 'ALL' || t.category === selectedCategory)
      .sort((a, b) => {
        const aMatchesMode = activeModeCode && a.modeCode === activeModeCode ? 1 : 0;
        const bMatchesMode = activeModeCode && b.modeCode === activeModeCode ? 1 : 0;
        if (aMatchesMode !== bMatchesMode) return bMatchesMode - aMatchesMode;
        if (a.isFeatured !== b.isFeatured) return a.isFeatured ? -1 : 1;
        return a.displayOrder - b.displayOrder;
      });
  }, [topics, selectedCategory, activeModeCode]);

  const visibleTopics = showAllSuggestions ? sortedTopics : sortedTopics.slice(0, 6);

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between gap-2">
        <label className="text-xs font-bold uppercase tracking-wider text-slate-600 flex items-center gap-1.5">
          <Tag className="w-3.5 h-3.5 text-emerald-600" />
          {label}
        </label>
        <span className="text-[11px] font-medium text-emerald-700 bg-emerald-50 border border-emerald-200 px-2 py-0.5 rounded-full">
          {t('common.dbSuggestions')} ({topics.length}) + {t('common.customInput')}
        </span>
      </div>

      {/* Custom Input Box */}
      <div className="relative">
        <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
        <input
          type="text"
          value={value}
          disabled={disabled}
          onChange={(e) => onChange(e.target.value)}
          placeholder={effectivePlaceholder}
          className="w-full pl-9 pr-9 py-2.5 text-sm rounded-xl border border-slate-200 bg-white text-slate-800 placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500/30 focus:border-emerald-500 disabled:bg-slate-100"
        />
        {value && !disabled && (
          <button
            type="button"
            onClick={() => onChange('')}
            title={t('common.deleteTopic')}
            className="absolute right-2.5 top-1/2 -translate-y-1/2 p-1 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100"
          >
            <X className="w-3.5 h-3.5" />
          </button>
        )}
      </div>

      {helperText && <p className="text-[11px] text-slate-500">{helperText}</p>}

      {/* Category Filter Pills */}
      {categories.length > 2 && (
        <div className="flex items-center gap-1 overflow-x-auto pb-1">
          {categories.map((cat) => (
            <button
              key={cat}
              type="button"
              disabled={disabled}
              onClick={() => setSelectedCategory(cat)}
              className={cn(
                'px-2.5 py-1 rounded-lg text-[11px] font-semibold whitespace-nowrap transition-colors cursor-pointer',
                selectedCategory === cat
                  ? 'bg-emerald-600 text-white shadow-xs'
                  : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
              )}
            >
              {cat === 'ALL' ? t('common.allTopics') : cat}
            </button>
          ))}
        </div>
      )}

      {/* Suggested Topic Chips from Database */}
      <div className="grid grid-cols-1 gap-1.5 max-h-52 overflow-y-auto pr-1">
        {visibleTopics.map((topic) => {
          const isSelected = value.trim().toLowerCase() === topic.name.trim().toLowerCase();
          const isRecommendedForMode = Boolean(activeModeCode && topic.modeCode === activeModeCode);

          return (
            <button
              key={topic.id}
              type="button"
              disabled={disabled}
              onClick={() => onChange(topic.name)}
              className={cn(
                'text-left px-3 py-2 rounded-xl border transition-all flex items-start justify-between gap-2 cursor-pointer',
                isSelected
                  ? 'bg-emerald-50/90 border-emerald-500 ring-1 ring-emerald-500/30'
                  : 'bg-white hover:bg-slate-50 border-slate-200/90'
              )}
            >
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-1.5 flex-wrap">
                  <span className="text-xs font-bold text-slate-800 truncate">{topic.name}</span>
                  <span className="text-[10px] font-bold px-1.5 py-0.2 rounded bg-slate-100 text-slate-600">
                    {topic.suggestedCefrLevel}
                  </span>
                  {isRecommendedForMode && (
                    <span className="inline-flex items-center gap-0.5 text-[10px] font-bold px-1.5 py-0.2 rounded bg-amber-100 text-amber-800">
                      <Sparkles className="w-2.5 h-2.5" /> {t('common.recommendedForMode')}
                    </span>
                  )}
                </div>
                <p className="text-[11px] text-slate-500 truncate mt-0.5">{topic.displayNameVi}</p>
              </div>
              {isSelected && <Check className="w-4 h-4 text-emerald-600 shrink-0 mt-0.5" />}
            </button>
          );
        })}
      </div>

      {sortedTopics.length > 6 && (
        <button
          type="button"
          onClick={() => setShowAllSuggestions((prev) => !prev)}
          className="text-xs font-semibold text-emerald-700 hover:text-emerald-800 cursor-pointer"
        >
          {showAllSuggestions
            ? t('common.collapseSuggestions')
            : t('common.viewMoreSuggestions', { count: sortedTopics.length - 6 })}
        </button>
      )}
    </div>
  );
};
