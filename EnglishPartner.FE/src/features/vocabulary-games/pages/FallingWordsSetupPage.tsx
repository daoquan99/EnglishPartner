import React, { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  useCreateFallingWordSession,
  useFallingWordPool,
} from '../hooks/useFallingWords';
import type {
  FallingWordGameSource,
  FallingWordPlayFilter,
} from '../types/fallingWords.types';

const CEFR_LEVELS = [
  { label: 'Tất cả trình độ', value: null },
  { label: 'A1', value: 'A1' },
  { label: 'A2', value: 'A2' },
  { label: 'B1', value: 'B1' },
  { label: 'B2', value: 'B2' },
  { label: 'C1', value: 'C1' },
  { label: 'C2', value: 'C2' },
];

const PLAY_FILTERS: { label: string; value: FallingWordPlayFilter; desc: string }[] = [
  { label: 'Tất cả', value: 'All', desc: 'Toàn bộ từ trong nguồn được chọn' },
  { label: 'Chưa chơi', value: 'Unplayed', desc: 'Từ chưa từng có kết quả chấm' },
  { label: 'Đã chơi', value: 'Played', desc: 'Từ đã từng chơi đúng hoặc sai' },
  { label: 'Chơi đúng', value: 'Correct', desc: 'Từ có kết quả chơi gần nhất là đúng' },
  { label: 'Chơi sai / Hết giờ', value: 'Failed', desc: 'Từ có kết quả chơi gần nhất là sai hoặc hết giờ' },
];

export const FallingWordsSetupPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const initialSource: FallingWordGameSource =
    searchParams.get('source') === 'DailyVocabulary'
      ? 'DailyVocabulary'
      : 'VocabularyCatalog';

  const [source, setSource] = useState<FallingWordGameSource>(initialSource);
  const [cefrLevel, setCefrLevel] = useState<string | null>(null);
  const [playFilter, setPlayFilter] = useState<FallingWordPlayFilter>('All');
  const [timeLimitSeconds, setTimeLimitSeconds] = useState<number>(5);
  const [requestedCount, setRequestedCount] = useState<number>(10);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const { data: poolData, isLoading: isLoadingPool, error: poolError } = useFallingWordPool({
    source,
    cefrLevel,
    playFilter,
  });

  const createSessionMutation = useCreateFallingWordSession();

  const handleStartGame = async () => {
    setErrorMessage(null);
    try {
      const session = await createSessionMutation.mutateAsync({
        request: {
          source,
          cefrLevel,
          playFilter,
          timeLimitSeconds,
          requestedCount,
        },
      });

      navigate(`/student/vocabulary-games/falling-words/sessions/${session.id}`);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Không thể tạo ván chơi. Vui lòng thử lại.';
      setErrorMessage(msg);
    }
  };

  const isDailyNotFound =
    poolError &&
    typeof poolError === 'object' &&
    'response' in poolError &&
    (poolError as { response?: { data?: { code?: string } } }).response?.data?.code ===
      'DailyPlanNotFound';

  return (
    <div className="max-w-3xl mx-auto px-4 py-8 flex flex-col gap-8">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 text-emerald-700 text-sm font-semibold mb-1">
            <span>🎮 Trò chơi luyện từ</span>
          </div>
          <h1 className="text-3xl font-extrabold text-slate-800 tracking-tight">
            Từ vựng rơi
          </h1>
          <p className="text-slate-500 text-sm mt-1">
            Gõ nhanh từ tiếng Anh tương ứng với nghĩa tiếng Việt trước khi thẻ rơi chạm đáy!
          </p>
        </div>

        <button
          type="button"
          onClick={() => navigate('/student/vocabulary-games/falling-words/history')}
          className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-white border border-slate-200 text-slate-700 font-semibold text-sm hover:bg-slate-50 shadow-sm transition"
        >
          <span>📜 Xem lịch sử chơi</span>
        </button>
      </div>

      {/* Main Settings Card */}
      <div className="bg-white rounded-3xl p-6 sm:p-8 border border-slate-200 shadow-sm flex flex-col gap-6">
        {/* Source Selection */}
        <div>
          <label className="block text-sm font-bold text-slate-700 mb-2">
            1. Chọn nguồn từ vựng
          </label>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <button
              type="button"
              onClick={() => setSource('VocabularyCatalog')}
              className={`p-4 rounded-2xl border-2 text-left transition ${
                source === 'VocabularyCatalog'
                  ? 'border-emerald-600 bg-emerald-50/50 text-emerald-950 font-bold'
                  : 'border-slate-200 hover:border-slate-300 text-slate-700'
              }`}
            >
              <div className="font-bold text-base">📚 Kho từ vựng chung</div>
              <div className="text-xs text-slate-500 font-normal mt-1">
                Luyện tập từ vựng phong phú trên toàn bộ hệ thống
              </div>
            </button>

            <button
              type="button"
              onClick={() => setSource('DailyVocabulary')}
              className={`p-4 rounded-2xl border-2 text-left transition ${
                source === 'DailyVocabulary'
                  ? 'border-emerald-600 bg-emerald-50/50 text-emerald-950 font-bold'
                  : 'border-slate-200 hover:border-slate-300 text-slate-700'
              }`}
            >
              <div className="font-bold text-base">📅 Daily Vocabulary hôm nay</div>
              <div className="text-xs text-slate-500 font-normal mt-1">
                Luyện tập các từ trong kế hoạch học ngày hôm nay của bạn
              </div>
            </button>
          </div>

          {isDailyNotFound && (
            <div className="mt-3 p-4 rounded-2xl bg-amber-50 border border-amber-200 text-amber-900 text-sm flex items-center justify-between gap-3">
              <span>Bạn chưa khởi tạo kế hoạch Daily Vocabulary hôm nay.</span>
              <button
                type="button"
                onClick={() => navigate('/student/vocabulary/daily')}
                className="px-3 py-1.5 rounded-lg bg-amber-600 hover:bg-amber-700 text-white font-bold text-xs shrink-0"
              >
                Mở Daily Vocabulary
              </button>
            </div>
          )}
        </div>

        {/* CEFR Level Selection (Optional) */}
        <div>
          <label className="block text-sm font-bold text-slate-700 mb-2">
            2. Trình độ CEFR (Tùy chọn)
          </label>
          <div className="flex flex-wrap gap-2">
            {CEFR_LEVELS.map((level) => {
              const isSelected = cefrLevel === level.value;
              return (
                <button
                  key={level.value ?? 'all'}
                  type="button"
                  onClick={() => setCefrLevel(level.value)}
                  className={`px-3.5 py-2 rounded-xl text-sm font-semibold transition border ${
                    isSelected
                      ? 'bg-emerald-600 text-white border-emerald-600 shadow-sm'
                      : 'bg-slate-50 text-slate-700 border-slate-200 hover:bg-slate-100'
                  }`}
                >
                  {level.label}
                </button>
              );
            })}
          </div>
        </div>

        {/* Play History Filter */}
        <div>
          <label className="block text-sm font-bold text-slate-700 mb-2">
            3. Bộ lọc trạng thái đã chơi
          </label>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2.5">
            {PLAY_FILTERS.map((f) => {
              const isSelected = playFilter === f.value;
              return (
                <button
                  key={f.value}
                  type="button"
                  onClick={() => setPlayFilter(f.value)}
                  className={`p-3 rounded-xl border text-left transition ${
                    isSelected
                      ? 'border-emerald-600 bg-emerald-50/40 text-emerald-950 font-bold'
                      : 'border-slate-200 hover:border-slate-300 text-slate-700'
                  }`}
                >
                  <div className="font-semibold text-sm">{f.label}</div>
                  <div className="text-xs text-slate-500 font-normal mt-0.5 line-clamp-1">
                    {f.desc}
                  </div>
                </button>
              );
            })}
          </div>
        </div>

        {/* Speed & Number of Rounds */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2 border-t border-slate-100">
          <div>
            <label className="block text-sm font-bold text-slate-700 mb-1">
              Thời gian mỗi từ (giây)
            </label>
            <div className="flex items-center gap-3">
              <input
                type="range"
                min={2}
                max={30}
                step={1}
                value={timeLimitSeconds}
                onChange={(e) => setTimeLimitSeconds(Number(e.target.value))}
                className="w-full accent-emerald-600"
              />
              <span className="font-mono font-bold text-emerald-800 text-base min-w-[3rem] text-right">
                {timeLimitSeconds}s
              </span>
            </div>
            <p className="text-xs text-slate-400 mt-1">Mặc định: 5 giây/từ</p>
          </div>

          <div>
            <label className="block text-sm font-bold text-slate-700 mb-1">
              Số từ trong một ván
            </label>
            <div className="flex items-center gap-3">
              <input
                type="range"
                min={5}
                max={50}
                step={5}
                value={requestedCount}
                onChange={(e) => setRequestedCount(Number(e.target.value))}
                className="w-full accent-emerald-600"
              />
              <span className="font-mono font-bold text-emerald-800 text-base min-w-[3rem] text-right">
                {requestedCount} từ
              </span>
            </div>
            <p className="text-xs text-slate-400 mt-1">Mặc định: 10 từ/ván</p>
          </div>
        </div>

        {/* Available Pool Status Banner */}
        <div className="p-4 rounded-2xl bg-slate-50 border border-slate-200/80 flex items-center justify-between text-sm">
          <div className="flex items-center gap-2">
            <span className="text-slate-500 font-medium">Số từ hợp lệ:</span>
            {isLoadingPool ? (
              <span className="text-slate-400 animate-pulse">Đang kiểm tra...</span>
            ) : poolData ? (
              <span className="font-bold text-emerald-700 font-mono text-base">
                {poolData.totalValidCount} từ
              </span>
            ) : (
              <span className="text-slate-400">—</span>
            )}
          </div>

          {poolData && poolData.totalValidCount < requestedCount && poolData.totalValidCount > 0 && (
            <span className="text-xs text-amber-700 font-medium">
              Ván chơi sẽ dùng {poolData.totalValidCount} từ có sẵn
            </span>
          )}
        </div>

        {errorMessage && (
          <div className="p-4 rounded-2xl bg-rose-50 border border-rose-200 text-rose-800 text-sm">
            {errorMessage}
          </div>
        )}

        {/* Start Game Action */}
        <button
          type="button"
          onClick={handleStartGame}
          disabled={
            isLoadingPool ||
            createSessionMutation.isPending ||
            (poolData?.totalValidCount ?? 0) === 0
          }
          className="w-full py-4 rounded-2xl bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white font-extrabold text-lg shadow-lg shadow-emerald-600/25 disabled:opacity-50 disabled:cursor-not-allowed transition flex items-center justify-center gap-2"
        >
          {createSessionMutation.isPending ? (
            <span>Đang chuẩn bị ván chơi...</span>
          ) : (
            <>
              <span>Bắt đầu chơi</span>
              <span>🚀</span>
            </>
          )}
        </button>
      </div>
    </div>
  );
};
