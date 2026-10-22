import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  useFallingWordHistory,
  useFallingWordSessionsList,
} from '../hooks/useFallingWords';

export const FallingWordsHistoryPage: React.FC = () => {
  const navigate = useNavigate();
  const [tab, setTab] = useState<'sessions' | 'words'>('sessions');
  const [sessionsPage, setSessionsPage] = useState(1);
  const [wordsPage, setWordsPage] = useState(1);

  const { data: sessionsData, isLoading: isLoadingSessions } = useFallingWordSessionsList({
    page: sessionsPage,
    pageSize: 15,
  });

  const { data: wordsData, isLoading: isLoadingWords } = useFallingWordHistory({
    page: wordsPage,
    pageSize: 20,
  });

  return (
    <div className="max-w-4xl mx-auto px-4 py-8 flex flex-col gap-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-extrabold text-slate-800 tracking-tight">
            Lịch sử chơi Từ vựng rơi
          </h1>
          <p className="text-slate-500 text-sm mt-1">
            Theo dõi chi tiết các ván chơi và lịch sử kiểm tra của từng từ vựng
          </p>
        </div>

        <button
          type="button"
          onClick={() => navigate('/student/vocabulary-games/falling-words')}
          className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-sm shadow-md transition"
        >
          <span>Chơi ván mới</span>
          <span>🎮</span>
        </button>
      </div>

      {/* Tabs Switcher */}
      <div className="flex items-center gap-2 border-b border-slate-200">
        <button
          type="button"
          onClick={() => setTab('sessions')}
          className={`py-3 px-4 font-bold text-sm border-b-2 transition ${
            tab === 'sessions'
              ? 'border-emerald-600 text-emerald-800'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          Các ván đã chơi ({sessionsData?.totalCount ?? 0})
        </button>
        <button
          type="button"
          onClick={() => setTab('words')}
          className={`py-3 px-4 font-bold text-sm border-b-2 transition ${
            tab === 'words'
              ? 'border-emerald-600 text-emerald-800'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          Lịch sử từng từ ({wordsData?.totalCount ?? 0})
        </button>
      </div>

      {/* Tab 1: Sessions List */}
      {tab === 'sessions' && (
        <div className="flex flex-col gap-4">
          {isLoadingSessions ? (
            <div className="py-12 text-center text-slate-400 font-medium">
              Đang tải danh sách các ván chơi...
            </div>
          ) : !sessionsData || sessionsData.items.length === 0 ? (
            <div className="p-8 text-center bg-white rounded-3xl border border-slate-200 text-slate-500">
              Bạn chưa có ván chơi nào. Hãy bắt đầu một ván chơi mới!
            </div>
          ) : (
            <div className="flex flex-col gap-3">
              {sessionsData.items.map((session) => {
                const dateStr = new Date(session.createdAtUtc).toLocaleString('vi-VN', {
                  dateStyle: 'short',
                  timeStyle: 'short',
                });
                const isCompleted = session.status === 'Completed';

                return (
                  <div
                    key={session.id}
                    className="p-5 rounded-2xl bg-white border border-slate-200 shadow-sm flex flex-col sm:flex-row sm:items-center justify-between gap-4 hover:border-slate-300 transition"
                  >
                    <div className="flex flex-col gap-1">
                      <div className="flex items-center gap-2">
                        <span className="font-bold text-slate-800 text-base">
                          {session.source === 'DailyVocabulary'
                            ? 'Daily Vocabulary'
                            : 'Kho từ vựng'}
                        </span>
                        {session.cefrLevel && (
                          <span className="px-2 py-0.5 rounded text-xs font-semibold bg-emerald-100 text-emerald-800">
                            {session.cefrLevel}
                          </span>
                        )}
                        <span
                          className={`px-2 py-0.5 rounded text-xs font-bold ${
                            isCompleted
                              ? 'bg-slate-100 text-slate-700'
                              : 'bg-rose-100 text-rose-800'
                          }`}
                        >
                          {isCompleted ? 'Hoàn thành' : 'Đã dừng'}
                        </span>
                      </div>
                      <div className="text-xs text-slate-400">
                        {dateStr} • {session.timeLimitSeconds}s/từ • {session.actualCount} từ
                      </div>
                    </div>

                    <div className="flex items-center gap-4 text-sm font-semibold">
                      <div className="flex flex-col items-center">
                        <span className="text-emerald-600 font-mono font-bold">
                          {session.correctCount}
                        </span>
                        <span className="text-[10px] text-slate-400 font-normal uppercase">
                          Đúng
                        </span>
                      </div>
                      <div className="flex flex-col items-center">
                        <span className="text-rose-600 font-mono font-bold">
                          {session.incorrectCount + session.timedOutCount}
                        </span>
                        <span className="text-[10px] text-slate-400 font-normal uppercase">
                          Sai
                        </span>
                      </div>
                      <div className="flex flex-col items-center">
                        <span className="text-emerald-800 font-mono font-bold">
                          {session.accuracy != null ? `${session.accuracy}%` : '—'}
                        </span>
                        <span className="text-[10px] text-slate-400 font-normal uppercase">
                          Độ chính xác
                        </span>
                      </div>
                      <button
                        type="button"
                        onClick={() =>
                          navigate(`/student/vocabulary-games/falling-words/sessions/${session.id}`)
                        }
                        className="py-1.5 px-3 rounded-lg border border-slate-200 text-xs font-bold text-slate-700 hover:bg-slate-50 transition"
                      >
                        Chi tiết
                      </button>
                    </div>
                  </div>
                );
              })}

              {/* Pagination */}
              {sessionsData.totalPages > 1 && (
                <div className="flex items-center justify-center gap-3 pt-4">
                  <button
                    type="button"
                    onClick={() => setSessionsPage((p) => Math.max(1, p - 1))}
                    disabled={sessionsPage <= 1}
                    className="px-3 py-1.5 rounded-lg border text-sm font-semibold disabled:opacity-40"
                  >
                    Trang trước
                  </button>
                  <span className="text-xs text-slate-500 font-medium">
                    {sessionsPage} / {sessionsData.totalPages}
                  </span>
                  <button
                    type="button"
                    onClick={() =>
                      setSessionsPage((p) => Math.min(sessionsData.totalPages, p + 1))
                    }
                    disabled={sessionsPage >= sessionsData.totalPages}
                    className="px-3 py-1.5 rounded-lg border text-sm font-semibold disabled:opacity-40"
                  >
                    Trang sau
                  </button>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* Tab 2: Words List */}
      {tab === 'words' && (
        <div className="flex flex-col gap-4">
          {isLoadingWords ? (
            <div className="py-12 text-center text-slate-400 font-medium">
              Đang tải lịch sử từng từ...
            </div>
          ) : !wordsData || wordsData.items.length === 0 ? (
            <div className="p-8 text-center bg-white rounded-3xl border border-slate-200 text-slate-500">
              Chưa có từ nào được ghi nhận kết quả.
            </div>
          ) : (
            <div className="overflow-x-auto bg-white rounded-2xl border border-slate-200 shadow-sm">
              <table className="w-full text-left text-sm">
                <thead className="bg-slate-50 text-slate-500 font-bold text-xs uppercase border-b border-slate-200">
                  <tr>
                    <th className="py-3 px-4">Từ tiếng Anh</th>
                    <th className="py-3 px-4">Kết quả</th>
                    <th className="py-3 px-4">Bạn đã nhập</th>
                    <th className="py-3 px-4">Thời gian phản hồi</th>
                    <th className="py-3 px-4">Ngày chơi</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {wordsData.items.map((item) => {
                    const isCorrect = item.outcome === 'Correct';
                    const isTimed = item.outcome === 'TimedOut';
                    const dateStr = new Date(item.startedAtUtc).toLocaleString('vi-VN', {
                      dateStyle: 'short',
                      timeStyle: 'short',
                    });

                    return (
                      <tr key={item.roundId} className="hover:bg-slate-50/60">
                        <td className="py-3 px-4 font-mono font-bold text-slate-800 text-base">
                          {item.expectedWord}
                        </td>
                        <td className="py-3 px-4">
                          <span
                            className={`px-2 py-0.5 rounded text-xs font-bold ${
                              isCorrect
                                ? 'bg-emerald-100 text-emerald-800'
                                : isTimed
                                ? 'bg-amber-100 text-amber-800'
                                : 'bg-rose-100 text-rose-800'
                            }`}
                          >
                            {isCorrect ? 'Đúng' : isTimed ? 'Hết giờ' : 'Sai'}
                          </span>
                        </td>
                        <td className="py-3 px-4 font-mono text-slate-600">
                          {item.learnerAnswer || '—'}
                        </td>
                        <td className="py-3 px-4 text-xs text-slate-500">
                          {item.elapsedMs != null
                            ? `${(item.elapsedMs / 1000).toFixed(2)}s`
                            : '—'}
                        </td>
                        <td className="py-3 px-4 text-xs text-slate-400">{dateStr}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>

              {/* Pagination */}
              {wordsData.totalPages > 1 && (
                <div className="flex items-center justify-center gap-3 p-4 border-t border-slate-100">
                  <button
                    type="button"
                    onClick={() => setWordsPage((p) => Math.max(1, p - 1))}
                    disabled={wordsPage <= 1}
                    className="px-3 py-1.5 rounded-lg border text-sm font-semibold disabled:opacity-40"
                  >
                    Trang trước
                  </button>
                  <span className="text-xs text-slate-500 font-medium">
                    {wordsPage} / {wordsData.totalPages}
                  </span>
                  <button
                    type="button"
                    onClick={() =>
                      setWordsPage((p) => Math.min(wordsData.totalPages, p + 1))
                    }
                    disabled={wordsPage >= wordsData.totalPages}
                    className="px-3 py-1.5 rounded-lg border text-sm font-semibold disabled:opacity-40"
                  >
                    Trang sau
                  </button>
                </div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
};
