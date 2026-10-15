import React from 'react';
import { Award, Cpu, CheckCircle2, XCircle } from 'lucide-react';
import {
  useActiveScoringRubricQuery,
  usePronunciationModelsQuery,
  useVocabularyGenerationModelsQuery,
} from '@/features/catalog';
import { Badge } from '@/shared/components/ui/Badge';

export const AdminRubricsPage: React.FC = () => {
  const { data: rubric } = useActiveScoringRubricQuery();
  const { data: vocabModels = [] } = useVocabularyGenerationModelsQuery();
  const { data: audioModels = [] } = usePronunciationModelsQuery();

  return (
    <div className="space-y-6">
      <div className="bg-white rounded-3xl border border-slate-200 p-6 flex items-center justify-between">
        <div>
          <h1 className="text-lg font-extrabold text-slate-900 flex items-center gap-2">
            <Award className="w-5 h-5 text-emerald-600" />
            Rubric Chấm Phát Âm & Danh mục AI Models trong Database
          </h1>
          <p className="text-xs text-slate-500 mt-0.5">
            Phân tách rõ Model Tạo Từ Vựng (Gemini & Gemma 4) và Model Chấm Phát Âm Audio (chỉ nhận Model hỗ trợ Audio Input).
          </p>
        </div>
        <Badge variant="emerald">Permission: rubrics.manage</Badge>
      </div>

      {/* Scoring Rubric Criteria */}
      {rubric && (
        <div className="bg-white rounded-3xl border border-slate-200 p-6 space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-sm font-extrabold text-slate-900">{rubric.name}</h2>
              <p className="text-xs text-slate-500">{rubric.description}</p>
            </div>
            <Badge variant="amber">{rubric.code}</Badge>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            {rubric.criteria.map((c) => (
              <div key={c.code} className="p-4 rounded-2xl bg-slate-50 border border-slate-200">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-extrabold text-slate-900">{c.name}</span>
                  <Badge variant="emerald">{c.weight}%</Badge>
                </div>
                <p className="text-xs text-slate-600 mt-1">{c.description}</p>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* AI Models Capability Matrix */}
      <div className="bg-white rounded-3xl border border-slate-200 overflow-hidden">
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between">
          <span className="text-sm font-extrabold text-slate-900 flex items-center gap-2">
            <Cpu className="w-4 h-4 text-emerald-600" />
            Ma trận Năng lực AI Models (`AiModelCatalog`)
          </span>
          <span className="text-xs text-slate-500">
            {vocabModels.length} Vocab Models · {audioModels.length} Audio Input Models
          </span>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse text-xs">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200 text-slate-600 uppercase">
                <th className="py-3 px-4">Tên Model</th>
                <th className="py-3 px-3">Mã Model</th>
                <th className="py-3 px-3">Tạo Từ Vựng JSON</th>
                <th className="py-3 px-3">Audio Input (Chấm Phát Âm)</th>
                <th className="py-3 px-4">Ghi chú năng lực</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {vocabModels.map((m) => (
                <tr key={m.modelCode} className="hover:bg-slate-50">
                  <td className="py-3 px-4 font-bold text-slate-900">{m.displayName}</td>
                  <td className="py-3 px-3 font-mono text-slate-600">{m.modelCode}</td>
                  <td className="py-3 px-3">
                    <span className="inline-flex items-center gap-1 text-emerald-700 font-semibold">
                      <CheckCircle2 className="w-3.5 h-3.5" /> Hỗ trợ
                    </span>
                  </td>
                  <td className="py-3 px-3">
                    {m.supportsAudioInput ? (
                      <span className="inline-flex items-center gap-1 text-emerald-700 font-semibold">
                        <CheckCircle2 className="w-3.5 h-3.5" /> Có Audio Input
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1 text-rose-600 font-semibold">
                        <XCircle className="w-3.5 h-3.5" /> Chỉ Text (Chặn ở Audio Studio)
                      </span>
                    )}
                  </td>
                  <td className="py-3 px-4 text-slate-500">{m.capabilityNote}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
