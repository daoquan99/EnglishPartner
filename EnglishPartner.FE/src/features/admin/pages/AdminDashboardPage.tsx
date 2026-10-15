import React from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
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
  const { t } = useTranslation();
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
      title: t('nav.adminVocabularies'),
      value: `${vocabularies.length} ${t('vocab.wordsUnit')}`,
      sub: t('nav.adminVocabulariesSub'),
      to: '/admin/vocabularies',
      icon: <img src="/assets/icons/navigation/nav-reviews.svg" alt="" className="w-5 h-5 object-contain" />,
    },
    {
      title: t('nav.adminTopics'),
      value: `${topics.length} topics`,
      sub: t('nav.adminTopicsSub'),
      to: '/admin/topics',
      icon: <img src="/assets/icons/navigation/nav-topics.svg" alt="" className="w-5 h-5 object-contain" />,
    },
    {
      title: t('nav.adminPrompts'),
      value: `${modes.length} Modes · ${speeds.length} Speeds`,
      sub: t('nav.adminPromptsSub'),
      to: '/admin/prompts',
      icon: <img src="/assets/icons/navigation/nav-prompts.svg" alt="" className="w-5 h-5 object-contain" />,
    },
    {
      title: t('nav.adminVoices'),
      value: `${voices.length} Voices`,
      sub: t('nav.adminVoicesSub'),
      to: '/admin/voices',
      icon: <img src="/assets/icons/navigation/nav-voices.svg" alt="" className="w-5 h-5 object-contain" />,
    },
    {
      title: t('nav.adminRubrics'),
      value: `${aiModels.length} AI Models`,
      sub: t('nav.adminRubricsSub'),
      to: '/admin/rubrics',
      icon: <img src="/assets/icons/navigation/nav-rubrics.svg" alt="" className="w-5 h-5 object-contain" />,
    },
    {
      title: t('nav.adminUsers'),
      value: `${users.length} users`,
      sub: t('nav.adminUsersSub'),
      to: '/admin/users',
      icon: <img src="/assets/icons/navigation/nav-account.svg" alt="" className="w-5 h-5 object-contain" />,
    },
    {
      title: t('nav.adminRoles'),
      value: `${roles.length} Roles`,
      sub: t('nav.adminRolesSub'),
      to: '/admin/roles',
      icon: <img src="/assets/icons/navigation/nav-roles.svg" alt="" className="w-5 h-5 object-contain" />,
    },
  ];

  return (
    <div className="space-y-6">
      <div className="bg-white rounded-3xl border border-slate-200 p-6 shadow-xs flex items-center gap-4">
        <div className="w-12 h-12 rounded-2xl bg-emerald-50 border border-emerald-200 flex items-center justify-center shrink-0">
          <img src="/assets/icons/navigation/nav-dashboard.svg" alt="" className="w-6 h-6 object-contain" />
        </div>
        <div>
          <h1 className="text-xl font-extrabold text-slate-900">
            {t('admin.dashboardTitle')}
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            {t('admin.dashboardSubtitle')}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
        {cards.map((c) => (
          <Link
            key={c.to}
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
