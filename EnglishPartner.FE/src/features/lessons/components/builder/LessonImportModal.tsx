import React, { useState } from 'react';
import { X, Check, AlertCircle, FileCheck, UploadCloud, Layers } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { useValidateCandidateMutation, useCommitLessonImportMutation } from '../../hooks/useLessonQueries';
import type { ValidationIssueDto } from '../../types/lessons.types';

interface LessonImportModalProps {
  isOpen: boolean;
  onClose: () => void;
  targetLessonId: string;
  onSuccess: () => void;
}

export const LessonImportModal: React.FC<LessonImportModalProps> = ({
  isOpen,
  onClose,
  targetLessonId,
  onSuccess,
}) => {
  const [jsonText, setJsonText] = useState('');
  const [commitMode, setCommitMode] = useState<'Append' | 'Replace'>('Append');
  const [issues, setIssues] = useState<ValidationIssueDto[]>([]);
  const [isValidated, setIsValidated] = useState(false);
  const [candidateParsed, setCandidateParsed] = useState<Record<string, unknown> | null>(null);

  const validateMutation = useValidateCandidateMutation();
  const commitMutation = useCommitLessonImportMutation();

  if (!isOpen) return null;

  const handleValidate = async () => {
    setIssues([]);
    setIsValidated(false);
    setCandidateParsed(null);

    let parsed: Record<string, unknown>;
    try {
      parsed = JSON.parse(jsonText);
      setCandidateParsed(parsed);
    } catch {
      setIssues([
        {
          path: '$',
          severity: 'Error',
          message: 'Dữ liệu JSON không hợp lệ. Vui lòng kiểm tra lại cú pháp trước khi kiểm tra schema.',
        },
      ]);
      return;
    }

    try {
      const res = await validateMutation.mutateAsync(parsed);
      setIssues(res.issues);
      setIsValidated(res.isValid);
    } catch (err: any) {
      setIssues([
        {
          path: '$',
          severity: 'Error',
          message: err?.message || 'Lỗi khi gọi API kiểm chứng candidate.',
        },
      ]);
    }
  };

  const handleCommit = async () => {
    if (!candidateParsed) return;
    const operationKey = `op_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;

    await commitMutation.mutateAsync({
      data: {
        targetLessonId,
        commitMode,
        candidate: candidateParsed,
      },
      operationKey,
    });

    onSuccess();
    onClose();
  };

  const errorCount = issues.filter((i) => i.severity === 'Error').length;
  const warningCount = issues.filter((i) => i.severity === 'Warning').length;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 p-4 overflow-y-auto">
      <div className="bg-white rounded-3xl border border-slate-200 shadow-2xl w-full max-w-2xl max-h-[90vh] flex flex-col my-8">
        {/* Header */}
        <div className="flex items-center justify-between p-5 border-b border-slate-100">
          <div>
            <h3 className="text-base font-bold text-slate-900 flex items-center gap-2">
              <UploadCloud className="w-5 h-5 text-emerald-600" />
              Nhập bài tập từ JSON Candidate
            </h3>
            <p className="text-xs text-slate-500">
              Kiểm tra schema và lưu trực tiếp các bài tập vào bài học.
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-slate-700 rounded-xl hover:bg-slate-100 cursor-pointer"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto p-5 space-y-4">
          {/* Commit Mode Radio */}
          <div className="flex items-center gap-4 bg-slate-50 p-3 rounded-2xl border border-slate-200/80">
            <span className="text-xs font-bold text-slate-700">Chế độ ghi:</span>
            <label className="flex items-center gap-2 text-xs font-medium text-slate-800 cursor-pointer">
              <input
                type="radio"
                name="commitMode"
                value="Append"
                checked={commitMode === 'Append'}
                onChange={() => setCommitMode('Append')}
                className="text-emerald-600 focus:ring-emerald-500"
              />
              Thêm tiếp vào cuối (Append)
            </label>
            <label className="flex items-center gap-2 text-xs font-medium text-slate-800 cursor-pointer">
              <input
                type="radio"
                name="commitMode"
                value="Replace"
                checked={commitMode === 'Replace'}
                onChange={() => setCommitMode('Replace')}
                className="text-emerald-600 focus:ring-emerald-500"
              />
              Ghi đè toàn bộ bài tập (Replace)
            </label>
          </div>

          {/* JSON Textarea */}
          <div className="space-y-1">
            <label className="text-xs font-bold text-slate-700 block">
              Dán nội dung JSON Candidate (Lesson Candidate Payload):
            </label>
            <textarea
              rows={9}
              placeholder='{\n  "lesson": { "title": "...", "exercises": [...] }\n}'
              value={jsonText}
              onChange={(e) => {
                setJsonText(e.target.value);
                setIsValidated(false);
              }}
              className="w-full text-xs font-mono bg-slate-900 text-emerald-400 p-3.5 rounded-2xl border border-slate-800 focus:outline-none focus:ring-2 focus:ring-emerald-500 leading-normal"
            />
          </div>

          {/* Validate Action */}
          <div className="flex items-center justify-between">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={handleValidate}
              disabled={!jsonText.trim() || validateMutation.isPending}
              className="gap-1.5 text-xs text-slate-700 border-slate-200 hover:border-emerald-500 hover:text-emerald-700 hover:bg-emerald-50"
            >
              <FileCheck className="w-4 h-4" />
              {validateMutation.isPending ? 'Đang kiểm chứng schema...' : 'Kiểm tra tính hợp lệ (Validate)'}
            </Button>

            {isValidated && errorCount === 0 && (
              <Badge variant="emerald">
                JSON hoàn toàn hợp lệ theo contract v1!
              </Badge>
            )}
          </div>

          {/* Validation Issues List */}
          {issues.length > 0 && (
            <div className="space-y-2 border-t border-slate-100 pt-3">
              <div className="flex items-center justify-between text-xs font-bold">
                <span className="text-slate-700">Kết quả kiểm tra Schema:</span>
                <div className="flex gap-2">
                  {errorCount > 0 && (
                    <Badge variant="rose">
                      {errorCount} lỗi (Error)
                    </Badge>
                  )}
                  {warningCount > 0 && (
                    <Badge variant="amber">
                      {warningCount} cảnh báo (Warning)
                    </Badge>
                  )}
                </div>
              </div>

              <div className="max-h-48 overflow-y-auto space-y-1.5">
                {issues.map((issue, idx) => (
                  <div
                    key={idx}
                    className={`p-2.5 rounded-xl border text-xs flex items-start gap-2 ${
                      issue.severity === 'Error'
                        ? 'bg-rose-50/80 border-rose-200 text-rose-900'
                        : 'bg-amber-50/80 border-amber-200 text-amber-900'
                    }`}
                  >
                    <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
                    <div className="min-w-0">
                      <span className="font-mono font-bold block">{issue.path}</span>
                      <span>{issue.message}</span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-end gap-2 p-5 border-t border-slate-100">
          <Button type="button" variant="outline" onClick={onClose} disabled={commitMutation.isPending}>
            Hủy
          </Button>

          <Button
            type="button"
            variant="primary"
            onClick={handleCommit}
            disabled={!isValidated || errorCount > 0 || commitMutation.isPending}
            className="gap-1.5"
          >
            <Check className="w-4 h-4" />
            {commitMutation.isPending ? 'Đang lưu vào bài học...' : `Lưu (${commitMode})`}
          </Button>
        </div>
      </div>
    </div>
  );
};
