import React from 'react';
import { Link } from 'react-router-dom';
import {
  Tags,
  MessageSquareCode,
  Volume2,
  Users,
  ShieldCheck,
  Award,
  BookOpen,
} from 'lucide-react';
import { useConversationModesQuery } from '@/features/catalog';
import {
  useAdminAiModelsQuery,
  useAdminRolesQuery,
  useAdminSpeechSpeedsQuery,
  useAdminTopicsQuery,
  useAdminUsersQuery,
  useAdminVocabulariesQuery,
  useAdminVoicesQuery,
} from '../hooks/useAdminQueries';

export const AdminDashboardPage: React.FC = () => {
  const { data: vocabularies = [] } = useAdminVocabulariesQuery();
  const { data: topics = [] } = useAdminTopicsQuery();
  const { data: modes = [] } = useConversationModesQuery();
  const { data: speeds = [] } = useAdminSpeechSpeedsQuery();
  const { data: voices = [] } = useAdminVoicesQuery();
  const { data: users = [] } = useAdminUsersQuery();
  const { data: roles = [] } = useAdminRolesQuery();
  const { data: aiModels = [] } = useAdminAiModelsQuery();

  const cards = [
    {
      title: 'Từ vựng / Flashcards (CRUD)',
      value: `${vocabularies.length} thẻ từ`,
      sub: 'Quản lý Sense, UK/US IPA, Synonyms, Collocations & Examples',
      to: '/admin/vocabularies',
      icon: <BookOpen className="w-5 h-5 text-emerald-600" />,
    },
    {
      title: 'Topics Aggregate (CRUD)',
      value: `${topics.length} chủ đề`,
      sub: `${topics.filter((t) => t.supportsConversation).length} Hội thoại · ${topics.filter((t) => t.supportsVocabulary).length} Từ vựng`,
      to: '/admin/topics',
      icon: <Tags className="w-5 h-5 text-teal-600" />,
    },
    {
      title: 'CEFR, 19 Modes & Tốc độ nói (CRUD)',
      value: `${modes.length} Modes · ${speeds.length} Speeds`,
      sub: 'CefrLevelPrompts, ConversationModes & SpeechSpeeds trong DB',
      to: '/admin/prompts',
      icon: <MessageSquareCode className="w-5 h-5 text-sky-600" />,
    },
    {
      title: '30 Giọng đọc Teacher (CRUD)',
      value: `${voices.length} Voices`,
      sub: `${voices.filter((v) => v.isFeatured).length} giọng Featured ưu tiên`,
      to: '/admin/voices',
      icon: <Volume2 className="w-5 h-5 text-violet-600" />,
    },
    {
      title: 'Rubric & AI Models (CRUD)',
      value: `${aiModels.length} AI Models`,
      sub: 'ENGLISH_PRONUNCIATION_V1 & AiModelCatalog trong DB',
      to: '/admin/rubrics',
      icon: <Award className="w-5 h-5 text-amber-600" />,
    },
    {
      title: 'Người dùng & Phân quyền (CRUD)',
      value: `${users.length} tài khoản`,
      sub: 'Tạo, sửa, khóa/mở khóa & gán Role cho Users',
      to: '/admin/users',
      icon: <Users className="w-5 h-5 text-rose-600" />,
    },
    {
      title: 'Ma trận Roles & Permissions (CRUD)',
      value: `${roles.length} Roles`,
      sub: 'ADMIN · TEACHER · LEARNER',
      to: '/admin/roles',
      icon: <ShieldCheck className="w-5 h-5 text-indigo-600" />,
    },
  ];

  return (
    <div className="space-y-6">
      <div className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs">
        <h1 className="text-xl font-extrabold text-slate-900">
          Trung tâm Quản trị Hệ thống (Admin Portal — Full CRUD)
        </h1>
        <p className="text-xs text-slate-500 mt-1">
          Quản lý đầy đủ Create / Read / Update / Delete cho toàn bộ các Aggregate trong Database: Flashcards, Topics, CEFR, 19 Modes, Speech Speeds, 30 Teacher Voices, Rubrics, AI Models, Users, Roles & Permissions.
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
