import React, { useState } from 'react';
import { X, Copy, Check, Sparkles, BookOpen } from 'lucide-react';
import { Button } from '@/shared/components/ui/Button';
import { toast } from '@/shared/lib/toast';

interface ExternalPromptDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  lessonMetadata: {
    title: string;
    cefrLevel: string;
    topic: string;
    targetVocabulary?: string[];
    targetGrammar?: string[];
  };
}

export const ExternalPromptDrawer: React.FC<ExternalPromptDrawerProps> = ({
  isOpen,
  onClose,
  lessonMetadata,
}) => {
  const [copied, setCopied] = useState(false);

  if (!isOpen) return null;

  const generatedPrompt = `Bạn là chuyên gia soạn thảo giáo án tiếng Anh chuyên nghiệp theo chuẩn CEFR và hợp đồng bài tập Lesson Exercises v1.
Hãy tạo một tập bài tập chất lượng cao cho bài học với thông tin sau:
- Tiêu đề bài học: "${lessonMetadata.title}"
- Trình độ CEFR: ${lessonMetadata.cefrLevel}
- Chủ đề (Topic): ${lessonMetadata.topic}
- Từ vựng trọng tâm: ${lessonMetadata.targetVocabulary?.join(', ') || 'Theo ngữ cảnh chủ đề'}
- Ngữ pháp trọng tâm: ${lessonMetadata.targetGrammar?.join(', ') || 'Theo ngữ cảnh chủ đề'}

Yêu cầu định dạng JSON đầu ra duy nhất (không kèm markdown ngoài khối JSON):
{
  "lesson": {
    "title": "${lessonMetadata.title}",
    "cefrLevel": "${lessonMetadata.cefrLevel}",
    "topic": "${lessonMetadata.topic}",
    "exercises": [
      {
        "key": "vocab_flashcards",
        "type": "Flashcards",
        "title": "Từ vựng trọng tâm",
        "instructions": "Lật thẻ và tự đánh giá khả năng ghi nhớ từ vựng.",
        "maxPoints": 0,
        "gradingMode": "SelfAssessment",
        "content": {
          "cards": [
            { "id": "c1", "term": "...", "ipa": "...", "meaningVi": "...", "exampleEn": "...", "exampleVi": "..." }
          ]
        }
      },
      {
        "key": "restaurant_mcq",
        "type": "MultipleChoice",
        "title": "Trắc nghiệm kiểm tra",
        "instructions": "Chọn phương án đúng nhất.",
        "maxPoints": 10,
        "gradingMode": "Deterministic",
        "content": {
          "question": "...",
          "options": [
            { "id": "opt_1", "text": "..." },
            { "id": "opt_2", "text": "..." }
          ]
        },
        "grading": {
          "correctOptionIds": ["opt_1"],
          "explanation": "..."
        }
      }
    ]
  }
}`;

  const handleCopy = () => {
    void navigator.clipboard.writeText(generatedPrompt);
    setCopied(true);
    toast.success('Đã sao chép prompt vào bộ nhớ tạm!');
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 p-4 overflow-y-auto">
      <div className="bg-white rounded-3xl border border-slate-200 shadow-2xl w-full max-w-2xl max-h-[90vh] flex flex-col my-8">
        <div className="flex items-center justify-between p-5 border-b border-slate-100">
          <div>
            <h3 className="text-base font-bold text-slate-900 flex items-center gap-2">
              <Sparkles className="w-5 h-5 text-amber-500" />
              Prompt tạo bài học bằng AI bên ngoài
            </h3>
            <p className="text-xs text-slate-500">
              Sao chép prompt này và dán vào ChatGPT, Claude hoặc Gemini (không cần API key).
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

        <div className="flex-1 overflow-y-auto p-5 space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-slate-700">Mẫu Prompt chuẩn định dạng JSON:</span>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={handleCopy}
              className="gap-1.5 text-xs text-slate-700 border-slate-200 hover:border-emerald-500 hover:text-emerald-700 hover:bg-emerald-50"
            >
              {copied ? <Check className="w-4 h-4 text-emerald-600" /> : <Copy className="w-4 h-4" />}
              {copied ? 'Đã sao chép!' : 'Sao chép Prompt'}
            </Button>
          </div>

          <textarea
            readOnly
            rows={14}
            value={generatedPrompt}
            className="w-full text-xs font-mono bg-slate-900 text-slate-200 p-4 rounded-2xl border border-slate-800 select-all leading-relaxed"
          />

          <div className="p-3 bg-amber-50 rounded-xl border border-amber-200 text-xs text-amber-900 space-y-1">
            <span className="font-bold block">Hướng dẫn các bước tiếp theo:</span>
            <ol className="list-decimal list-inside space-y-0.5 text-amber-800">
              <li>Sao chép prompt ở trên và dán vào mô hình AI yêu thích của bạn.</li>
              <li>Nhận kết quả JSON từ AI và sao chép toàn bộ khối JSON.</li>
              <li>Mở nút &quot;Nhập từ JSON&quot; trong bài học này để kiểm tra schema và lưu trực tiếp.</li>
            </ol>
          </div>
        </div>

        <div className="flex items-center justify-end p-5 border-t border-slate-100">
          <Button type="button" variant="primary" onClick={onClose}>
            Đóng
          </Button>
        </div>
      </div>
    </div>
  );
};
