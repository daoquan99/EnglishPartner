import React, { useState } from 'react';
import { Plus, Trash2, Save, Tags } from 'lucide-react';
import {
  useAdminTopicsQuery,
  useCreateTopicMutation,
  useDeleteTopicMutation,
} from '../hooks/useAdminQueries';
import { useConversationModesQuery } from '@/features/catalog';
import { CanAccess } from '@/features/auth';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import type { CefrLevel, TopicDto } from '@/shared/api/openapi.types';

export const AdminTopicsPage: React.FC = () => {
  const { data: topics = [] } = useAdminTopicsQuery();
  const { data: modes = [] } = useConversationModesQuery();
  const createMutation = useCreateTopicMutation();
  const deleteMutation = useDeleteTopicMutation();

  const [localTopics, setLocalTopics] = useState<TopicDto[] | null>(null);
  const displayedTopics = localTopics ?? topics;

  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [displayNameVi, setDisplayNameVi] = useState('');
  const [category, setCategory] = useState('Software Engineering');
  const [modeCode, setModeCode] = useState('technical_interview');
  const [suggestedCefrLevel, setSuggestedCefrLevel] = useState<CefrLevel>('B2');
  const [supportsConversation, setSupportsConversation] = useState(true);
  const [supportsVocabulary, setSupportsVocabulary] = useState(true);
  const [isFeatured, setIsFeatured] = useState(true);

  const handleAddTopic = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!code.trim() || !name.trim()) return;

    const payload = {
      code: code.trim().toLowerCase().replace(/\s+/g, '_'),
      name: name.trim(),
      displayNameVi: displayNameVi.trim() || name.trim(),
      category,
      modeCode: modeCode || null,
      suggestedCefrLevel,
      supportsConversation,
      supportsVocabulary,
      description: displayNameVi.trim() || name.trim(),
      isFeatured,
      isActive: true,
      displayOrder: displayedTopics.length + 1,
    };

    try {
      const saved = await createMutation.mutateAsync(payload);
      setLocalTopics([saved, ...displayedTopics]);
    } catch {
      const fallbackTopic: TopicDto = {
        id: crypto.randomUUID(),
        ...payload,
      };
      setLocalTopics([fallbackTopic, ...displayedTopics]);
    }

    setCode('');
    setName('');
    setDisplayNameVi('');
  };

  const handleDelete = async (id: string) => {
    try {
      await deleteMutation.mutateAsync(id);
    } catch {
      // ignore offline
    }
    setLocalTopics(displayedTopics.filter((t) => t.id !== id));
  };

  return (
    <div className="space-y-6">
      <div className="bg-white rounded-3xl border border-slate-200 p-6 flex items-center justify-between">
        <div>
          <h1 className="text-lg font-extrabold text-slate-900 flex items-center gap-2">
            <Tags className="w-5 h-5 text-emerald-600" />
            Quản lý Topics Aggregate ({displayedTopics.length} chủ đề)
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Entity/Aggregate độc lập cung cấp gợi ý cho cả Chủ đề Cuộc trò chuyện và Chủ đề Tạo thêm từ vựng.
          </p>
        </div>
        <Badge variant="emerald">Permission: topics.manage</Badge>
      </div>

      {/* Create / Upsert Topic Form */}
      <CanAccess permission="topics.manage">
        <form
          onSubmit={handleAddTopic}
          className="bg-white rounded-3xl border border-slate-200 p-6 space-y-4"
        >
          <h2 className="text-sm font-bold text-slate-800 flex items-center gap-1.5">
            <Plus className="w-4 h-4 text-emerald-600" />
            Thêm / Cập nhật Topic vào Database
          </h2>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div>
              <label className="block text-xs font-bold text-slate-600 mb-1">Mã Code (Unique)</label>
              <input
                type="text"
                value={code}
                onChange={(e) => setCode(e.target.value)}
                placeholder="vd: ef_core_performance"
                className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs"
                required
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-600 mb-1">
                Tên Chủ đề (Tiếng Anh)
              </label>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="vd: EF Core Query Splitting & Tracking"
                className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs"
                required
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-600 mb-1">
                Mô tả / Nhãn Tiếng Việt
              </label>
              <input
                type="text"
                value={displayNameVi}
                onChange={(e) => setDisplayNameVi(e.target.value)}
                placeholder="vd: Tối ưu EF Core Tracking & Split Queries"
                className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-4 gap-3 items-center">
            <div>
              <label className="block text-xs font-bold text-slate-600 mb-1">Danh mục</label>
              <input
                type="text"
                value={category}
                onChange={(e) => setCategory(e.target.value)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-600 mb-1">
                Gắn với Mode (Tùy chọn)
              </label>
              <select
                value={modeCode}
                onChange={(e) => setModeCode(e.target.value)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs bg-white"
              >
                <option value="">-- Dùng chung mọi Mode --</option>
                {modes.map((m) => (
                  <option key={m.modeCode} value={m.modeCode}>
                    {m.modeCode}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-600 mb-1">CEFR Gợi ý</label>
              <select
                value={suggestedCefrLevel}
                onChange={(e) => setSuggestedCefrLevel(e.target.value as CefrLevel)}
                className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs bg-white"
              >
                {(['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as CefrLevel[]).map((l) => (
                  <option key={l} value={l}>
                    {l}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex items-center gap-3 pt-4 text-xs font-semibold text-slate-700">
              <label className="flex items-center gap-1 cursor-pointer">
                <input
                  type="checkbox"
                  checked={supportsConversation}
                  onChange={(e) => setSupportsConversation(e.target.checked)}
                />
                Hội thoại
              </label>
              <label className="flex items-center gap-1 cursor-pointer">
                <input
                  type="checkbox"
                  checked={supportsVocabulary}
                  onChange={(e) => setSupportsVocabulary(e.target.checked)}
                />
                Từ vựng
              </label>
              <label className="flex items-center gap-1 cursor-pointer">
                <input
                  type="checkbox"
                  checked={isFeatured}
                  onChange={(e) => setIsFeatured(e.target.checked)}
                />
                Nổi bật
              </label>
            </div>
          </div>

          <Button type="submit" size="sm">
            <Save className="w-3.5 h-3.5" />
            Lưu Topic vào Database
          </Button>
        </form>
      </CanAccess>

      {/* Topics Table */}
      <div className="bg-white rounded-3xl border border-slate-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse text-xs">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200 text-slate-600 uppercase">
                <th className="py-3 px-4">Chủ đề (EN / VI)</th>
                <th className="py-3 px-3">Danh mục</th>
                <th className="py-3 px-3">Mode liên kết</th>
                <th className="py-3 px-3">CEFR</th>
                <th className="py-3 px-3">Phạm vi gợi ý</th>
                <th className="py-3 px-3 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {displayedTopics.map((t) => (
                <tr key={t.id} className="hover:bg-slate-50/80">
                  <td className="py-3 px-4">
                    <div className="font-bold text-slate-900">{t.name}</div>
                    <div className="text-slate-500">{t.displayNameVi}</div>
                    <div className="font-mono text-[10px] text-slate-400">{t.code}</div>
                  </td>
                  <td className="py-3 px-3">
                    <Badge variant="default">{t.category}</Badge>
                  </td>
                  <td className="py-3 px-3 font-mono text-slate-600">{t.modeCode || 'All Modes'}</td>
                  <td className="py-3 px-3">
                    <Badge variant="emerald">{t.suggestedCefrLevel}</Badge>
                  </td>
                  <td className="py-3 px-3 space-x-1">
                    {t.supportsConversation && <Badge variant="sky">Live Tutor</Badge>}
                    {t.supportsVocabulary && <Badge variant="violet">Vocab AI</Badge>}
                  </td>
                  <td className="py-3 px-3 text-right">
                    <CanAccess permission="topics.manage">
                      <button
                        type="button"
                        onClick={() => handleDelete(t.id)}
                        className="p-1.5 text-slate-400 hover:text-rose-600 rounded-lg cursor-pointer"
                        title="Xóa Topic"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </CanAccess>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
