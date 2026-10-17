import type { review as en } from '../en/review'
import type { TranslationOf } from '../../types'

export const review: TranslationOf<typeof en> = {
  title: 'Ôn tập',
  loadFailed: 'Hiện chưa tải được bài ôn tập của bạn.',
  tryAgain: 'Thử lại',
  preparing: 'Đang chuẩn bị các từ hôm nay…',
  nothing: 'Hôm nay không có gì để ôn. Hãy thêm vài từ vào danh sách của bạn!',
  backToVocabulary: 'Quay lại Từ vựng của tôi',
  progress: 'Còn {{left}} từ · Đúng {{correct}} từ',
  nearlyDone: 'Sắp xong rồi — thêm vài từ nữa thôi!',
  wordLoadFailed: 'Chưa tải được từ này.',
  skip: 'Bỏ qua từ này',
  wordPreparing: 'Đang chuẩn bị từ cho bạn…',
  correct: 'Đúng rồi!',
  wrong: 'Đáp án là "{{answer}}".',
  next: 'Tiếp',
  saveFailed: 'Chưa lưu được câu trả lời này.',
  picture: {
    question: 'Từ nào hợp với bức ảnh?',
    alt: 'Đây là gì?',
  },
  listening: {
    question: 'Nghe và chọn từ bạn nghe được.',
    play: '🔊 Nghe',
  },
  typing: {
    question: 'Gõ từ có nghĩa này.',
    startsWith: 'Bắt đầu bằng chữ',
    answerLabel: 'Câu trả lời của bạn',
    check: 'Kiểm tra',
  },
  hint: 'Xem gợi ý',
  summary: {
    done: 'Ôn tập xong rồi!',
    timeUp: 'Hết giờ rồi — hôm nay bạn làm tốt lắm!',
    result: 'Bạn đã trả lời {{answered}} câu và đúng {{correct}} câu.',
    rest: 'Phần còn lại để lần sau nhé.',
    viewProgress: 'Xem tiến độ',
  },
}
