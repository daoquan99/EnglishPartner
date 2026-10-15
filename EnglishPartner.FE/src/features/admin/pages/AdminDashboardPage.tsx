import React from 'react';
import { Link } from 'react-router-dom';
import { Tags, MessageSquareCode, Volume2, Users, ShieldCheck, Award } from 'lucide-react';
import {
  useConversationModesQuery,
  usePronunciationModelsQuery,
  useVocabularyGenerationModelsQuery,
} from '@/features/catalog';
import {
  useAdminRolesQuery,
  useAdminTopicsQuery,
  useAdminUsersQuery,
  useAdminVoicesQuery,
} from '../hooks/useAdminQueries';

export const AdminDashboardPage: React.FC = () => {
  const { data: topics = [] } = useAdminTopicsQuery();
  const { data: modes = [] } = useConversationModesQuery();
  const { data: voices = [] } = useAdminVoicesQuery();
  const { data: users = [] } = useAdminUsersQuery();
  const { data: roles = [] } = useAdminRolesQuery();
  const { data: vocabModels = [] } = useVocabularyGenerationModelsQuery();
  const { data: audioModels = [] } = usePronunciationModelsQuery();

  const cards = [
    {
      title: 'Topics Aggregate (DB)',
      value: `${topics.length} chủ đề`,
      sub: `${topics.filter((t) => t.supportsConversation).length} Hội thoại · ${topics.filter((t) => t.supportsVocabulary).length} Từ vựng`,
      to: '/admin/topics',
      icon: <Tags className="w-5 h-5 text-emerald-600" />,
    },
    {
      title: 'Chế độ Luyện nói (DB)',
      value: `${modes.length} Modes`,
      sub: 'CefrPromptBuilder lưu trong SQL Server',
      to: '/admin/prompts',
      icon: <MessageSquareCode className="w-5 h-5 text-sky-600" />,
    },
    {
      title: 'Giọng đọc Teacher (Google)',
      value: `${voices.length} Voices`,
      sub: `${voices.filter((v) => v.isFeatured).length} giọng Featured ưu tiên`,
      to: '/admin/voices',
      icon: <Volume2 className="w-5 h-5 text-violet-600" />,
    },
    {
      title: 'AI Models (Vocab & Audio)',
      value: `${vocabModels.length} Vocab / ${audioModels.length} Audio`,
      sub: 'Gemini 3.8/2.5 & Gemma 4 (Text-only)',
      to: '/admin/rubrics',
      icon: <Award className="w-5 h-5 text-amber-600" />,
    },
    {
      title: 'Người dùng (Users)',
      value: `${users.length} tài khoản`,
      sub: 'Quản lý gán Role & trạng thái hoạt động',
      to: '/admin/users',
      icon: <Users className="w-5 h-5 text-rose-600" />,
    },
    {
      title: 'Roles & Permissions',
      value: `${roles.length} Roles`,
      sub: 'ADMIN · TEACHER · LEARNER',
      to: '/admin/roles',
      icon: <ShieldCheck className="w-5 h-5 text-teal-600" />,
    },
  ];

  return (
    <div className="space-y-6">
      <div className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs">
        <h1 className="text-xl font-extrabold text-slate-900">
          Trung tâm Quản trị Hệ thống (Admin Portal)
        </h1>
        <p className="text-xs text-slate-500 mt-1">
          Quản lý tập trung các Aggregate trong Database: Topics, CEFR & Session Mode Prompts, 30 Google Teacher Voices, AI Models & RBAC Permissions.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
        {cards.map((c) => (
          <Link
            key={c.title}
            to={c.to}
            className="bg-white rounded-3xl border border-slate-200 p-5 hover:border-emerald-500 hover:shadow-md transition-all space-y-2"
          >
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold uppercase tracking-wider text-slate-500">
                {c.title}
              </span>
              <div className="w-10 h-10 rounded-2xl bg-slate-50 flex items-center justify-center">
                {c.icon}
              </div>
            </div>
            <div className="text-2xl font-extrabold text-slate-900">{c.value}</div>
            <div className="text-xs text-slate-500">{c.sub}</div>
          </Link>
        ))}
      </div>
    </div>
  );
};
