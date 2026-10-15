import React, { useState } from 'react';
import {
  History,
  Play,
  MessageSquare,
  MessageSquarePlus,
  Clock,
  Volume2,
  Sparkles,
  ArrowLeft,
} from 'lucide-react';
import { Dialog } from '@/shared/components/ui/Dialog';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog';
import { IconButton } from '@/shared/components/ui/IconButton';
import { Button } from '@/shared/components/ui/Button';
import { Badge } from '@/shared/components/ui/Badge';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import { conversationsApi } from '../api/conversationsApi';
import type { ConversationDetailDto, ConversationSummaryDto } from '../types/tutor.types';

export interface ConversationHistoryModalProps {
  isOpen?: boolean;
  open?: boolean;
  onClose: () => void;
  conversations: ConversationSummaryDto[];
  activeConversationId?: string | null;
  onResumeConversation?: (detail: ConversationDetailDto) => void;
  onSelectAndResume?: (detail: ConversationDetailDto) => void;
  onStartNewConversation: () => void;
  onDeleteConversation: (id: string) => Promise<void> | void;
}

export const ConversationHistoryModal: React.FC<ConversationHistoryModalProps> = ({
  isOpen,
  open,
  onClose,
  conversations,
  activeConversationId,
  onResumeConversation,
  onSelectAndResume,
  onStartNewConversation,
  onDeleteConversation,
}) => {
  const visible = Boolean(isOpen ?? open);
  const handleResumeCallback = onResumeConversation ?? onSelectAndResume;

  const [selectedDetail, setSelectedDetail] = useState<ConversationDetailDto | null>(null);
  const [loadingDetailId, setLoadingDetailId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ConversationSummaryDto | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const handleViewDetail = async (conv: ConversationSummaryDto) => {
    setLoadingDetailId(conv.id);
    try {
      const detail = await conversationsApi.getConversationDetail(conv.id);
      if (detail) {
        setSelectedDetail(detail);
      }
    } catch {
      setSelectedDetail({
        ...conv,
        messages: [],
        corrections: [],
      });
    } finally {
      setLoadingDetailId(null);
    }
  };

  const handleResume = async (conv: ConversationSummaryDto) => {
    setLoadingDetailId(conv.id);
    try {
      const detail = await conversationsApi.getConversationDetail(conv.id);
      if (detail) {
        handleResumeCallback?.(detail);
        onClose();
        return;
      }
    } catch {
      // ignore
    } finally {
      setLoadingDetailId(null);
    }
    handleResumeCallback?.({
      ...conv,
      messages: [],
      corrections: [],
    });
    onClose();
  };

  const handleConfirmDelete = async () => {
    if (!deleteTarget) return;
    setIsDeleting(true);
    try {
      await onDeleteConversation(deleteTarget.id);
      if (selectedDetail?.id === deleteTarget.id) {
        setSelectedDetail(null);
      }
    } finally {
      setIsDeleting(false);
      setDeleteTarget(null);
    }
  };

  return (
    <>
      <Dialog
        isOpen={visible}
        onClose={() => {
          setSelectedDetail(null);
          onClose();
        }}
        maxWidth="4xl"
        icon={<History className="w-5 h-5" />}
        title={
          selectedDetail
            ? `Chi tiết Hội thoại: ${selectedDetail.title}`
            : `Lịch sử Cuộc trò chuyện (${conversations.length})`
        }
        description="Xem lại chi tiết nội dung song ngữ, chuyển sang cuộc hội thoại mới, tiếp tục phiên cũ hoặc xóa lịch sử"
        footer={
          <div className="flex items-center justify-between w-full flex-wrap gap-2">
            {selectedDetail ? (
              <>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setSelectedDetail(null)}
                >
                  <ArrowLeft className="w-4 h-4" />
                  Quay lại danh sách
                </Button>
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      setSelectedDetail(null);
                      onStartNewConversation();
                      onClose();
                    }}
                  >
                    <MessageSquarePlus className="w-4 h-4 text-emerald-700" />
                    Tạo cuộc hội thoại mới
                  </Button>
                  <Button
                    variant="primary"
                    size="sm"
                    onClick={() => {
                      handleResumeCallback?.(selectedDetail);
                      setSelectedDetail(null);
                      onClose();
                    }}
                  >
                    <Play className="w-4 h-4" />
                    Tiếp tục phiên hội thoại này
                  </Button>
                </div>
              </>
            ) : (
              <>
                <span className="text-xs text-slate-500">
                  Tổng cộng <strong>{conversations.length}</strong> phiên hội thoại đã lưu
                </span>
                <div className="flex items-center gap-2">
                  <Button
                    variant="primary"
                    size="sm"
                    onClick={() => {
                      onStartNewConversation();
                      onClose();
                    }}
                  >
                    <MessageSquarePlus className="w-4 h-4" />
                    Tạo cuộc hội thoại mới
                  </Button>
                  <Button variant="outline" size="sm" onClick={onClose}>
                    Đóng (ESC)
                  </Button>
                </div>
              </>
            )}
          </div>
        }
      >
        {selectedDetail ? (
          <div className="space-y-4">
            <div className="p-4 rounded-2xl bg-slate-50 border border-slate-200 flex flex-wrap items-center justify-between gap-2 text-xs">
              <div className="flex items-center gap-2 flex-wrap">
                <Badge variant="emerald">CEFR {selectedDetail.cefrLevel}</Badge>
                <Badge variant="sky">{selectedDetail.conversationMode}</Badge>
                <Badge variant="violet">Voice: {selectedDetail.voiceName}</Badge>
                <Badge variant="amber">{selectedDetail.speechSpeed.toFixed(2)}x</Badge>
                {selectedDetail.topic && (
                  <span className="font-semibold text-slate-700">
                    Chủ đề: {selectedDetail.topic}
                  </span>
                )}
              </div>
              <span className="text-slate-500">
                {new Date(selectedDetail.updatedAtUtc || selectedDetail.createdAtUtc).toLocaleString(
                  'vi-VN'
                )}
              </span>
            </div>

            {selectedDetail.messages.length === 0 ? (
              <EmptyState
                title="Phiên hội thoại này chưa có lượt tin nhắn nào"
                description="Nhấn 'Tiếp tục phiên hội thoại này' ở góc dưới để bắt đầu nói chuyện."
              />
            ) : (
              <div className="space-y-3 max-h-[50vh] overflow-y-auto pr-1">
                {selectedDetail.messages.map((m) => (
                  <div
                    key={m.id || m.turnId}
                    className={`p-3.5 rounded-2xl border text-xs space-y-1 ${
                      m.speaker === 'user'
                        ? 'bg-emerald-50/70 border-emerald-200 ml-8'
                        : 'bg-white border-slate-200 mr-8'
                    }`}
                  >
                    <div className="flex items-center justify-between text-[11px] font-bold text-slate-500">
                      <span>{m.speaker === 'user' ? 'Bạn (Học viên)' : 'AI Teacher'}</span>
                      <span>{new Date(m.createdAtUtc).toLocaleTimeString('vi-VN')}</span>
                    </div>
                    <p className="font-bold text-slate-900 text-sm">{m.transcriptLine1}</p>
                    {m.translationLine2 && (
                      <p className="text-slate-600">{m.translationLine2}</p>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        ) : conversations.length === 0 ? (
          <EmptyState
            title="Chưa có lịch sử cuộc trò chuyện nào"
            description="Khi bạn bắt đầu luyện nói cùng Gia Sư AI, toàn bộ nội dung hội thoại song ngữ và sửa lỗi ngữ pháp sẽ được lưu tự động tại đây."
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3.5">
            {conversations.map((conv) => {
              const isCurrentActive = activeConversationId === conv.id;
              return (
                <div
                  key={conv.id}
                  className={`p-4 rounded-2xl border transition-all flex flex-col justify-between gap-3 shadow-2xs ${
                    isCurrentActive
                      ? 'border-emerald-500 bg-emerald-50/30 ring-1 ring-emerald-400/50'
                      : 'border-slate-200 hover:border-emerald-400 bg-white'
                  }`}
                >
                  <div className="space-y-2">
                    <div className="flex items-start justify-between gap-2">
                      <h3 className="text-sm font-extrabold text-slate-900 line-clamp-1">
                        {conv.title || `${conv.conversationMode} (${conv.cefrLevel})`}
                      </h3>
                      <div className="flex items-center gap-1 shrink-0">
                        {isCurrentActive && <Badge variant="emerald">Đang mở</Badge>}
                        <Badge variant="emerald">CEFR {conv.cefrLevel}</Badge>
                      </div>
                    </div>

                    <div className="flex items-center gap-1.5 flex-wrap">
                      <Badge variant="sky">{conv.conversationMode}</Badge>
                      <Badge variant="violet">
                        <Volume2 className="w-3 h-3" /> {conv.voiceName}
                      </Badge>
                      {conv.topic && (
                        <span className="text-[11px] text-slate-600 bg-slate-100 px-2 py-0.5 rounded-lg truncate max-w-[200px]">
                          {conv.topic}
                        </span>
                      )}
                    </div>

                    {conv.lastMessagePreview && (
                      <p className="text-xs text-slate-500 line-clamp-2 italic">
                        &ldquo;{conv.lastMessagePreview}&rdquo;
                      </p>
                    )}
                  </div>

                  <div className="pt-2.5 border-t border-slate-100 flex items-center justify-between gap-2">
                    <div className="flex items-center gap-3 text-[11px] text-slate-500 font-medium">
                      <span className="inline-flex items-center gap-1">
                        <MessageSquare className="w-3.5 h-3.5 text-emerald-600" />
                        {conv.messageCount} lượt
                      </span>
                      <span className="inline-flex items-center gap-1">
                        <Sparkles className="w-3.5 h-3.5 text-amber-500" />
                        {conv.correctionCount} sửa lỗi
                      </span>
                      <span className="inline-flex items-center gap-1">
                        <Clock className="w-3.5 h-3.5 text-slate-400" />
                        {new Date(conv.updatedAtUtc || conv.createdAtUtc).toLocaleDateString('vi-VN')}
                      </span>
                    </div>

                    <div className="flex items-center gap-1.5">
                      <IconButton
                        action="view"
                        size="sm"
                        label="Xem chi tiết hội thoại"
                        disabled={loadingDetailId === conv.id}
                        onClick={() => void handleViewDetail(conv)}
                      />
                      <Button
                        variant="primary"
                        size="sm"
                        disabled={loadingDetailId === conv.id}
                        onClick={() => void handleResume(conv)}
                      >
                        <Play className="w-3.5 h-3.5" />
                        Tiếp tục
                      </Button>
                      <IconButton
                        action="delete"
                        size="sm"
                        label={`Xóa hội thoại ${conv.title}`}
                        onClick={() => setDeleteTarget(conv)}
                      />
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </Dialog>

      <ConfirmDialog
        isOpen={Boolean(deleteTarget)}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => void handleConfirmDelete()}
        isLoading={isDeleting}
        title="Xóa lịch sử cuộc trò chuyện"
        targetName={deleteTarget?.title || 'Phiên hội thoại đã chọn'}
        description="Toàn bộ tin nhắn song ngữ và lịch sử sửa lỗi ngữ pháp của phiên này sẽ bị xóa khỏi danh sách."
      />
    </>
  );
};
