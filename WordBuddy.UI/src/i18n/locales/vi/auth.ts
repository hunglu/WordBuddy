import type { auth as en } from '../en/auth'
import type { TranslationOf } from '../../types'

export const auth: TranslationOf<typeof en> = {
  login: {
    welcome: 'Chào mừng bạn quay lại!',
    email: 'Email',
    password: 'Mật khẩu',
    submit: 'Đăng nhập',
    submitting: 'Đang đăng nhập…',
    failed: 'Email hoặc mật khẩu chưa đúng.',
    newHere: 'Bạn mới đến?',
    createAccount: 'Tạo tài khoản',
  },
  register: {
    join: 'Tham gia WordBuddy!',
    name: 'Tên',
    email: 'Email',
    password: 'Mật khẩu',
    confirmPassword: 'Nhập lại mật khẩu',
    iAm: 'Tôi là…',
    child: 'Trẻ em',
    adult: 'Người lớn',
    submit: 'Tạo tài khoản',
    submitting: 'Đang tạo tài khoản…',
    failed: 'Không tạo được tài khoản của bạn.',
    haveAccount: 'Đã có tài khoản?',
    logIn: 'Đăng nhập',
  },
  validation: {
    emailInvalid: 'Nhập một địa chỉ email hợp lệ',
    passwordRequired: 'Cần nhập mật khẩu',
    displayNameRequired: 'Cần nhập tên hiển thị',
    passwordMin: 'Mật khẩu phải có ít nhất 8 ký tự',
    passwordMismatch: 'Mật khẩu nhập lại chưa khớp',
    chooseOne: 'Hãy chọn một',
  },
}
