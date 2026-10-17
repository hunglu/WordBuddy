import type { profile as en } from '../en/profile'
import type { TranslationOf } from '../../types'

export const profile: TranslationOf<typeof en> = {
  title: 'Hồ sơ của tôi',
  loadFailed: 'Hiện chưa tải được hồ sơ của bạn.',
  form: {
    noAlias: 'Chưa có biệt danh',
    aliasLabel: 'Biệt danh (từ 3 đến 20 ký tự)',
    childHint: 'Hãy chọn một cái tên vui. Đừng dùng tên thật của bạn.',
    avatar: 'Ảnh đại diện',
    avatarsFailed: 'Không tải được ảnh đại diện.',
    saveFailed: 'Chưa lưu được.',
  },
  account: {
    heading: 'Tài khoản',
    email: 'Email',
    ageGroup: 'Nhóm tuổi',
    displayName: 'Tên hiển thị',
    childNote: 'Chỉ mình bạn thấy tên này. Mọi người khác thấy biệt danh của bạn.',
    saveName: 'Lưu tên',
    saveNameFailed: 'Chưa lưu được tên.',
  },
  dob: {
    label: 'Ngày sinh',
    childHint: 'Hãy hỏi người giám hộ nếu bạn chưa nhớ. Người hỗ trợ của bạn có thể thấy ngày này.',
    adultHint: 'Những người hỗ trợ đang liên kết với bạn có thể thấy ngày này. Nó giúp WordBuddy chọn nhịp học phù hợp.',
    save: 'Lưu ngày sinh',
    saveFailed: 'Chưa lưu được ngày sinh.',
  },
  password: {
    heading: 'Đổi mật khẩu',
    current: 'Mật khẩu hiện tại',
    new: 'Mật khẩu mới',
    confirm: 'Nhập lại mật khẩu mới',
    submit: 'Đổi mật khẩu',
    changed: 'Đã đổi mật khẩu.',
    failed: 'Chưa đổi được mật khẩu.',
  },
  timeZone: {
    label: 'Múi giờ',
    hint: 'Thử thách tính ngày của bạn theo múi giờ này.',
    notSet: 'Chưa chọn (UTC)',
    useBrowser: 'Dùng múi giờ của trình duyệt ({{zone}})',
    save: 'Lưu múi giờ',
    failed: 'Chưa lưu được múi giờ.',
  },
  supporters: {
    heading: 'Người hỗ trợ của tôi',
    loadFailed: 'Hiện chưa tải được danh sách người hỗ trợ của bạn.',
    none: 'Chưa có người hỗ trợ.',
    guardian: 'Người giám hộ',
    manage: 'Quản lý người hỗ trợ',
  },
  validation: {
    nameMin: 'Nhập ít nhất 2 ký tự',
    nameMax: 'Nhập tối đa 50 ký tự',
    currentRequired: 'Nhập mật khẩu hiện tại của bạn',
    passwordMin: 'Mật khẩu phải có ít nhất 8 ký tự',
    passwordMax: 'Nhập tối đa 128 ký tự',
    mismatch: 'Mật khẩu nhập lại chưa khớp',
    futureDate: 'Ngày không được ở tương lai',
  },
}
