import React from 'react';
import { Volume2, Star } from 'lucide-react';
import { useAdminVoicesQuery } from '../hooks/useAdminQueries';
import { Badge } from '@/shared/components/ui/Badge';

export const AdminVoicesPage: React.FC = () => {
  const { data: voices = [] } = useAdminVoicesQuery();

  return (
    <div className="space-y-6">
      <div className="bg-white rounded-3xl border border-slate-200 p-6 flex items-center justify-between">
        <div>
          <h1 className="text-lg font-extrabold text-slate-900 flex items-center gap-2">
            <Volume2 className="w-5 h-5 text-emerald-600" />
            Danh mục 30 Giọng nói của Teacher (`TeacherVoices`)
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Toàn bộ 30 giọng đọc Google Prebuilt Voices lưu trong Database (6 giọng Featured + 24 giọng mở rộng).
          </p>
        </div>
        <Badge variant="emerald">Permission: voices.manage</Badge>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3.5">
        {voices.map((v) => (
          <div
            key={v.id}
            className="bg-white rounded-2xl border border-slate-200 p-4 space-y-2 shadow-2xs"
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="text-sm font-extrabold text-slate-900">{v.label}</span>
                {v.isFeatured && (
                  <Badge variant="amber">
                    <Star className="w-3 h-3" /> Featured
                  </Badge>
                )}
              </div>
              <Badge variant={v.gender === 'Female' ? 'violet' : 'sky'}>{v.gender}</Badge>
            </div>
            <div className="text-xs font-semibold text-emerald-800">{v.style}</div>
            <div className="text-[11px] text-slate-500">
              Persona: <strong>{v.persona}</strong> · {v.description}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
