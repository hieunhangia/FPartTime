-- =====================================================================================
-- Seed Data: Sample Jobs, Industries, Salaries, Schedules, Applications, and Reports
-- Target Employers:
--   '0000000010' (Highlands Coffee Hòa Lạc)
--   '0000000020' (Circle K Cầu Giấy & Đống Đa)
--   '0000000030' (The IELTS Workshop)
--   '0000000040' (Golden Gate Ba Đình)
--   '0000000050' (Canifa Thanh Xuân)
--   '0000000060' (Ánh Dương Event)
--   '0000000070' (Cozy Book Cafe)
-- =====================================================================================

DECLARE @EmpHighlands UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000010');
DECLARE @EmpCircleK UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000020');
DECLARE @EmpIelts UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000030');
DECLARE @EmpGoldenGate UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000040');
DECLARE @EmpCanifa UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000050');
DECLARE @EmpAnhDuong UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000060');
DECLARE @EmpCozyBook UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000070');

DECLARE @CandNam UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000001');
DECLARE @CandTrang UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000002');
DECLARE @CandDuc UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000003');
DECLARE @CandLinh UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000004');
DECLARE @CandLong UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000005');

DECLARE @CensorKien UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000100');

-- =====================================================================================
-- 1. NẠP 18 NGÀNH NGHỀ PHỔ BIẾN (INDUSTRIES)
-- =====================================================================================
IF NOT EXISTS (SELECT 1 FROM Industries WHERE Id = '11111111-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO Industries (Id, Name, IsActive)
    VALUES ('11111111-0000-0000-0000-000000000001', N'F&B - Phục vụ & Pha chế', 1),
           ('11111111-0000-0000-0000-000000000002', N'Bán lẻ & Cửa hàng tiện lợi', 1),
           ('11111111-0000-0000-0000-000000000003', N'Gia sư & Trợ giảng', 1),
           ('11111111-0000-0000-0000-000000000004', N'Giao hàng & Vận chuyển', 1),
           ('11111111-0000-0000-0000-000000000005', N'Marketing & Truyền thông số', 1),
           ('11111111-0000-0000-0000-000000000006', N'Tổ chức sự kiện & Tiệc cưới', 1),
           ('11111111-0000-0000-0000-000000000007', N'Lễ tân & Chăm sóc khách hàng', 1),
           ('11111111-0000-0000-0000-000000000008', N'Công nghệ thông tin & Nhập liệu', 1),
           ('11111111-0000-0000-0000-000000000009', N'Thiết kế & Đồ họa đa phương tiện', 1),
           ('11111111-0000-0000-0000-000000000010', N'Hành chính & Văn phòng', 1),
           ('11111111-0000-0000-0000-000000000011', N'Khách sạn & Du lịch', 1),
           ('11111111-0000-0000-0000-000000000012', N'Làm đẹp, Spa & Chăm sóc sắc đẹp', 1),
           ('11111111-0000-0000-0000-000000000013', N'Bảo vệ & An ninh trật tự', 1),
           ('11111111-0000-0000-0000-000000000014', N'Tạp vụ & Vệ sinh công nghiệp', 1),
           ('11111111-0000-0000-0000-000000000015', N'Thể thao & Fitness', 1),
           ('11111111-0000-0000-0000-000000000016', N'Dịch thuật & Biên soạn nội dung', 1),
           ('11111111-0000-0000-0000-000000000017', N'Nhiếp ảnh & Quay phim', 1),
           ('11111111-0000-0000-0000-000000000018', N'Khảo sát & Nghiên cứu thị trường', 1);
END;

-- =====================================================================================
-- 2. NẠP 36 VIỆC LÀM THỰC TẾ (JOBS) PHÂN BỔ ĐA DOANH NGHIỆP VÀ ĐA DẠNG MỌI TRẠNG THÁI
-- =====================================================================================
IF @EmpHighlands IS NOT NULL
   AND @EmpCircleK IS NOT NULL
   AND @EmpIelts IS NOT NULL
   AND @EmpGoldenGate IS NOT NULL
   AND @EmpCanifa IS NOT NULL
   AND @EmpAnhDuong IS NOT NULL
   AND @EmpCozyBook IS NOT NULL
BEGIN
    INSERT INTO Jobs (Id, Title, Description, VacancyCount, GenderRequirement, MinAgeRequirement, MaxAgeRequirement,
                      CommuneCode, DetailAddress, ExpiryAt, Status, CreatedAt, UpdatedAt, LastBumpedAt, EmployerId, IndustryId)
    VALUES
        -- HIGHLANDS COFFEE (8 Jobs)
        ('20000001-0000-0000-0000-000000000001',
         N'Nhân viên pha chế Barista ca linh hoạt',
         N'Pha chế các loại đồ uống cà phê phin, freeze, trà sữa theo quy chuẩn Highlands Coffee. Giữ gìn quầy bar sạch sẽ, hỗ trợ order khi đông khách. Môi trường trẻ trung, hỗ trợ xoay ca học tập cho sinh viên ĐH FPT.',
         3, NULL, 18, 25, '09988', N'Tòa nhà Alpha, ĐH FPT, Khu CNC Hòa Lạc',
         CAST(DATEADD(DAY, 30, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -5, GETUTCDATE()), DATEADD(HOUR, -1, GETUTCDATE()),
         @EmpHighlands, '11111111-0000-0000-0000-000000000001'),

        ('20000001-0000-0000-0000-000000000002',
         N'Nhân viên phục vụ bàn ca sáng',
         N'Chào đón khách hàng, bưng bê đồ uống, lau dọn bàn ghế gọn gàng, hỗ trợ khách tìm chỗ ngồi. Nhanh nhẹn, tươi cười, trung thực. Có hỗ trợ bữa phụ nhẹ giữa ca.',
         2, NULL, 18, 24, '09988', N'Số 15 đường Cần Kiệm, Khu CNC Hòa Lạc',
         CAST(DATEADD(DAY, 25, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -8, GETUTCDATE()), DATEADD(DAY, -8, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()),
         @EmpHighlands, '11111111-0000-0000-0000-000000000001'),

        ('20000001-0000-0000-0000-000000000003',
         N'Nhân viên thu ngân quầy F&B (Sắp hết hạn)',
         N'Nhận order, nhập hóa đơn trên máy POS, nhận thanh toán tiền mặt/chuyển khoản và in bill. Cẩn thận, chính xác số liệu, thái độ ân cần chu đáo với khách hàng.',
         1, 'Female', 18, 26, '09988', N'Tòa nhà Beta, Khu CNC Hòa Lạc',
         CAST(DATEADD(DAY, 2, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -28, GETUTCDATE()), DATEADD(DAY, -28, GETUTCDATE()), NULL,
         @EmpHighlands, '11111111-0000-0000-0000-000000000001'),

        ('20000001-0000-0000-0000-000000000004',
         N'Phụ bar & chuẩn bị nguyên liệu đầu ca sáng',
         N'Kiểm tra date sữa, đánh siro, chuẩn bị đá viên, hỗ trợ sơ chế trái cây và bổ sung hạt cà phê vào máy trước giờ mở cửa đón khách sáng sớm.',
         2, NULL, 18, 26, '09988', N'Khu Shophouse liền kề ĐH FPT, Hòa Lạc',
         CAST(DATEADD(DAY, 20, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, -3, GETUTCDATE()), NULL,
         @EmpHighlands, '11111111-0000-0000-0000-000000000001'),

        ('20000001-0000-0000-0000-000000000005',
         N'Nhân viên phục vụ bàn tiệc sự kiện cuối tuần',
         N'Phục vụ các đoàn khách du lịch và sự kiện giao lưu doanh nghiệp tại cơ sở Hòa Lạc vào Thứ Bảy, Chủ Nhật. Trả lương ngay sau ca làm.',
         4, NULL, 18, 26, '09988', N'Số 88 đường Mục Uyên, Khu CNC Hòa Lạc',
         CAST(DATEADD(DAY, 28, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -2, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()), DATEADD(HOUR, -6, GETUTCDATE()),
         @EmpHighlands, '11111111-0000-0000-0000-000000000001'),

        ('20000001-0000-0000-0000-000000000006',
         N'Nhân viên pha chế Barista ca tối (Đã đủ người)',
         N'Pha chế các dòng cà phê truyền thống và trà đá xay. Đã tuyển đủ nhân sự theo kế hoạch, tin đăng được đóng để quản lý danh sách trúng tuyển.',
         2, NULL, 18, 25, '09988', N'Số 15 đường Cần Kiệm, Khu CNC Hòa Lạc',
         CAST(DATEADD(DAY, 15, GETUTCDATE()) AS DATE), 'Closed', DATEADD(DAY, -15, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), NULL,
         @EmpHighlands, '11111111-0000-0000-0000-000000000001'),

        ('20000001-0000-0000-0000-000000000007',
         N'Giám sát ca làm việc part-time (Có kinh nghiệm F&B)',
         N'Hỗ trợ quản lý cửa hàng phân chia ca trực, kiểm soát định lượng nguyên vật liệu, xử lý phát sinh của khách hàng trong ca. Ưu tiên sinh viên năm cuối đã có kinh nghiệm F&B trên 6 tháng.',
         1, NULL, 21, 28, '09988', N'Tòa nhà Alpha, ĐH FPT, Khu CNC Hòa Lạc',
         CAST(DATEADD(DAY, 22, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -4, GETUTCDATE()), DATEADD(DAY, -4, GETUTCDATE()), NULL,
         @EmpHighlands, '11111111-0000-0000-0000-000000000001'),

        ('20000001-0000-0000-0000-000000000008',
         N'Nhân viên tạp vụ & rửa cốc chén quầy bar',
         N'Vận hành máy rửa ly, dọn dẹp khay cốc, lau rửa sàn nhà và đổ rác cuối ca. Công việc nhẹ nhàng, không yêu cầu kinh nghiệm, môi trường điều hòa mát mẻ.',
         2, NULL, 18, 45, '09988', N'Khu Shophouse liền kề ĐH FPT, Hòa Lạc',
         CAST(DATEADD(DAY, 30, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -6, GETUTCDATE()), DATEADD(DAY, -6, GETUTCDATE()), NULL,
         @EmpHighlands, '11111111-0000-0000-0000-000000000014'),

        -- CIRCLE K (7 Jobs)
        ('20000001-0000-0000-0000-000000000009',
         N'Nhân viên thu ngân & bán hàng ca đêm Circle K',
         N'Thanh toán hóa đơn cho khách qua máy POS, tiếp nhận đồ ăn nhanh và chế biến mì/bánh mì theo yêu cầu. Phụ cấp ca đêm đặc biệt hấp dẫn, an ninh cửa hàng đảm bảo.',
         3, NULL, 18, 27, '00166', N'Số 120 đường Cầu Giấy, Phường Cầu Giấy',
         CAST(DATEADD(DAY, 30, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE()), DATEADD(HOUR, -3, GETUTCDATE()),
         @EmpCircleK, '11111111-0000-0000-0000-000000000002'),

        ('20000001-0000-0000-0000-000000000010',
         N'Nhân viên sắp xếp & kiểm kê kệ hàng tiện lợi ca sáng',
         N'Nhận hàng hóa từ xe trung chuyển, kiểm tra hạn sử dụng và sắp xếp lên kệ theo sơ đồ chuẩn. Dán tem giá và giữ quầy hàng luôn ngăn nắp, sạch đẹp.',
         2, NULL, 18, 26, '00166', N'Số 45 đường Trần Thái Tông, Phường Cầu Giấy',
         CAST(DATEADD(DAY, 20, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -10, GETUTCDATE()), DATEADD(DAY, -10, GETUTCDATE()), NULL,
         @EmpCircleK, '11111111-0000-0000-0000-000000000002'),

        ('20000001-0000-0000-0000-000000000011',
         N'Nhân viên bán hàng part-time xoay ca Đống Đa',
         N'Phục vụ khách hàng mua sắm tại cửa hàng tiện lợi, tư vấn các chương trình tích điểm thẻ thành viên và khuyến mãi combo đồ ăn nước uống.',
         4, NULL, 18, 25, '00190', N'Số 68 đường Ô Chợ Dừa, Phường Ô Chợ Dừa',
         CAST(DATEADD(DAY, 26, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, -3, GETUTCDATE()), DATEADD(HOUR, -2, GETUTCDATE()),
         @EmpCircleK, '11111111-0000-0000-0000-000000000002'),

        ('20000001-0000-0000-0000-000000000012',
         N'Nhân viên kiểm đếm tồn kho hàng tuần Circle K',
         N'Sử dụng máy quét mã vạch kiểm đếm số lượng tồn kho từng nhóm hàng bánh kẹo, nước ngọt, thuốc lá định kỳ vào sáng Thứ Ba và Thứ Năm.',
         2, NULL, 19, 26, '00190', N'Số 152 đường Xã Đàn, Phường Ô Chợ Dừa',
         CAST(DATEADD(DAY, 18, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -8, GETUTCDATE()), DATEADD(DAY, -8, GETUTCDATE()), NULL,
         @EmpCircleK, '11111111-0000-0000-0000-000000000002'),

        ('20000001-0000-0000-0000-000000000013',
         N'Nhân viên bán hàng thời vụ cuối tuần (Sắp hết hạn)',
         N'Tăng cường hỗ trợ bán hàng vào Thứ Bảy và Chủ Nhật cho cụm cửa hàng tiện lợi gần các trường đại học. Nhận lương liền sau tuần làm việc.',
         2, NULL, 18, 24, '00166', N'Số 89 đường Hồ Tùng Mậu, Phường Cầu Giấy',
         CAST(DATEADD(DAY, 1, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -29, GETUTCDATE()), DATEADD(DAY, -29, GETUTCDATE()), NULL,
         @EmpCircleK, '11111111-0000-0000-0000-000000000002'),

        ('20000001-0000-0000-0000-000000000014',
         N'Nhân viên chuẩn bị bữa ăn sáng nhanh Circle K',
         N'Chuẩn bị bánh bao, xúc xích nướng, luộc trứng và làm cà phê máy phục vụ khách đi làm sáng sớm từ 06:00 đến 09:30 hàng ngày.',
         2, NULL, 18, 30, '00166', N'Số 210 đường Cầu Giấy, Phường Cầu Giấy',
         CAST(DATEADD(DAY, 25, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -5, GETUTCDATE()), NULL,
         @EmpCircleK, '11111111-0000-0000-0000-000000000002'),

        ('20000001-0000-0000-0000-000000000015',
         N'Nhân viên trực quầy tiện lợi ca chiều (Đã đóng)',
         N'Bán hàng ca chiều từ 13:00 đến 18:00. Vị trí đã tuyển đủ ứng viên sinh viên trường Giao thông Vận tải.',
         2, NULL, 18, 25, '00190', N'Số 68 đường Ô Chợ Dừa, Phường Ô Chợ Dừa',
         CAST(DATEADD(DAY, 10, GETUTCDATE()) AS DATE), 'Closed', DATEADD(DAY, -14, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()), NULL,
         @EmpCircleK, '11111111-0000-0000-0000-000000000002'),

        -- THE IELTS WORKSHOP (5 Jobs)
        ('20000001-0000-0000-0000-000000000016',
         N'Trợ giảng Tiếng Anh lớp IELTS Foundation (6.5+)',
         N'Hỗ trợ giáo viên đứng lớp sửa bài phát âm, chấm bài tập về nhà và hướng dẫn học viên thực hành giao tiếp theo nhóm nhỏ. Yêu cầu IELTS từ 6.5 trở lên, phát âm chuẩn.',
         2, NULL, 19, 25, '00190', N'Số 17 ngõ 27 đường Huỳnh Thúc Kháng, Đống Đa',
         CAST(DATEADD(DAY, 28, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -6, GETUTCDATE()), DATEADD(DAY, -6, GETUTCDATE()), DATEADD(HOUR, -4, GETUTCDATE()),
         @EmpIelts, '11111111-0000-0000-0000-000000000003'),

        ('20000001-0000-0000-0000-000000000017',
         N'Gia sư chấm bài Writing & Speaking online',
         N'Nhận bài luận Writing Task 1, 2 và file ghi âm Speaking của học viên qua phần mềm nội bộ, chấm điểm chi tiết theo band descriptors và ghi chú lỗi sai cụ thể.',
         3, NULL, 20, 26, '00190', N'Làm việc online linh hoạt tại nhà',
         CAST(DATEADD(DAY, 30, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -4, GETUTCDATE()), DATEADD(DAY, -4, GETUTCDATE()), NULL,
         @EmpIelts, '11111111-0000-0000-0000-000000000003'),

        ('20000001-0000-0000-0000-000000000018',
         N'Tư vấn viên tuyển sinh khóa học IELTS ca tối',
         N'Trực hotline và fanpage trung tâm, tư vấn lộ trình học phù hợp theo trình độ đầu vào của học viên, sắp xếp lịch thi thử IELTS Mock Test miễn phí.',
         2, 'Female', 19, 25, '00166', N'Tầng 3 số 80 đường Duy Tân, Phường Cầu Giấy',
         CAST(DATEADD(DAY, 24, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -5, GETUTCDATE()), NULL,
         @EmpIelts, '11111111-0000-0000-0000-000000000007'),

        ('20000001-0000-0000-0000-000000000019',
         N'Trợ giảng lớp phát âm tiếng Anh thiếu nhi cuối tuần',
         N'Quản lý trật tự lớp học vui nhộn cho các bé tiểu học, tổ chức minigame từ vựng tiếng Anh và hỗ trợ giảng viên nước ngoài tương tác cùng học sinh.',
         2, NULL, 18, 24, '00190', N'Số 17 ngõ 27 đường Huỳnh Thúc Kháng, Đống Đa',
         CAST(DATEADD(DAY, 20, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE()), NULL,
         @EmpIelts, '11111111-0000-0000-0000-000000000003'),

        ('20000001-0000-0000-0000-000000000020',
         N'Cộng tác viên biên soạn tài liệu bổ trợ ngữ pháp (Đã đủ người)',
         N'Biên soạn bài tập trắc nghiệm và flashcard từ vựng theo giáo trình IELTS nội bộ. Dự án đã tuyển đủ cộng tác viên.',
         1, NULL, 19, 26, '00190', N'Làm việc online linh hoạt',
         CAST(DATEADD(DAY, 10, GETUTCDATE()) AS DATE), 'Closed', DATEADD(DAY, -18, GETUTCDATE()), DATEADD(DAY, -3, GETUTCDATE()), NULL,
         @EmpIelts, '11111111-0000-0000-0000-000000000016'),

        -- GOLDEN GATE LẨU NƯỚNG (6 Jobs)
        ('20000001-0000-0000-0000-000000000021',
         N'Nhân viên phục vụ bàn nhà hàng K-Pub Ba Đình',
         N'Đón tiếp khách hàng, bưng khay thịt nướng và đồ ăn kèm, thay vỉ nướng định kỳ cho khách, giữ gìn bàn ăn sạch sẽ. Bao ăn ca tại nhà hàng, tip tự giữ.',
         4, NULL, 18, 26, '00004', N'Số 29 đường Liễu Giai, Phường Ba Đình',
         CAST(DATEADD(DAY, 28, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -6, GETUTCDATE()), DATEADD(DAY, -6, GETUTCDATE()), DATEADD(HOUR, -5, GETUTCDATE()),
         @EmpGoldenGate, '11111111-0000-0000-0000-000000000001'),

        ('20000001-0000-0000-0000-000000000022',
         N'Phụ bếp sơ chế thực phẩm & làm sốt lẩu ca chiều',
         N'Cắt thái rau củ, định lượng khay thịt đông lạnh, khuấy sốt ướp theo công thức định mức của bếp trưởng. Môi trường bếp chuyên nghiệp, vệ sinh an toàn thực phẩm.',
         3, NULL, 18, 30, '00004', N'Số 29 đường Liễu Giai, Phường Ba Đình',
         CAST(DATEADD(DAY, 22, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -9, GETUTCDATE()), DATEADD(DAY, -9, GETUTCDATE()), NULL,
         @EmpGoldenGate, '11111111-0000-0000-0000-000000000001'),

        ('20000001-0000-0000-0000-000000000023',
         N'Nhân viên tiếp đón lễ tân nhà hàng nướng',
         N'Đứng tại sảnh đón khách, kiểm tra thông tin đặt bàn trước qua hotline, hướng dẫn khách vào vị trí và giới thiệu menu buffet nướng lẩu.',
         2, 'Female', 18, 25, '00004', N'Tòa nhà Lotte Center, 54 Liễu Giai, Ba Đình',
         CAST(DATEADD(DAY, 26, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -4, GETUTCDATE()), DATEADD(DAY, -4, GETUTCDATE()), NULL,
         @EmpGoldenGate, '11111111-0000-0000-0000-000000000007'),

        ('20000001-0000-0000-0000-000000000024',
         N'Nhân viên phục vụ bàn tiệc nướng cao điểm (Đã hết hạn)',
         N'Phục vụ khách ăn tối tiệc sinh nhật và liên hoan cuối tuần. Tin tuyển dụng đã hết hạn nộp hồ sơ cách đây 3 ngày.',
         3, NULL, 18, 25, '00004', N'Số 29 đường Liễu Giai, Phường Ba Đình',
         CAST(DATEADD(DAY, -3, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -33, GETUTCDATE()), DATEADD(DAY, -33, GETUTCDATE()), NULL,
         @EmpGoldenGate, '11111111-0000-0000-0000-000000000001'),

        ('20000001-0000-0000-0000-000000000025',
         N'Nhân viên phục vụ nhà hàng nướng ca tối (Bị ẩn vi phạm)',
         N'Tin tuyển dụng bị khóa bởi kiểm duyệt viên do có phản ánh bắt nhân viên làm thêm 1.5 tiếng dọn dẹp vệ sinh bếp sau ca mà không tính lương làm thêm giờ.',
         2, NULL, 18, 26, '00004', N'Số 12 đường Đội Cấn, Phường Ba Đình',
         CAST(DATEADD(DAY, 15, GETUTCDATE()) AS DATE), 'AdminHidden', DATEADD(DAY, -12, GETUTCDATE()), DATEADD(DAY, -4, GETUTCDATE()), NULL,
         @EmpGoldenGate, '11111111-0000-0000-0000-000000000001'),

        ('20000001-0000-0000-0000-000000000026',
         N'Nhân viên phụ dọn rửa bát máy & vệ sinh ca đêm',
         N'Thu dọn vỉ nướng, chén đĩa vào bồn ngâm, xếp vào máy rửa công nghiệp và lau dọn bề mặt sàn nhà hàng sạch sẽ sau giờ đóng cửa.',
         2, NULL, 18, 40, '00004', N'Số 29 đường Liễu Giai, Phường Ba Đình',
         CAST(DATEADD(DAY, 20, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -8, GETUTCDATE()), DATEADD(DAY, -8, GETUTCDATE()), NULL,
         @EmpGoldenGate, '11111111-0000-0000-0000-000000000014'),

        -- CANIFA THỜI TRANG (4 Jobs)
        ('20000001-0000-0000-0000-000000000027',
         N'Nhân viên tư vấn bán hàng thời trang Canifa Thanh Xuân',
         N'Đón tiếp khách hàng, tư vấn chọn size, chất liệu và phối đồ trang phục. Hỗ trợ khách thử đồ và gấp quần áo ngay ngắn trên kệ trưng bày.',
         3, 'Female', 18, 24, '00367', N'Số 181 đường Nguyễn Trãi, Phường Thanh Xuân',
         CAST(DATEADD(DAY, 28, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -5, GETUTCDATE()), NULL,
         @EmpCanifa, '11111111-0000-0000-0000-000000000002'),

        ('20000001-0000-0000-0000-000000000028',
         N'Nhân viên phụ kho & dán tem nhãn sản phẩm part-time',
         N'Nhận kiện hàng quần áo len, sơ mi mới về, bắn mã vạch giá, gắn chip chống trộm và chuyển giao vào kho phụ cận quầy bán hàng.',
         2, 'Male', 18, 28, '00367', N'Số 181 đường Nguyễn Trãi, Phường Thanh Xuân',
         CAST(DATEADD(DAY, 22, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -7, GETUTCDATE()), NULL,
         @EmpCanifa, '11111111-0000-0000-0000-000000000002'),

        ('20000001-0000-0000-0000-000000000029',
         N'Thu ngân cửa hàng thời trang ca tối',
         N'Thực hiện thanh toán qua máy pos, tháo chip chống trộm, đóng túi giấy cho khách hàng và in hóa đơn bán lẻ. Công việc nhẹ nhàng, môi trường máy lạnh.',
         2, 'Female', 18, 26, '00367', N'Số 254 đường Nguyễn Tuân, Phường Thanh Xuân',
         CAST(DATEADD(DAY, 25, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -4, GETUTCDATE()), DATEADD(DAY, -4, GETUTCDATE()), NULL,
         @EmpCanifa, '11111111-0000-0000-0000-000000000002'),

        ('20000001-0000-0000-0000-000000000030',
         N'Nhân viên hỗ trợ kiểm kê định kỳ cuối tháng',
         N'Sử dụng máy kiểm kê cầm tay quét mã toàn bộ áo quần tại kho và kệ bán lẻ để đối chiếu số liệu hệ thống. Làm việc trong 2 ngày cuối tháng.',
         4, NULL, 18, 26, '00367', N'Số 181 đường Nguyễn Trãi, Phường Thanh Xuân',
         CAST(DATEADD(DAY, 15, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -10, GETUTCDATE()), DATEADD(DAY, -10, GETUTCDATE()), NULL,
         @EmpCanifa, '11111111-0000-0000-0000-000000000002'),

        -- ÁNH DƯƠNG EVENT (4 Jobs)
        ('20000001-0000-0000-0000-000000000031',
         N'Lễ tân đón khách tiệc cưới & sự kiện cuối tuần',
         N'Đứng tại sảnh tiệc đón khách, cài hoa cài áo, hướng dẫn quan khách vào sảnh tiệc và trao quà lưu niệm. Yêu cầu ngoại hình thanh lịch, chiều cao từ 1m60.',
         6, 'Female', 18, 25, '00070', N'Khách sạn Melia, 44 Lý Thường Kiệt, Hoàn Kiếm',
         CAST(DATEADD(DAY, 26, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -4, GETUTCDATE()), DATEADD(DAY, -4, GETUTCDATE()), NULL,
         @EmpAnhDuong, '11111111-0000-0000-0000-000000000006'),

        ('20000001-0000-0000-0000-000000000032',
         N'Nhân viên kỹ thuật setup âm thanh ánh sáng sự kiện',
         N'Phụ giúp kỹ thuật viên chính kéo dây micro, test đèn sân khấu, căn chỉnh loa và chạy thử slide thuyết trình tại các hội thảo doanh nghiệp.',
         3, 'Male', 18, 28, '00070', N'Cung Văn hóa Hữu nghị Việt Xô, Hoàn Kiếm',
         CAST(DATEADD(DAY, 24, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -6, GETUTCDATE()), DATEADD(DAY, -6, GETUTCDATE()), NULL,
         @EmpAnhDuong, '11111111-0000-0000-0000-000000000006'),

        ('20000001-0000-0000-0000-000000000033',
         N'PG / PB giới thiệu sản phẩm triển lãm công nghệ',
         N'Mặc đồng phục thương hiệu, phát tờ rơi giới thiệu giải pháp AI, hướng dẫn khách tham quan trải nghiệm thử nghiệm tại gian hàng triển lãm.',
         4, NULL, 18, 25, '00166', N'Trung tâm Hội nghị Quốc gia, gần Cầu Giấy',
         CAST(DATEADD(DAY, 18, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -8, GETUTCDATE()), DATEADD(DAY, -8, GETUTCDATE()), NULL,
         @EmpAnhDuong, '11111111-0000-0000-0000-000000000006'),

        ('20000001-0000-0000-0000-000000000034',
         N'Telesales tài chính linh hoạt (Bị ẩn do báo cáo lừa đảo)',
         N'Tin tuyển dụng bị khóa bởi Censor do nội dung mô tả sai sự thật, lôi kéo nạp tiền vào sàn ảo không rõ nguồn gốc.',
         5, NULL, 18, 28, '00070', N'Số 8 Tràng Thi, Phường Hoàn Kiếm',
         CAST(DATEADD(DAY, 10, GETUTCDATE()) AS DATE), 'AdminHidden', DATEADD(DAY, -15, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), NULL,
         @EmpAnhDuong, '11111111-0000-0000-0000-000000000005'),

        -- COZY BOOK CAFE (2 Jobs)
        ('20000001-0000-0000-0000-000000000035',
         N'Nhân viên trực quầy sách & pha chế trà nhẹ nhàng',
         N'Phục vụ các loại trà thảo mộc, cà phê máy đơn giản. Quản lý giá sách, sắp xếp sách trả về đúng vị trí danh mục, duy trì không gian đọc sách yên tĩnh.',
         2, NULL, 18, 25, '00070', N'Số 12 đường Đinh Lễ, Phường Hoàn Kiếm',
         CAST(DATEADD(DAY, 30, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, -3, GETUTCDATE()), NULL,
         @EmpCozyBook, '11111111-0000-0000-0000-000000000001'),

        ('20000001-0000-0000-0000-000000000036',
         N'Cộng tác viên tổ chức workshop đọc sách cuối tuần',
         N'Hỗ trợ chuẩn bị máy chiếu, đón khách tham dự câu lạc bộ sách, điều phối minigame giao lưu tác giả và chụp ảnh kỷ niệm buổi sinh hoạt.',
         1, NULL, 19, 25, '00070', N'Số 12 đường Đinh Lễ, Phường Hoàn Kiếm',
         CAST(DATEADD(DAY, 25, GETUTCDATE()) AS DATE), 'Active', DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -5, GETUTCDATE()), NULL,
         @EmpCozyBook, '11111111-0000-0000-0000-000000000006');

    -- =====================================================================================
    -- 3. NẠP MỨC LƯƠNG ĐA DẠNG ĐƠN VỊ THỜI GIAN (JOB SALARIES: Hour, Session, Day, Month, Project)
    -- =====================================================================================
    INSERT INTO JobSalaries (Id, Amount, TimeQuantity, TimeUnit, JobId)
    VALUES
        -- Highlands Coffee
        (NEWID(), 28000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000001'),
        (NEWID(), 25000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000002'),
        (NEWID(), 30000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000003'),
        (NEWID(), 120000.00, 1, 'Session', '20000001-0000-0000-0000-000000000004'),
        (NEWID(), 250000.00, 1, 'Session', '20000001-0000-0000-0000-000000000005'),
        (NEWID(), 28000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000006'),
        (NEWID(), 3800000.00, 1, 'Month', '20000001-0000-0000-0000-000000000007'),
        (NEWID(), 24000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000008'),

        -- Circle K
        (NEWID(), 38000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000009'),
        (NEWID(), 26000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000010'),
        (NEWID(), 26000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000011'),
        (NEWID(), 180000.00, 1, 'Session', '20000001-0000-0000-0000-000000000012'),
        (NEWID(), 220000.00, 1, 'Session', '20000001-0000-0000-0000-000000000013'),
        (NEWID(), 130000.00, 1, 'Session', '20000001-0000-0000-0000-000000000014'),
        (NEWID(), 26000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000015'),

        -- The IELTS Workshop
        (NEWID(), 70000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000016'),
        (NEWID(), 1500000.00, 1, 'Project', '20000001-0000-0000-0000-000000000017'),
        (NEWID(), 3500000.00, 1, 'Month', '20000001-0000-0000-0000-000000000018'),
        (NEWID(), 200000.00, 1, 'Session', '20000001-0000-0000-0000-000000000019'),
        (NEWID(), 1200000.00, 1, 'Project', '20000001-0000-0000-0000-000000000020'),

        -- Golden Gate
        (NEWID(), 27000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000021'),
        (NEWID(), 3600000.00, 1, 'Month', '20000001-0000-0000-0000-000000000022'),
        (NEWID(), 30000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000023'),
        (NEWID(), 220000.00, 1, 'Session', '20000001-0000-0000-0000-000000000024'),
        (NEWID(), 25000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000025'),
        (NEWID(), 32000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000026'),

        -- Canifa
        (NEWID(), 26000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000027'),
        (NEWID(), 160000.00, 1, 'Session', '20000001-0000-0000-0000-000000000028'),
        (NEWID(), 28000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000029'),
        (NEWID(), 400000.00, 1, 'Day', '20000001-0000-0000-0000-000000000030'),

        -- Ánh Dương Event
        (NEWID(), 250000.00, 1, 'Session', '20000001-0000-0000-0000-000000000031'),
        (NEWID(), 500000.00, 1, 'Day', '20000001-0000-0000-0000-000000000032'),
        (NEWID(), 350000.00, 1, 'Session', '20000001-0000-0000-0000-000000000033'),
        (NEWID(), 4000000.00, 1, 'Month', '20000001-0000-0000-0000-000000000034'),

        -- Cozy Book Cafe
        (NEWID(), 25000.00, 1, 'Hour', '20000001-0000-0000-0000-000000000035'),
        (NEWID(), 180000.00, 1, 'Session', '20000001-0000-0000-0000-000000000036');

    -- =====================================================================================
    -- 4. NẠP LỊCH LÀM VIỆC CÔNG VIỆC (JOB SCHEDULES)
    -- =====================================================================================
    INSERT INTO JobSchedules (Id, DayOfWeek, StartTime, EndTime, JobId)
    VALUES
        -- Job 1: Barista Highlands
        (NEWID(), 'Monday', '13:00:00', '17:30:00', '20000001-0000-0000-0000-000000000001'),
        (NEWID(), 'Wednesday', '13:00:00', '17:30:00', '20000001-0000-0000-0000-000000000001'),
        (NEWID(), 'Saturday', '08:00:00', '12:30:00', '20000001-0000-0000-0000-000000000001'),

        -- Job 2: Phục vụ sáng Highlands
        (NEWID(), 'Tuesday', '07:00:00', '11:30:00', '20000001-0000-0000-0000-000000000002'),
        (NEWID(), 'Thursday', '07:00:00', '11:30:00', '20000001-0000-0000-0000-000000000002'),

        -- Job 3: Thu ngân Highlands
        (NEWID(), 'Monday', '11:30:00', '16:00:00', '20000001-0000-0000-0000-000000000003'),
        (NEWID(), 'Friday', '11:30:00', '16:00:00', '20000001-0000-0000-0000-000000000003'),

        -- Job 4: Phụ bar Highlands
        (NEWID(), 'Monday', '06:00:00', '08:30:00', '20000001-0000-0000-0000-000000000004'),
        (NEWID(), 'Wednesday', '06:00:00', '08:30:00', '20000001-0000-0000-0000-000000000004'),

        -- Job 5: Tiệc cuối tuần Highlands
        (NEWID(), 'Saturday', '09:00:00', '15:00:00', '20000001-0000-0000-0000-000000000005'),
        (NEWID(), 'Sunday', '09:00:00', '15:00:00', '20000001-0000-0000-0000-000000000005'),

        -- Job 9: Ca đêm Circle K
        (NEWID(), 'Monday', '22:00:00', '06:00:00', '20000001-0000-0000-0000-000000000009'),
        (NEWID(), 'Wednesday', '22:00:00', '06:00:00', '20000001-0000-0000-0000-000000000009'),
        (NEWID(), 'Friday', '22:00:00', '06:00:00', '20000001-0000-0000-0000-000000000009'),

        -- Job 10: Sắp xếp kệ sáng Circle K
        (NEWID(), 'Tuesday', '07:30:00', '11:30:00', '20000001-0000-0000-0000-000000000010'),
        (NEWID(), 'Thursday', '07:30:00', '11:30:00', '20000001-0000-0000-0000-000000000010'),

        -- Job 16: Trợ giảng IELTS Workshop
        (NEWID(), 'Tuesday', '18:30:00', '21:00:00', '20000001-0000-0000-0000-000000000016'),
        (NEWID(), 'Thursday', '18:30:00', '21:00:00', '20000001-0000-0000-0000-000000000016'),

        -- Job 21: Phục vụ K-Pub Golden Gate
        (NEWID(), 'Friday', '17:30:00', '22:30:00', '20000001-0000-0000-0000-000000000021'),
        (NEWID(), 'Saturday', '17:30:00', '22:30:00', '20000001-0000-0000-0000-000000000021'),
        (NEWID(), 'Sunday', '17:30:00', '22:30:00', '20000001-0000-0000-0000-000000000021'),

        -- Job 27: Tư vấn Canifa
        (NEWID(), 'Monday', '17:30:00', '21:30:00', '20000001-0000-0000-0000-000000000027'),
        (NEWID(), 'Wednesday', '17:30:00', '21:30:00', '20000001-0000-0000-0000-000000000027'),

        -- Job 31: Lễ tân sự kiện Ánh Dương
        (NEWID(), 'Saturday', '08:00:00', '12:30:00', '20000001-0000-0000-0000-000000000031'),
        (NEWID(), 'Sunday', '08:00:00', '12:30:00', '20000001-0000-0000-0000-000000000031'),

        -- Job 35: Quầy sách Cozy Book
        (NEWID(), 'Monday', '08:00:00', '12:00:00', '20000001-0000-0000-0000-000000000035'),
        (NEWID(), 'Wednesday', '08:00:00', '12:00:00', '20000001-0000-0000-0000-000000000035');

    -- =====================================================================================
    -- 5. NẠP ĐƠN ỨNG TUYỂN THỰC TẾ (JOB APPLICATIONS: Pending, Accepted, Rejected)
    -- =====================================================================================
    IF @CandNam IS NOT NULL AND @CandTrang IS NOT NULL AND @CandDuc IS NOT NULL AND @CandLinh IS NOT NULL AND @CandLong IS NOT NULL
    BEGIN
        INSERT INTO JobApplications (Id, Status, AppliedAt, JobId, CandidateId)
        VALUES
            -- Ứng viên Nguyễn Hoàng Nam (0000000001)
            (NEWID(), 'Accepted', DATEADD(HOUR, -3, GETUTCDATE()), '20000001-0000-0000-0000-000000000001', @CandNam),
            (NEWID(), 'Pending', DATEADD(DAY, -1, GETUTCDATE()), '20000001-0000-0000-0000-000000000009', @CandNam),
            (NEWID(), 'Rejected', DATEADD(DAY, -4, GETUTCDATE()), '20000001-0000-0000-0000-000000000028', @CandNam),

            -- Ứng viên Trần Thị Thu Trang (0000000002)
            (NEWID(), 'Accepted', DATEADD(DAY, -2, GETUTCDATE()), '20000001-0000-0000-0000-000000000016', @CandTrang),
            (NEWID(), 'Pending', DATEADD(DAY, -1, GETUTCDATE()), '20000001-0000-0000-0000-000000000027', @CandTrang),
            (NEWID(), 'Pending', DATEADD(DAY, -3, GETUTCDATE()), '20000001-0000-0000-0000-000000000031', @CandTrang),

            -- Ứng viên Lê Minh Đức (0000000003)
            (NEWID(), 'Accepted', DATEADD(DAY, -3, GETUTCDATE()), '20000001-0000-0000-0000-000000000017', @CandDuc),
            (NEWID(), 'Pending', DATEADD(DAY, -2, GETUTCDATE()), '20000001-0000-0000-0000-000000000012', @CandDuc),

            -- Ứng viên Phạm Khánh Linh (0000000004)
            (NEWID(), 'Accepted', DATEADD(DAY, -2, GETUTCDATE()), '20000001-0000-0000-0000-000000000027', @CandLinh),
            (NEWID(), 'Pending', DATEADD(DAY, -1, GETUTCDATE()), '20000001-0000-0000-0000-000000000033', @CandLinh),

            -- Ứng viên Đặng Văn Long (0000000005)
            (NEWID(), 'Pending', DATEADD(DAY, -1, GETUTCDATE()), '20000001-0000-0000-0000-000000000010', @CandLong),
            (NEWID(), 'Rejected', DATEADD(DAY, -5, GETUTCDATE()), '20000001-0000-0000-0000-000000000021', @CandLong),
            (NEWID(), 'Accepted', DATEADD(DAY, -3, GETUTCDATE()), '20000001-0000-0000-0000-000000000032', @CandLong);
    END;

    -- =====================================================================================
    -- 6. NẠP BÁO CÁO VI PHẠM (JOB REPORTS: Pending, Resolved, Dismissed)
    -- =====================================================================================
    IF @CandNam IS NOT NULL AND @CandTrang IS NOT NULL AND @CandDuc IS NOT NULL AND @CandLinh IS NOT NULL AND @CensorKien IS NOT NULL
    BEGIN
        INSERT INTO JobReports (Id, Reason, Status, CreatedAt, JobId, ReporterId, ResolverId)
        VALUES
            -- Báo cáo 1: Telesales lừa đảo sàn tài chính (Pending - Chờ Censor xử lý)
            (NEWID(),
             N'Mô tả công việc ghi là trực văn phòng chăm sóc khách hàng, nhưng khi phỏng vấn lại yêu cầu gọi điện thoại lôi kéo người khác nạp tiền tham gia sàn tiền ảo nước ngoài không rõ nguồn gốc.',
             'Pending', DATEADD(DAY, -1, GETUTCDATE()), '20000001-0000-0000-0000-000000000034', @CandTrang, NULL),

            -- Báo cáo 2: Nhà hàng ép làm thêm giờ không trả lương (Resolved - Censor đã xử lý, Job bị AdminHidden)
            (NEWID(),
             N'Nhà hàng yêu cầu nhân viên ở lại dọn dẹp vệ sinh bếp và cọ sàn quá 1 tiếng sau ca mà không tính thêm tiền làm thêm giờ như đã cam kết lúc nhận việc.',
             'Resolved', DATEADD(DAY, -5, GETUTCDATE()), '20000001-0000-0000-0000-000000000025', @CandNam, @CensorKien),

            -- Báo cáo 3: Circle K địa chỉ khó tìm (Dismissed - Censor bác bỏ do tin đăng có đầy đủ hotline và hướng dẫn)
            (NEWID(),
             N'Ứng viên phản ánh địa chỉ kho hàng khó tìm, tuy nhiên nhà tuyển dụng đã ghi rõ sơ đồ hướng dẫn tại số 45 Trần Thái Tông và số điện thoại quản lý nhận cuộc gọi.',
             'Dismissed', DATEADD(DAY, -4, GETUTCDATE()), '20000001-0000-0000-0000-000000000010', @CandLong, @CensorKien),

            -- Báo cáo 4: Yêu cầu giữ CCCD gốc bất hợp pháp (Pending - Chờ Censor xử lý)
            (NEWID(),
             N'Người quản lý yêu cầu ứng viên phải nộp lại bản gốc Căn cước công dân để giữ chân trong 1 tháng thử việc, vi phạm quy định pháp luật lao động.',
             'Pending', DATEADD(HOUR, -12, GETUTCDATE()), '20000001-0000-0000-0000-000000000024', @CandDuc, NULL),

            -- Báo cáo 5: Thay đổi trang phục biểu diễn không thỏa thuận trước (Resolved - Censor đã xử lý)
            (NEWID(),
             N'Nhà tuyển dụng thay đổi trang phục của PG sự kiện trái với cam kết ban đầu, không phù hợp thuần phong mỹ tục.',
             'Resolved', DATEADD(DAY, -3, GETUTCDATE()), '20000001-0000-0000-0000-000000000033', @CandLinh, @CensorKien);
    END;
END;
