import type { common as en } from '../en/common'
import type { TranslationOf } from '../../types'

export const common: TranslationOf<typeof en> = {
  loading: 'Đang tải…',
  save: 'Lưu',
  saving: 'Đang lưu…',
  cancel: 'Hủy',
  delete: 'Xóa',
  deleting: 'Đang xóa…',
  edit: 'Sửa',
  close: 'Đóng',
  back: 'Quay lại',
  create: 'Tạo',
  add: 'Thêm',
  remove: 'Bỏ',
  retry: 'Thử lại',
  saved: 'Đã lưu.',
  ageGroup: { Child: 'Trẻ em', Adult: 'Người lớn' },
  genericError: 'Có lỗi xảy ra. Vui lòng thử lại.',
  language: {
    label: 'Ngôn ngữ',
    saveFailed: 'Ngôn ngữ đã đổi trên thiết bị này, nhưng chưa lưu được vào hồ sơ của bạn.',
  },
  pager: {
    previous: 'Trước',
    next: 'Sau',
    pageOf: 'Trang {{page}} / {{total}} · {{count}} {{items}}',
    items: 'mục',
  },
  supporterGate: {
    title: 'Thêm người hỗ trợ',
    body: 'Trước khi học, bạn cần một người lớn đồng hành. Hãy tạo một mã và gửi cho họ.',
    action: 'Thêm người hỗ trợ',
  },
}
