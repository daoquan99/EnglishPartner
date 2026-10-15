import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

const resources = {
  vi: {
    translation: {
      appTitle: 'LingoMint AI Tutor',
      studentPortal: 'Cổng Học Viên',
      adminPortal: 'Cổng Quản Trị (Admin)',
      liveTutorNav: 'Gia Sư Luyện Nói Live',
      dailyVocabNav: 'Từ Vựng & Chấm Phát Âm',
      login: 'Đăng nhập',
      register: 'Đăng ký',
      logout: 'Đăng xuất',
      cefrLevel: 'Trình độ CEFR',
      conversationMode: 'Chế độ luyện nói',
      teacherVoice: 'Giọng nói của Teacher',
      speechSpeed: 'Tốc độ nói',
      conversationTopic: 'Chủ đề cuộc trò chuyện (Topic)',
      vocabTopic: 'Chủ đề tạo thêm từ',
      vocabAiModel: 'Model Tạo Từ Vựng AI (Gemini & Gemma 4)',
      pronunciationAiModel: 'Model Chấm Phát Âm Audio (Chỉ nhận Audio Input)',
    },
  },
  en: {
    translation: {
      appTitle: 'LingoMint AI Tutor',
      studentPortal: 'Student Portal',
      adminPortal: 'Admin Portal',
      liveTutorNav: 'Live Speaking Tutor',
      dailyVocabNav: 'Daily Vocabulary & Pronunciation',
      login: 'Sign In',
      register: 'Create Account',
      logout: 'Sign Out',
      cefrLevel: 'CEFR Level',
      conversationMode: 'Speaking Mode',
      teacherVoice: 'Teacher Voice',
      speechSpeed: 'Speech Speed',
      conversationTopic: 'Conversation Topic',
      vocabTopic: 'Vocabulary Generation Topic',
      vocabAiModel: 'AI Vocabulary Model (Gemini & Gemma 4)',
      pronunciationAiModel: 'Audio Pronunciation Model (Audio Input Only)',
    },
  },
};

void i18n.use(initReactI18next).init({
  resources,
  lng: 'vi',
  fallbackLng: 'en',
  interpolation: {
    escapeValue: false,
  },
});

export default i18n;
