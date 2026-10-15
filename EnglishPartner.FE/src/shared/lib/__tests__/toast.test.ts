import { describe, it, expect, beforeEach, vi } from 'vitest';
import { toast, useToastStore } from '../toast';

describe('Global Toast & Toaster Store', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    useToastStore.getState().clearToasts();
  });

  it('adds success, error, warning, info toasts', () => {
    const sId = toast.success('Thành công', { title: 'Chúc mừng' });
    const eId = toast.error('Có lỗi xảy ra');
    const wId = toast.warning('Cảnh báo');
    const iId = toast.info('Thông tin');

    const toasts = useToastStore.getState().toasts;
    expect(toasts).toHaveLength(4);

    expect(toasts.find((t) => t.id === sId)?.type).toBe('success');
    expect(toasts.find((t) => t.id === sId)?.title).toBe('Chúc mừng');
    expect(toasts.find((t) => t.id === eId)?.type).toBe('error');
    expect(toasts.find((t) => t.id === wId)?.type).toBe('warning');
    expect(toasts.find((t) => t.id === iId)?.type).toBe('info');
  });

  it('removes a toast when dismiss is called with id', () => {
    const id1 = toast.success('Toast 1');
    const id2 = toast.success('Toast 2');

    expect(useToastStore.getState().toasts).toHaveLength(2);

    toast.dismiss(id1);
    expect(useToastStore.getState().toasts).toHaveLength(1);
    expect(useToastStore.getState().toasts[0].id).toBe(id2);
  });

  it('clears all toasts when dismiss is called without arguments', () => {
    toast.success('Toast 1');
    toast.error('Toast 2');
    toast.info('Toast 3');

    expect(useToastStore.getState().toasts).toHaveLength(3);

    toast.dismiss();
    expect(useToastStore.getState().toasts).toHaveLength(0);
  });

  it('auto-dismisses toast after specified duration', () => {
    toast.success('Auto dismiss test', { duration: 2000 });
    expect(useToastStore.getState().toasts).toHaveLength(1);

    vi.advanceTimersByTime(1999);
    expect(useToastStore.getState().toasts).toHaveLength(1);

    vi.advanceTimersByTime(1);
    expect(useToastStore.getState().toasts).toHaveLength(0);
  });

  it('handles toast.promise lifecycle', async () => {
    const fakeAsync = () =>
      new Promise<string>((resolve) => {
        setTimeout(() => resolve('Xong rồi'), 500);
      });

    const promise = toast.promise(fakeAsync(), {
      loading: 'Đang tải...',
      success: (res) => `Kết quả: ${res}`,
      error: 'Lỗi',
    });

    const initial = useToastStore.getState().toasts[0];
    expect(initial.type).toBe('loading');
    expect(initial.message).toBe('Đang tải...');

    vi.advanceTimersByTime(500);
    const result = await promise;
    expect(result).toBe('Xong rồi');

    const finished = useToastStore.getState().toasts[0];
    expect(finished.type).toBe('success');
    expect(finished.message).toBe('Kết quả: Xong rồi');
  });
});
