import { describe, it, expect } from 'vitest';
import type { AxiosError } from 'axios';
import { extractApiErrorMessage } from '../httpClient';

describe('extractApiErrorMessage Helper', () => {
  it('handles network errors without response', () => {
    const error = {
      message: 'Network Error',
    } as AxiosError;

    expect(extractApiErrorMessage(error)).toBe(
      'Không thể kết nối đến máy chủ. Vui lòng kiểm tra đường truyền hoặc Backend API.'
    );
  });

  it('handles timeout errors', () => {
    const error = {
      code: 'ECONNABORTED',
      message: 'timeout of 30000ms exceeded',
    } as AxiosError;

    expect(extractApiErrorMessage(error)).toBe(
      'Yêu cầu kết nối quá thời gian chờ (Timeout). Vui lòng thử lại.'
    );
  });

  it('extracts detail from ProblemDetails response', () => {
    const error = {
      response: {
        status: 400,
        data: {
          detail: 'Email đã tồn tại trên hệ thống.',
        },
      },
    } as AxiosError;

    expect(extractApiErrorMessage(error)).toBe('Email đã tồn tại trên hệ thống.');
  });

  it('extracts error from legacy { error: ... } body', () => {
    const error = {
      response: {
        status: 400,
        data: {
          error: 'Tên người dùng đã được sử dụng.',
        },
      },
    } as AxiosError;

    expect(extractApiErrorMessage(error)).toBe('Tên người dùng đã được sử dụng.');
  });

  it('extracts error from ProblemDetails body having both detail and error matching RFC 9457 extension', () => {
    const error = {
      response: {
        status: 400,
        data: {
          type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
          title: 'Bad Request',
          status: 400,
          detail: 'Dữ liệu không hợp lệ.',
          error: 'Dữ liệu không hợp lệ.',
          traceId: '0HN1234567890',
        },
      },
    } as AxiosError;

    expect(extractApiErrorMessage(error)).toBe('Dữ liệu không hợp lệ.');
  });

  it('respects precedence order: detail > error > message > errors > title', () => {
    // 1. detail takes precedence over error
    const errDetailOverError = {
      response: {
        status: 400,
        data: {
          detail: 'Detail message',
          error: 'Error message',
          message: 'Generic message',
        },
      },
    } as AxiosError;
    expect(extractApiErrorMessage(errDetailOverError)).toBe('Detail message');

    // 2. error takes precedence over message
    const errErrorOverMessage = {
      response: {
        status: 400,
        data: {
          error: 'Error message',
          message: 'Generic message',
        },
      },
    } as AxiosError;
    expect(extractApiErrorMessage(errErrorOverMessage)).toBe('Error message');

    // 3. message takes precedence over errors dictionary
    const errMessageOverErrors = {
      response: {
        status: 400,
        data: {
          message: 'Generic message',
          errors: { Field: ['Field error'] },
        },
      },
    } as AxiosError;
    expect(extractApiErrorMessage(errMessageOverErrors)).toBe('Generic message');

    // 4. errors dictionary takes precedence over title
    const errErrorsOverTitle = {
      response: {
        status: 400,
        data: {
          title: 'Validation Failed',
          errors: { Email: ['Email is invalid'] },
        },
      },
    } as AxiosError;
    expect(extractApiErrorMessage(errErrorsOverTitle)).toBe('Email is invalid');
  });

  it('extracts validation errors dictionary from ASP.NET ProblemDetails', () => {
    const error = {
      response: {
        status: 400,
        data: {
          title: 'Validation failed',
          errors: {
            Password: ['Mật khẩu phải chứa ít nhất 1 chữ hoa'],
          },
        },
      },
    } as AxiosError;

    expect(extractApiErrorMessage(error)).toBe('Mật khẩu phải chứa ít nhất 1 chữ hoa');
  });

  it('handles plain string response', () => {
    const error = {
      response: {
        status: 400,
        data: 'Tên người dùng không hợp lệ.',
      },
    } as AxiosError;

    expect(extractApiErrorMessage(error)).toBe('Tên người dùng không hợp lệ.');
  });

  it('falls back to standard status code text when data has no details', () => {
    const error403 = {
      response: {
        status: 403,
        data: {},
      },
    } as AxiosError;
    expect(extractApiErrorMessage(error403)).toBe('Bạn không có quyền thực hiện thao tác này (403 Forbidden).');

    const error404 = {
      response: {
        status: 404,
        data: {},
      },
    } as AxiosError;
    expect(extractApiErrorMessage(error404)).toBe('Không tìm thấy tài nguyên yêu cầu (404 Not Found).');

    const error500 = {
      response: {
        status: 500,
        data: {},
      },
    } as AxiosError;
    expect(extractApiErrorMessage(error500)).toBe('Máy chủ gặp sự cố nội bộ (500 Server Error). Vui lòng thử lại sau.');
  });
});
