-- =====================================================================================
-- Seed Data: Thông báo hệ thống thực tế (Notifications)
-- Phân bố cho Admin, Censor, Employers và Candidates
-- =====================================================================================

DECLARE @AdminId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000000');
DECLARE @CensorId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000100');
DECLARE @Employer1Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000010'); -- Highlands Coffee
DECLARE @Employer2Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000020'); -- Circle K
DECLARE @Candidate1Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000001'); -- Nguyễn Hoàng Nam
DECLARE @Candidate2Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000002'); -- Trần Thị Thu Trang

INSERT INTO Notifications (Id, Title, Content, IsRead, CreatedAt, UserId) VALUES
-- =====================================================================================
-- 1. THÔNG BÁO CHO QUẢN TRỊ VIÊN HỆ THỐNG (ADMIN)
-- =====================================================================================
(NEWID(), N'Hệ thống sao lưu dữ liệu tự động', N'Bản sao lưu cơ sở dữ liệu định kỳ hàng ngày đã hoàn tất thành công vào lúc 03:00 UTC.', 1, DATEADD(DAY, -2, GETUTCDATE()), @AdminId),
(NEWID(), N'Doanh thu phát sinh từ đơn hàng mới', N'Nhà tuyển dụng Highlands Coffee Khu CNC Hòa Lạc vừa thanh toán thành công đơn hàng Gói Ultra 1 Tháng qua cổng PayOS (Số tiền: 200.000 VNĐ).', 1, DATEADD(DAY, -5, GETUTCDATE()), @AdminId),
(NEWID(), N'Yêu cầu đăng ký nhà tuyển dụng mới', N'Ứng viên Ngô Thanh Trúc vừa gửi hồ sơ đăng ký doanh nghiệp mới "Cửa hàng Thiết kế Trúc Art". Đang chờ kiểm duyệt viên thẩm định.', 0, DATEADD(HOUR, -18, GETUTCDATE()), @AdminId),
(NEWID(), N'Báo cáo vi phạm việc làm cần theo dõi', N'Ứng viên vừa gửi phản ánh việc làm có dấu hiệu lừa đảo tuyển dụng tài chính tại địa bàn Ba Đình. Censor đang thụ lý giải quyết.', 0, DATEADD(DAY, -1, GETUTCDATE()), @AdminId),
(NEWID(), N'Cập nhật chính sách dịch vụ', N'Đã cập nhật biểu phí các gói dịch vụ nhà tuyển dụng Plus, Pro, Ultra áp dụng cho quý mới.', 1, DATEADD(DAY, -7, GETUTCDATE()), @AdminId),

-- =====================================================================================
-- 2. THÔNG BÁO CHO KIỂM DUYỆT VIÊN (CENSOR)
-- =====================================================================================
(NEWID(), N'Yêu cầu xét duyệt hồ sơ doanh nghiệp mới', N'Ứng viên Ngô Thanh Trúc đã nộp hồ sơ xin cấp quyền Nhà tuyển dụng. Vui lòng kiểm tra mã số thuế, CCCD và file giấy phép kinh doanh đính kèm.', 0, DATEADD(HOUR, -18, GETUTCDATE()), @CensorId),
(NEWID(), N'Báo cáo vi phạm tin tuyển dụng mới', N'Ứng viên Trần Thị Thu Trang gửi báo cáo vi phạm đối với tin đăng "Nhân viên Telesales tài chính". Lý do: Yêu cầu nạp tiền tham gia sàn ảo trái phép.', 0, DATEADD(DAY, -1, GETUTCDATE()), @CensorId),
(NEWID(), N'Đã phê duyệt xử lý báo cáo vi phạm', N'Bạn đã đánh dấu giải quyết báo cáo #RP-002 và tạm ẩn tin đăng vi phạm quy chế lao động của Nhà hàng lẩu nướng.', 1, DATEADD(DAY, -4, GETUTCDATE()), @CensorId),
(NEWID(), N'Từ chối hồ sơ doanh nghiệp không hợp lệ', N'Bạn đã từ chối yêu cầu cấp quyền NTD của Đỗ Quốc Cường do mã số thuế không tồn tại trên Cổng thông tin Quốc gia.', 1, DATEADD(DAY, -2, GETUTCDATE()), @CensorId),

-- =====================================================================================
-- 3. THÔNG BÁO CHO NHÀ TUYỂN DỤNG HIGHLANDS COFFEE (0000000010)
-- =====================================================================================
(NEWID(), N'Ứng viên mới nộp hồ sơ', N'Ứng viên Nguyễn Hoàng Nam vừa nộp đơn ứng tuyển vào vị trí "Nhân viên Barista pha chế ca linh hoạt". Hãy xem hồ sơ ngay!', 0, DATEADD(HOUR, -3, GETUTCDATE()), @Employer1Id),
(NEWID(), N'Kích hoạt gói dịch vụ Ultra thành công', N'Đơn hàng mua Gói Ultra (1 Tháng) đã thanh toán thành công. Tài khoản của bạn được cộng 20 tin đăng hoạt động và 50 lượt đẩy tin miễn phí.', 1, DATEADD(DAY, -5, GETUTCDATE()), @Employer1Id),
(NEWID(), N'Đẩy tin tuyển dụng thành công', N'Tin tuyển dụng "Nhân viên phục vụ bàn ca sáng" của bạn vừa được đẩy lên vị trí nổi bật trên trang chủ.', 1, DATEADD(DAY, -2, GETUTCDATE()), @Employer1Id),
(NEWID(), N'Cảnh báo tin tuyển dụng sắp hết hạn', N'Tin tuyển dụng "Nhân viên thu ngân quầy F&B" sẽ hết hạn trong 2 ngày tới. Vui lòng gia hạn để duy trì hiển thị.', 0, DATEADD(DAY, -1, GETUTCDATE()), @Employer1Id),
(NEWID(), N'Gợi ý ứng viên tiềm năng', N'Hệ thống AI vừa tìm thấy 3 ứng viên có kỹ năng pha chế và lịch rảnh phù hợp với tin tuyển dụng của bạn tại khu vực Hòa Lạc.', 1, DATEADD(DAY, -3, GETUTCDATE()), @Employer1Id),

-- =====================================================================================
-- 4. THÔNG BÁO CHO NHÀ TUYỂN DỤNG CIRCLE K (0000000020)
-- =====================================================================================
(NEWID(), N'Ứng viên mới nộp hồ sơ', N'Ứng viên Đặng Văn Long vừa ứng tuyển vào vị trí "Nhân viên sắp xếp và kiểm kê kệ hàng tiện lợi ca sáng".', 0, DATEADD(DAY, -1, GETUTCDATE()), @Employer2Id),
(NEWID(), N'Giao dịch thanh toán hết hạn', N'Giao dịch thanh toán #PAYOS-ORD-20261025-003 cho đơn hàng gói Pro đã hết hiệu lực 15 phút. Bạn có thể bấm tạo giao dịch mới trong trang Chi tiết đơn hàng.', 0, DATEADD(MINUTE, -30, GETUTCDATE()), @Employer2Id),
(NEWID(), N'Kích hoạt gói Pro thành công', N'Gói Pro (1 Tháng) của bạn còn 14 ngày sử dụng. Bạn còn 8 lượt đẩy tin miễn phí.', 1, DATEADD(DAY, -16, GETUTCDATE()), @Employer2Id),

-- =====================================================================================
-- 5. THÔNG BÁO CHO ỨNG VIÊN NGUYỄN HOÀNG NAM (0000000001)
-- =====================================================================================
(NEWID(), N'Hồ sơ ứng tuyển được chấp nhận! 🎉', N'Chúc mừng Nam! Đơn ứng tuyển vị trí "Nhân viên Barista pha chế ca linh hoạt" tại Highlands Coffee Hòa Lạc đã được nhà tuyển dụng chấp nhận. Quản lý sẽ gọi điện xếp lịch phỏng vấn.', 0, DATEADD(HOUR, -2, GETUTCDATE()), @Candidate1Id),
(NEWID(), N'Phản hồi từ Nhà tuyển dụng', N'Đơn ứng tuyển vị trí "Nhân viên kho part-time" tại Canifa chưa phù hợp với đợt tuyển dụng này do đã tuyển đủ chỉ tiêu.', 1, DATEADD(DAY, -3, GETUTCDATE()), @Candidate1Id),
(NEWID(), N'Việc làm mới phù hợp với bạn', N'Có 2 việc làm F&B ca chiều mới đăng tại khu vực Hòa Lạc trùng khớp với lịch rảnh của bạn. Hãy khám phá ngay!', 1, DATEADD(DAY, -4, GETUTCDATE()), @Candidate1Id),
(NEWID(), N'Cập nhật trạng thái tìm việc', N'Trạng thái "Bật tìm việc" của bạn sẽ hết hạn sau 5 ngày nữa. Đừng quên gia hạn để tiếp tục được các nhà tuyển dụng tìm thấy.', 0, DATEADD(DAY, -1, GETUTCDATE()), @Candidate1Id),

-- =====================================================================================
-- 6. THÔNG BÁO CHO ỨNG VIÊN TRẦN THỊ THU TRANG (0000000002)
-- =====================================================================================
(NEWID(), N'Hồ sơ ứng tuyển được chấp nhận! 🎉', N'Chúc mừng Trang! Đơn ứng tuyển vị trí "Trợ giảng Tiếng Anh lớp IELTS Foundation" tại The IELTS Workshop đã được chấp nhận. Vui lòng kiểm tra email để nhận bài kiểm tra năng lực.', 0, DATEADD(DAY, -1, GETUTCDATE()), @Candidate2Id),
(NEWID(), N'Báo cáo vi phạm đã được xử lý', N'Cảm ơn bạn đã đóng góp xây dựng môi trường việc làm an toàn. Báo cáo về tin tuyển dụng lừa đảo tài chính đã được ban kiểm duyệt xử lý và gỡ bỏ.', 1, DATEADD(HOUR, -6, GETUTCDATE()), @Candidate2Id),
(NEWID(), N'Việc làm gia sư mới đăng', N'Trung tâm gia sư vừa đăng tin tuyển "Gia sư Tiếng Anh cho học sinh THCS" tại Cầu Giấy với mức lương 70.000đ/giờ.', 1, DATEADD(DAY, -3, GETUTCDATE()), @Candidate2Id);