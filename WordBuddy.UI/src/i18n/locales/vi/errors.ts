import type { errors as en } from '../en/errors'
import type { TranslationOf } from '../../types'

export const errors: TranslationOf<typeof en> = {
  codes: {
    Login: {
      InvalidCredentials: 'Email hoặc mật khẩu chưa đúng.',
    },
    RegisterUser: {
      EmailTaken: 'Email này đã được đăng ký.',
    },
    User: {
      NotFound: 'Không tìm thấy người dùng.',
      AliasLength: 'Biệt danh phải dài từ 3 đến 20 ký tự.',
      AliasRevealsIdentity: 'Biệt danh của trẻ không được trùng với tên hiển thị hoặc phần tên trong email.',
      AliasTaken: 'Biệt danh này đã có người dùng.',
      UnknownAvatar: 'Ảnh đại diện này không có trong danh mục.',
      UnknownTimeZone: 'Múi giờ này không hợp lệ.',
      DateOfBirthInvalid: 'Ngày sinh không hợp lệ.',
      DateOfBirthAgeGroupMismatch: 'Ngày sinh không khớp với nhóm tuổi của tài khoản.',
      DisplayNameLength: 'Tên hiển thị phải dài từ 2 đến 50 ký tự.',
      WrongCurrentPassword: 'Mật khẩu hiện tại chưa đúng.',
      PasswordTooWeak: 'Mật khẩu mới không hợp lệ.',
      InvalidLanguage: 'Ngôn ngữ không hợp lệ.',
    },
    UpdatePreferredLanguage: {
      Validation: 'Ngôn ngữ không hợp lệ.',
    },
  },
  generic: 'Có lỗi xảy ra. Vui lòng thử lại.',
  noAccess: 'Bạn không có quyền xem trang này.',
}
