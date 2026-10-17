import type { lessons as en } from '../en/lessons'
import type { TranslationOf } from '../../types'

export const lessons: TranslationOf<typeof en> = {
  title: 'Bài học',
  all: 'Tất cả',
  allLevels: 'Mọi trình độ',
  type: {
    Vocabulary: 'Từ vựng',
    Grammar: 'Ngữ pháp',
    DailyPhrase: 'Câu nói hằng ngày',
  },
  level: {
    Beginner: 'Mới bắt đầu',
    Intermediate: 'Trung cấp',
    Advanced: 'Nâng cao',
  },
  loading: 'Đang tải bài học…',
  loadFailed: 'Hiện chưa tải được bài học. Vui lòng thử lại sau một lát.',
  noMatch: 'Chưa có bài học nào khớp với bộ lọc này.',
  detail: {
    loading: 'Đang tải bài học…',
    loadFailed: 'Hiện chưa tải được bài học này.',
    complete: 'Đánh dấu đã xong',
    completed: 'Đã xong! 🎉',
    saving: 'Đang lưu…',
  },
  list: {
    playAudio: 'Nghe âm thanh',
    watchVideo: 'Xem video',
    playPronunciation: 'Nghe cách phát âm của {{word}}',
    awaiting: 'Đang chờ duyệt',
  },
}
