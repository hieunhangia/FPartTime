-- =====================================================================================
-- Seed Data: Sample Jobs, Industries, Salaries, Schedules, Applications, and Reports
-- Target Employer: UserName = '0000000010'
-- Target Candidates: UserName = '0000000001', '0000000002'
-- Target Communes: '09988' (Xã Hòa Lạc), '08980' (Xã Yên Lãng), '16279' (Xã Nông Cống)
-- =====================================================================================

DECLARE @EmployerId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000010');
DECLARE @Candidate1Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000001');
DECLARE @Candidate2Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000002');
DECLARE @CensorId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000100');

IF @EmployerId IS NOT NULL
BEGIN
    -- Mở rộng giới hạn bài đăng để chứa đủ 36 công việc mẫu
    UPDATE Employers SET MaxActivePosts = 50 WHERE UserId = @EmployerId;

    -- =====================================================================================
    -- 1. NẠP 18 NGÀNH NGHỀ PHỔ BIẾN (INDUSTRIES)
    -- =====================================================================================
    IF NOT EXISTS (SELECT 1 FROM Industries WHERE Id = '11111111-0000-0000-0000-000000000001')
    BEGIN
        INSERT INTO Industries (Id, Name) VALUES
        ('11111111-0000-0000-0000-000000000001', N'F&B - Phục vụ & Pha chế'),
        ('11111111-0000-0000-0000-000000000002', N'Bán lẻ & Cửa hàng tiện lợi'),
        ('11111111-0000-0000-0000-000000000003', N'Gia sư & Trợ giảng'),
        ('11111111-0000-0000-0000-000000000004', N'Giao hàng & Vận chuyển'),
        ('11111111-0000-0000-0000-000000000005', N'Marketing & Truyền thông số'),
        ('11111111-0000-0000-0000-000000000006', N'Tổ chức sự kiện & Tiệc cưới'),
        ('11111111-0000-0000-0000-000000000007', N'Lễ tân & Chăm sóc khách hàng'),
        ('11111111-0000-0000-0000-000000000008', N'Công nghệ thông tin & Nhập liệu'),
        ('11111111-0000-0000-0000-000000000009', N'Thiết kế & Đồ họa đa phương tiện'),
        ('11111111-0000-0000-0000-000000000010', N'Hành chính & Văn phòng'),
        ('11111111-0000-0000-0000-000000000011', N'Khách sạn & Du lịch'),
        ('11111111-0000-0000-0000-000000000012', N'Làm đẹp, Spa & Chăm sóc sắc đẹp'),
        ('11111111-0000-0000-0000-000000000013', N'Bảo vệ & An ninh trật tự'),
        ('11111111-0000-0000-0000-000000000014', N'Tạp vụ & Vệ sinh công nghiệp'),
        ('11111111-0000-0000-0000-000000000015', N'Thể thao & Fitness'),
        ('11111111-0000-0000-0000-000000000016', N'Dịch thuật & Biên soạn nội dung'),
        ('11111111-0000-0000-0000-000000000017', N'Nhiếp ảnh & Quay phim'),
        ('11111111-0000-0000-0000-000000000018', N'Khảo sát & Nghiên cứu thị trường');
    END;

    -- =====================================================================================
    -- 2. NẠP 36 CÔNG VIỆC (JOBS) CHO USER 0000000010
    -- Phân bố ngẫu nhiên đều cho 3 xã: 09988 (Hòa Lạc), 08980 (Yên Lãng), 16279 (Nông Cống)
    -- =====================================================================================
    INSERT INTO Jobs (Id, Title, Description, VacancyCount, GenderRequirement, MinAgeRequirement, MaxAgeRequirement, CommuneCode, DetailAddress, ExpiryAt, Status, CreatedAt, EmployerId, IndustryId) VALUES
    ('22222222-0000-0000-0000-000000000001',
     N'Nhân viên pha chế Barista ca linh hoạt',
     N'Pha chế các loại cà phê, trà hoa quả theo công thức chuẩn của quán. Hỗ trợ đón khách, order đồ uống và giữ gìn vệ sinh sạch sẽ khu vực quầy bar. Môi trường trẻ trung, thân thiện, có hỗ trợ đào tạo bài bản cho người mới bắt đầu.',
     3, NULL, 18, 26, '09988', N'Số 15 đường Cần Kiệm, gần ĐH FPT, Khu CNC Hòa Lạc', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -14, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000001'),

    ('22222222-0000-0000-0000-000000000002',
     N'Nhân viên phục vụ quán ăn gia đình ca tối',
     N'Chào đón khách hàng, lấy order, bưng bê món ăn và dọn dẹp bàn sau khi khách dùng bữa. Yêu cầu nhanh nhẹn, hòa đồng, trung thực. Có hỗ trợ ăn tối miễn phí tại quán.',
     2, NULL, 18, 28, '08980', N'Số 42 đường Trục chính Yên Lãng', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -12, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000001'),

    ('22222222-0000-0000-0000-000000000003',
     N'Nhân viên thu ngân siêu thị mini',
     N'Thực hiện thanh toán hàng hóa qua máy quét mã vạch và phần mềm POS, in hóa đơn, bàn giao tiền ca và kiểm kê kệ hàng gần quầy. Công việc nhẹ nhàng trong môi trường máy lạnh mát mẻ.',
     2, 'Female', 18, 30, '16279', N'Số 89 đường Quốc lộ 45, Tiểu khu Nam Tiến', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -10, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000002'),

    ('22222222-0000-0000-0000-000000000004',
     N'Nhân viên bán hàng & sắp xếp kệ hàng tiện lợi 24/7',
     N'Tiếp nhận hàng hóa, kiểm tra date và xếp hàng lên quầy kệ ngăn nắp. Tư vấn các chương trình khuyến mãi cho khách hàng đến mua sắm tại cửa hàng tiện lợi.',
     4, NULL, 18, 25, '09988', N'Số 88 đường Mục Uyên, Khu CNC Hòa Lạc', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -9, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000002'),

    ('22222222-0000-0000-0000-000000000005',
     N'Gia sư Tiếng Anh cho học sinh THCS',
     N'Dạy kèm ngữ pháp và kỹ năng giao tiếp tiếng Anh cơ bản cho học sinh lớp 7-8. Giúp học sinh củng cố kiến thức trên lớp và ôn thi giữa kỳ, cuối kỳ đạt kết quả tốt.',
     1, NULL, 19, 25, '08980', N'Thôn Bến, gần trường THCS Yên Lãng', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -8, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000003'),

    ('22222222-0000-0000-0000-000000000006',
     N'Trợ giảng lớp Toán tư duy tiểu học cuối tuần',
     N'Hỗ trợ giáo viên đứng lớp quản lý trật tự, chấm bài tập thực hành, hướng dẫn các em học sinh làm bài và trao đổi tình hình học tập với phụ huynh sau buổi học.',
     2, NULL, 18, 24, '16279', N'Số 12 đường Trần Phú, Xã Nông Cống', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -7, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000003'),

    ('22222222-0000-0000-0000-000000000007',
     N'Nhân viên giao đồ ăn & bưu phẩm nội khu CNC Hòa Lạc',
     N'Nhận đồ ăn và các kiện hàng bưu phẩm nhỏ từ các quán ăn, giao nhanh đến ký túc xá và văn phòng doanh nghiệp trong khuôn viên khu CNC. Có phụ cấp xăng xe.',
     5, NULL, 18, 35, '09988', N'Khu ký túc xá sinh viên, Khu CNC Hòa Lạc', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -15, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000004'),

    ('22222222-0000-0000-0000-000000000008',
     N'Nhân viên phân loại & đóng gói bưu phẩm bưu cục',
     N'Phân chia bưu gửi theo từng tuyến giao nhận, dán tem mã vận đơn và hỗ trợ bốc dỡ các kiện hàng nhẹ tại kho bưu cục. Công việc đều đặn, không áp lực doanh số.',
     3, 'Male', 18, 32, '08980', N'Bưu cục trung tâm, Thôn Đồi Sen, Xã Yên Lãng', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -11, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000004'),

    ('22222222-0000-0000-0000-000000000009',
     N'Cộng tác viên viết bài Content Marketing & Fanpage',
     N'Sáng tạo nội dung bài viết quảng bá nông sản và đặc sản địa phương trên Fanpage Facebook, TikTok. Trả lời comment, giải đáp thắc mắc cơ bản của khách hàng.',
     2, NULL, 18, 28, '16279', N'Số 150 đường Lê Lai, Xã Nông Cống', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -6, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000005'),

    ('22222222-0000-0000-0000-000000000010',
     N'Cộng tác viên Seeding và phát triển cộng đồng sinh viên',
     N'Tham gia chia sẻ bài viết, kích hoạt thảo luận trên các diễn đàn, hội nhóm sinh viên tại Hòa Lạc để lan tỏa thông tin các sự kiện học thuật và nghề nghiệp.',
     3, NULL, 18, 24, '09988', N'Tòa nhà Innovation Hub, Lô CN1, Khu CNC Hòa Lạc', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -5, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000005'),

    ('22222222-0000-0000-0000-000000000011',
     N'Nhân viên phục vụ tiệc cưới cuối tuần',
     N'Chuẩn bị setup bàn tiệc, bưng bê món ăn theo quy trình tiệc cưới chuyên nghiệp, dọn dẹp bàn tiệc sau chương trình. Thích hợp cho sinh viên làm thêm cuối tuần.',
     6, NULL, 18, 26, '08980', N'Trung tâm tiệc cưới Ven Sông, Xã Yên Lãng', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -13, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000006'),

    ('22222222-0000-0000-0000-000000000012',
     N'PG/PB hỗ trợ gian hàng triển lãm hội chợ thương mại',
     N'Đứng quầy giới thiệu sản phẩm tiêu dùng, phát tờ rơi brochure thông tin, hướng dẫn khách tham quan trải nghiệm sản phẩm và tham gia bốc thăm trúng thưởng.',
     4, NULL, 18, 25, '16279', N'Sân vận động Trung tâm, Xã Nông Cống', DATEADD(DAY, 36, GETUTCDATE()), 'Closed', DATEADD(DAY, -20, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000006'),

    ('22222222-0000-0000-0000-000000000013',
     N'Lễ tân tòa nhà văn phòng ca xoay linh hoạt',
     N'Trực quầy sảnh chính tòa nhà, chào đón khách đến công tác, hướng dẫn khách làm thủ tục đăng ký ra vào, tiếp nhận bưu phẩm thư tín chuyển phát nhanh.',
     2, 'Female', 19, 26, '09988', N'Tòa nhà Alpha Office, Km 29 Đại lộ Thăng Long, Hòa Lạc', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -4, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000007'),

    ('22222222-0000-0000-0000-000000000014',
     N'Nhân viên trực tổng đài & CSKH bán thời gian',
     N'Nghe điện thoại tổng đài, giải đáp thông tin dịch vụ và các câu hỏi thường gặp của khách hàng, ghi chép phản hồi vào phần mềm quản lý chăm sóc khách hàng.',
     3, NULL, 18, 28, '08980', N'Văn phòng chăm sóc khách hàng, Số 19 đường Làng, Xã Yên Lãng', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -7, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000007'),

    ('22222222-0000-0000-0000-000000000015',
     N'Cộng tác viên kiểm thử phần mềm (Manual Tester) part-time',
     N'Chạy kiểm thử các chức năng trên ứng dụng web và mobile theo test case có sẵn, ghi chép và log các lỗi phát hiện lên Jira. Ưu tiên sinh viên ngành CNTT có tư duy logic tốt.',
     2, NULL, 19, 26, '16279', N'Phòng R&D Công nghệ số, Thôn Minh Hải, Xã Nông Cống', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -5, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000008'),

    ('22222222-0000-0000-0000-000000000016',
     N'Nhân viên nhập liệu số hóa hồ sơ chứng từ',
     N'Nhập thông tin khách hàng, hóa đơn chứng từ từ bản scan giấy sang bảng tính Excel và phần mềm lưu trữ số. Yêu cầu tính cẩn thận, tỉ mỉ, tốc độ gõ phím nhanh.',
     3, NULL, 18, 27, '09988', N'Tòa nhà Công nghệ F-Town, Khu CNC Hòa Lạc', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -3, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000008'),

    ('22222222-0000-0000-0000-000000000017',
     N'Thiết kế đồ họa 2D banner quảng cáo part-time',
     N'Thiết kế ấn phẩm truyền thông, banner mạng xã hội Facebook, poster sự kiện bằng Canva hoặc Photoshop theo brief của phòng truyền thông. Có thể mang laptop cá nhân làm việc.',
     1, NULL, 18, 28, '08980', N'Cụm văn phòng sáng tạo Yên Lãng', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -8, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000009'),

    ('22222222-0000-0000-0000-000000000018',
     N'Video Editor dựng video ngắn TikTok / Reels theo ca',
     N'Cắt ghép video, chèn nhạc bắt trend, tạo hiệu ứng chữ và âm thanh cho các video ngắn giới thiệu sản phẩm. Yêu cầu biết sử dụng CapCut PC hoặc Premiere căn bản.',
     2, NULL, 18, 26, '16279', N'Studio Media Nam Tiến, Xã Nông Cống', DATEADD(DAY, 36, GETUTCDATE()), 'AdminHidden', DATEADD(DAY, -16, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000009'),

    ('22222222-0000-0000-0000-000000000019',
     N'Trợ lý văn phòng kiêm sắp xếp tài liệu bán thời gian',
     N'Phân loại và đóng tập hồ sơ hợp đồng, kiểm tra tài liệu văn phòng, scan giấy tờ gửi email và chuẩn bị phòng họp khi có khách đối tác đến làm việc.',
     1, NULL, 19, 26, '09988', N'Văn phòng quản lý dự án, Khu CNC Hòa Lạc', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -6, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000010'),

    ('22222222-0000-0000-0000-000000000020',
     N'Nhân viên photocopy và in ấn văn phòng phẩm',
     N'Vận hành máy photocopy, máy scan, đóng sổ gáy xoắn tài liệu cho khách hàng, hỗ trợ bán sách vở văn phòng phẩm tại cửa hàng gần cổng trường học.',
     2, NULL, 18, 25, '08980', N'Cửa hàng In ấn số 56 đường Liên Xã Yên Lãng', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -9, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000010'),

    ('22222222-0000-0000-0000-000000000021',
     N'Nhân viên phụ bếp buffet khách sạn ca sáng',
     N'Sơ chế rau củ quả, chuẩn bị nguyên liệu thịt cá cho các món ăn sáng, phụ trách chia món và rửa dọn dụng cụ nấu nướng sau giờ tiệc buffet.',
     2, NULL, 18, 32, '16279', N'Khách sạn Hương Quê, Tiểu khu Nam Tiến, Xã Nông Cống', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -11, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000011'),

    ('22222222-0000-0000-0000-000000000022',
     N'Hướng dẫn viên phụ trách đoàn khách dã ngoại sinh thái',
     N'Đón tiếp các đoàn tham quan học sinh, sinh viên, dẫn đoàn qua các trạm trải nghiệm, tổ chức các trò chơi hoạt náo và đảm bảo an toàn cho các bạn trong đoàn.',
     3, NULL, 20, 28, '09988', N'Khu trải nghiệm sinh thái Hồ Tân Xã, Xã Hòa Lạc', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -15, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000011'),

    ('22222222-0000-0000-0000-000000000023',
     N'Trợ lý thợ phụ gội đầu dưỡng sinh & làm móng part-time',
     N'Thực hiện các bước gội đầu dưỡng sinh thư giãn, đắp mặt nạ, ngâm chân thảo mộc cho khách và dọn dẹp vệ sinh phòng dịch vụ sạch sẽ, ngăn nắp.',
     2, 'Female', 18, 28, '08980', N'Tiệm Spa An Nhiên, Thôn Yên Lãng, Xã Yên Lãng', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -7, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000012'),

    ('22222222-0000-0000-0000-000000000024',
     N'Kỹ thuật viên phụ việc tiệm làm tóc & mi móng',
     N'Hỗ trợ thợ chính xả tóc, sấy tóc, khử trùng kềm kéo dụng cụ và giữ vệ sinh không gian tiệm. Được chỉ dạy nâng cao tay nghề nếu có tinh thần cầu tiến.',
     1, 'Female', 18, 26, '16279', N'Beauty Salon Mai Anh, Số 48 đường Thanh Niên, Xã Nông Cống', DATEADD(DAY, 36, GETUTCDATE()), 'Closed', DATEADD(DAY, -22, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000012'),

    ('22222222-0000-0000-0000-000000000025',
     N'Nhân viên giữ xe quán cà phê theo ca',
     N'Dắt xe, bấm vé và sắp xếp xe gọn gàng vào bãi cho khách đến uống nước. Giữ gìn an ninh, quan sát khu vực để xe của khách hàng luôn an toàn.',
     2, 'Male', 18, 40, '09988', N'Cổng số 2 Khu CNC Hòa Lạc, Xã Hòa Lạc', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -5, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000013'),

    ('22222222-0000-0000-0000-000000000026',
     N'Nhân viên an ninh tuần tra bãi xe ca đêm',
     N'Trực chốt kiểm soát phương tiện ban đêm, đi tuần tra định kỳ quanh khuôn viên nhà xưởng bãi giữ xe, giám sát hệ thống camera an ninh và phòng cháy chữa cháy.',
     2, 'Male', 20, 45, '08980', N'Cụm công nghiệp Yên Lãng, Xã Yên Lãng', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -12, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000013'),

    ('22222222-0000-0000-0000-000000000027',
     N'Nhân viên tạp vụ văn phòng theo giờ buổi sáng',
     N'Lau dọn sàn nhà, lau kính, rửa cốc chén, thu gom và phân loại rác thải văn phòng trước khi nhân viên vào làm việc ca sáng. Môi trường sạch sẽ, lịch sự.',
     1, 'Female', 22, 50, '16279', N'Tòa nhà Chi nhánh Ngân hàng, Xã Nông Cống', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -8, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000014'),

    ('22222222-0000-0000-0000-000000000028',
     N'Nhân viên vệ sinh buồng phòng homestay theo ca',
     N'Thay chăn ga gối đệm, hút bụi, lau sàn, cọ rửa nhà tắm và bổ sung vật phẩm tiện ích trong phòng homestay sau khi khách làm thủ tục check-out trả phòng.',
     2, NULL, 20, 45, '09988', N'Homestay Thung Lũng Xanh, Thôn 4, Xã Hòa Lạc', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -4, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000014'),

    ('22222222-0000-0000-0000-000000000029',
     N'Lễ tân quầy dịch vụ phòng tập Gym & Yoga',
     N'Chào đón hội viên, quẹt thẻ check-in, phát khăn tập, thu ngân bán nước bù khoáng và hỗ trợ tư vấn các gói dịch vụ thẻ tập thể thao tại trung tâm.',
     2, NULL, 18, 25, '08980', N'Trung tâm Thể hình Star Fitness, Xã Yên Lãng', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -6, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000015'),

    ('22222222-0000-0000-0000-000000000030',
     N'Cứu hộ hồ bơi kiêm quản lý phòng thay đồ ca chiều',
     N'Quan sát nhắc nhở người bơi tuân thủ nội quy an toàn tại hồ bơi, cứu hộ xử lý sự cố kịp thời, kiểm tra vệ sinh và khóa tủ gửi đồ của khách bơi.',
     1, 'Male', 19, 28, '16279', N'Khu liên hợp thể thao Nông Cống, Xã Nông Cống', DATEADD(DAY, 36, GETUTCDATE()), 'AdminHidden', DATEADD(DAY, -18, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000015'),

    ('22222222-0000-0000-0000-000000000031',
     N'Cộng tác viên dịch thuật tài liệu Anh - Việt bán thời gian',
     N'Biên dịch tài liệu hướng dẫn kỹ thuật, tài liệu hội thảo và các bài viết tin tức công nghệ từ tiếng Anh sang tiếng Việt. Được làm việc linh hoạt kết hợp từ xa.',
     2, NULL, 19, 30, '09988', N'Tòa nhà Alpha, Khu CNC Hòa Lạc', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -3, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000016'),

    ('22222222-0000-0000-0000-000000000032',
     N'Biên tập viên hiệu đính sách & tài liệu giáo dục',
     N'Đọc soát lỗi chính tả, câu từ, ngữ pháp tiếng Việt, đối chiếu bản dịch với bản gốc các đầu sách kỹ năng sống và tài liệu tham khảo cho học sinh.',
     1, NULL, 20, 30, '08980', N'Văn phòng sách Tri Thức, Thôn Bến, Xã Yên Lãng', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -5, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000016'),

    ('22222222-0000-0000-0000-000000000033',
     N'Phụ quay & setup đèn ánh sáng tại studio chụp ảnh',
     N'Hỗ trợ nhiếp ảnh gia sắp xếp phông nền, bố trí chân máy, đèn led, chuẩn bị phụ kiện chụp ảnh và hỗ trợ khách hàng chỉnh trang trang phục trong studio.',
     2, NULL, 18, 26, '16279', N'Studio Ảnh cưới Mộng Mơ, Số 30 đường Tỉnh lộ 505', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -7, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000017'),

    ('22222222-0000-0000-0000-000000000034',
     N'Thợ chụp ảnh sự kiện & kỷ yếu học sinh sinh viên',
     N'Chụp ảnh phóng sự sự kiện ngày hội trường, chụp ảnh nhóm kỷ yếu tốt nghiệp tại các trường đại học lân cận. Yêu cầu có máy ảnh cá nhân và biết chụp góc rộng.',
     2, NULL, 19, 27, '09988', N'Khuôn viên Đại học FPT, Khu CNC Hòa Lạc', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -14, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000017'),

    ('22222222-0000-0000-0000-000000000035',
     N'Nhân viên phỏng vấn khảo sát người tiêu dùng tại chợ',
     N'Gặp gỡ phỏng vấn người tiêu dùng theo bảng hỏi có sẵn trên tablet về thói quen mua sắm hàng tiêu dùng, ghi nhận ý kiến đánh giá chất lượng sản phẩm.',
     3, NULL, 18, 28, '08980', N'Khu vực chợ trung tâm Yên Lãng, Xã Yên Lãng', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -9, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000018'),

    ('22222222-0000-0000-0000-000000000036',
     N'Khách hàng bí mật (Mystery Shopper) đánh giá cửa hàng',
     N'Đóng vai khách hàng đến trải nghiệm mua sắm tại các đại lý, ghi nhận thái độ phục vụ của nhân viên, chụp hình quầy kệ và hoàn thành phiếu đánh giá trực tuyến.',
     2, NULL, 19, 30, '16279', N'Trung tâm bán lẻ Nam Tiến, Xã Nông Cống', DATEADD(DAY, 36, GETUTCDATE()), 'Active', DATEADD(DAY, -2, GETUTCDATE()), @EmployerId, '11111111-0000-0000-0000-000000000018');

    -- =====================================================================================
    -- 3. NẠP MỨC LƯƠNG CÔNG VIỆC (JOB SALARIES)
    -- Đa dạng hóa: một số công việc có 1 tùy chọn, nhiều công việc có 2-3 tùy chọn lương
    -- Đầy đủ các đơn vị thời gian: Hour, Session, Day, Week, Month, Project
    -- =====================================================================================
    INSERT INTO JobSalaries (Id, Amount, TimeQuantity, TimeUnit, JobId) VALUES
    -- Job 1: Barista (2 tùy chọn: theo giờ hoặc theo tháng cố định)
    ('33333333-0000-0000-0000-000000000001', 28000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000001'),
    ('33333333-0000-0000-0000-000000000002', 3500000.00, 1, 'Month', '22222222-0000-0000-0000-000000000001'),

    -- Job 2: Phục vụ quán ăn ca tối (1 tùy chọn)
    ('33333333-0000-0000-0000-000000000003', 25000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000002'),

    -- Job 3: Thu ngân siêu thị mini (2 tùy chọn: theo giờ hoặc theo tháng)
    ('33333333-0000-0000-0000-000000000004', 26000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000003'),
    ('33333333-0000-0000-0000-000000000005', 3200000.00, 1, 'Month', '22222222-0000-0000-0000-000000000003'),

    -- Job 4: Cửa hàng tiện lợi 24/7 (3 tùy chọn: theo giờ, theo ngày, hoặc theo tháng)
    ('33333333-0000-0000-0000-000000000006', 27000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000004'),
    ('33333333-0000-0000-0000-000000000007', 220000.00, 1, 'Day', '22222222-0000-0000-0000-000000000004'),
    ('33333333-0000-0000-0000-000000000008', 4000000.00, 1, 'Month', '22222222-0000-0000-0000-000000000004'),

    -- Job 5: Gia sư Tiếng Anh (2 tùy chọn: theo buổi hoặc trọn gói tháng)
    ('33333333-0000-0000-0000-000000000009', 150000.00, 1, 'Session', '22222222-0000-0000-0000-000000000005'),
    ('33333333-0000-0000-0000-000000000010', 1200000.00, 1, 'Month', '22222222-0000-0000-0000-000000000005'),

    -- Job 6: Trợ giảng Toán tư duy (1 tùy chọn)
    ('33333333-0000-0000-0000-000000000011', 200000.00, 1, 'Session', '22222222-0000-0000-0000-000000000006'),

    -- Job 7: Shipper nội khu (3 tùy chọn: theo giờ, theo ngày hoặc theo tuần)
    ('33333333-0000-0000-0000-000000000012', 35000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000007'),
    ('33333333-0000-0000-0000-000000000013', 280000.00, 1, 'Day', '22222222-0000-0000-0000-000000000007'),
    ('33333333-0000-0000-0000-000000000014', 1600000.00, 1, 'Week', '22222222-0000-0000-0000-000000000007'),

    -- Job 8: Phân loại bưu phẩm bưu cục (1 tùy chọn)
    ('33333333-0000-0000-0000-000000000015', 30000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000008'),

    -- Job 9: Content Marketing & Fanpage (3 tùy chọn: theo giờ, theo dự án bài viết hoặc lương tháng)
    ('33333333-0000-0000-0000-000000000016', 35000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000009'),
    ('33333333-0000-0000-0000-000000000017', 500000.00, 1, 'Project', '22222222-0000-0000-0000-000000000009'),
    ('33333333-0000-0000-0000-000000000018', 2500000.00, 1, 'Month', '22222222-0000-0000-0000-000000000009'),

    -- Job 10: Seeding cộng đồng (2 tùy chọn: theo giờ hoặc theo chiến dịch)
    ('33333333-0000-0000-0000-000000000019', 30000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000010'),
    ('33333333-0000-0000-0000-000000000020', 400000.00, 1, 'Project', '22222222-0000-0000-0000-000000000010'),

    -- Job 11: Phục vụ tiệc cưới (2 tùy chọn: theo ca tiệc hoặc trọn ngày 2 ca)
    ('33333333-0000-0000-0000-000000000021', 180000.00, 1, 'Session', '22222222-0000-0000-0000-000000000011'),
    ('33333333-0000-0000-0000-000000000022', 350000.00, 1, 'Day', '22222222-0000-0000-0000-000000000011'),

    -- Job 12: PG/PB hội chợ triển lãm (1 tùy chọn)
    ('33333333-0000-0000-0000-000000000023', 300000.00, 1, 'Day', '22222222-0000-0000-0000-000000000012'),

    -- Job 13: Lễ tân tòa nhà văn phòng (2 tùy chọn: theo giờ hoặc theo tháng)
    ('33333333-0000-0000-0000-000000000024', 32000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000013'),
    ('33333333-0000-0000-0000-000000000025', 3800000.00, 1, 'Month', '22222222-0000-0000-0000-000000000013'),

    -- Job 14: Trực tổng đài & CSKH (1 tùy chọn)
    ('33333333-0000-0000-0000-000000000026', 28000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000014'),

    -- Job 15: Manual Tester part-time (3 tùy chọn: theo giờ, theo sprint dự án, hoặc theo tháng)
    ('33333333-0000-0000-0000-000000000027', 45000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000015'),
    ('33333333-0000-0000-0000-000000000028', 1200000.00, 1, 'Project', '22222222-0000-0000-0000-000000000015'),
    ('33333333-0000-0000-0000-000000000029', 4000000.00, 1, 'Month', '22222222-0000-0000-0000-000000000015'),

    -- Job 16: Nhập liệu số hóa hồ sơ (2 tùy chọn: theo giờ hoặc theo tháng)
    ('33333333-0000-0000-0000-000000000030', 27000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000016'),
    ('33333333-0000-0000-0000-000000000031', 3000000.00, 1, 'Month', '22222222-0000-0000-0000-000000000016'),

    -- Job 17: Thiết kế đồ họa 2D (3 tùy chọn: theo giờ, theo bộ ấn phẩm dự án, hoặc lương tháng)
    ('33333333-0000-0000-0000-000000000032', 50000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000017'),
    ('33333333-0000-0000-0000-000000000033', 350000.00, 1, 'Project', '22222222-0000-0000-0000-000000000017'),
    ('33333333-0000-0000-0000-000000000034', 3500000.00, 1, 'Month', '22222222-0000-0000-0000-000000000017'),

    -- Job 18: Video Editor TikTok / Reels (2 tùy chọn: theo giờ hoặc theo từng video dựng)
    ('33333333-0000-0000-0000-000000000035', 40000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000018'),
    ('33333333-0000-0000-0000-000000000036', 250000.00, 1, 'Project', '22222222-0000-0000-0000-000000000018'),

    -- Job 19: Trợ lý văn phòng bán thời gian (1 tùy chọn)
    ('33333333-0000-0000-0000-000000000037', 30000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000019'),

    -- Job 20: Photocopy & in ấn văn phòng phẩm (1 tùy chọn)
    ('33333333-0000-0000-0000-000000000038', 24000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000020'),

    -- Job 21: Phụ bếp buffet ca sáng (2 tùy chọn: theo giờ hoặc theo tháng)
    ('33333333-0000-0000-0000-000000000039', 28000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000021'),
    ('33333333-0000-0000-0000-000000000040', 3200000.00, 1, 'Month', '22222222-0000-0000-0000-000000000021'),

    -- Job 22: Hướng dẫn viên dã ngoại (2 tùy chọn: nửa ngày theo ca hoặc trọn ngày)
    ('33333333-0000-0000-0000-000000000041', 180000.00, 1, 'Session', '22222222-0000-0000-0000-000000000022'),
    ('33333333-0000-0000-0000-000000000042', 350000.00, 1, 'Day', '22222222-0000-0000-0000-000000000022'),

    -- Job 23: Trợ lý thợ phụ gội đầu dưỡng sinh (1 tùy chọn)
    ('33333333-0000-0000-0000-000000000043', 27000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000023'),

    -- Job 24: Kỹ thuật viên phụ việc tiệm tóc & móng (1 tùy chọn)
    ('33333333-0000-0000-0000-000000000044', 25000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000024'),

    -- Job 25: Giữ xe quán cafe (2 tùy chọn: theo giờ hoặc theo ca trực)
    ('33333333-0000-0000-0000-000000000045', 24000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000025'),
    ('33333333-0000-0000-0000-000000000046', 100000.00, 1, 'Session', '22222222-0000-0000-0000-000000000025'),

    -- Job 26: An ninh tuần tra bãi xe đêm (1 tùy chọn)
    ('33333333-0000-0000-0000-000000000047', 30000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000026'),

    -- Job 27: Tạp vụ văn phòng theo giờ sáng (1 tùy chọn)
    ('33333333-0000-0000-0000-000000000048', 35000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000027'),

    -- Job 28: Vệ sinh buồng phòng homestay (2 tùy chọn: theo phòng hoặc theo ngày)
    ('33333333-0000-0000-0000-000000000049', 80000.00, 1, 'Session', '22222222-0000-0000-0000-000000000028'),
    ('33333333-0000-0000-0000-000000000050', 250000.00, 1, 'Day', '22222222-0000-0000-0000-000000000028'),

    -- Job 29: Lễ tân phòng tập Gym & Yoga (2 tùy chọn: theo giờ hoặc theo tháng)
    ('33333333-0000-0000-0000-000000000051', 26000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000029'),
    ('33333333-0000-0000-0000-000000000052', 3000000.00, 1, 'Month', '22222222-0000-0000-0000-000000000029'),

    -- Job 30: Cứu hộ hồ bơi ca chiều (1 tùy chọn)
    ('33333333-0000-0000-0000-000000000053', 35000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000030'),

    -- Job 31: Dịch thuật tài liệu Anh - Việt (3 tùy chọn: theo giờ, theo gói tài liệu dự án, hoặc theo tháng)
    ('33333333-0000-0000-0000-000000000054', 50000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000031'),
    ('33333333-0000-0000-0000-000000000055', 800000.00, 1, 'Project', '22222222-0000-0000-0000-000000000031'),
    ('33333333-0000-0000-0000-000000000056', 4000000.00, 1, 'Month', '22222222-0000-0000-0000-000000000031'),

    -- Job 32: Biên tập viên hiệu đính sách (2 tùy chọn: theo đầu sách hoặc theo tháng)
    ('33333333-0000-0000-0000-000000000057', 1000000.00, 1, 'Project', '22222222-0000-0000-0000-000000000032'),
    ('33333333-0000-0000-0000-000000000058', 3000000.00, 1, 'Month', '22222222-0000-0000-0000-000000000032'),

    -- Job 33: Phụ quay & setup đèn studio (1 tùy chọn)
    ('33333333-0000-0000-0000-000000000059', 35000.00, 1, 'Hour', '22222222-0000-0000-0000-000000000033'),

    -- Job 34: Thợ chụp ảnh kỷ yếu & sự kiện (3 tùy chọn: theo ca chụp, theo ngày, hoặc trọn gói sự kiện)
    ('33333333-0000-0000-0000-000000000060', 500000.00, 1, 'Session', '22222222-0000-0000-0000-000000000034'),
    ('33333333-0000-0000-0000-000000000061', 900000.00, 1, 'Day', '22222222-0000-0000-0000-000000000034'),
    ('33333333-0000-0000-0000-000000000062', 1500000.00, 1, 'Project', '22222222-0000-0000-0000-000000000034'),

    -- Job 35: Phỏng vấn khảo sát người tiêu dùng (1 tùy chọn)
    ('33333333-0000-0000-0000-000000000063', 200000.00, 1, 'Session', '22222222-0000-0000-0000-000000000035'),

    -- Job 36: Mystery Shopper đánh giá cửa hàng (3 tùy chọn: theo lượt cửa hàng, theo ngày, hoặc trọn chiến dịch)
    ('33333333-0000-0000-0000-000000000064', 150000.00, 1, 'Session', '22222222-0000-0000-0000-000000000036'),
    ('33333333-0000-0000-0000-000000000065', 550000.00, 1, 'Day', '22222222-0000-0000-0000-000000000036'),
    ('33333333-0000-0000-0000-000000000066', 2000000.00, 1, 'Project', '22222222-0000-0000-0000-000000000036');

    -- =====================================================================================
    -- 4. NẠP LỊCH LÀM VIỆC THEO CA (JOB SCHEDULES)
    -- =====================================================================================
    INSERT INTO JobSchedules (Id, DayOfWeek, StartTime, EndTime, JobId) VALUES
    -- Job 1
    ('44444444-0000-0000-0000-000000000001', 'Monday', '08:00:00', '12:00:00', '22222222-0000-0000-0000-000000000001'),
    ('44444444-0000-0000-0000-000000000002', 'Wednesday', '08:00:00', '12:00:00', '22222222-0000-0000-0000-000000000001'),
    ('44444444-0000-0000-0000-000000000003', 'Friday', '08:00:00', '12:00:00', '22222222-0000-0000-0000-000000000001'),
    -- Job 2
    ('44444444-0000-0000-0000-000000000004', 'Tuesday', '17:30:00', '22:30:00', '22222222-0000-0000-0000-000000000002'),
    ('44444444-0000-0000-0000-000000000005', 'Thursday', '17:30:00', '22:30:00', '22222222-0000-0000-0000-000000000002'),
    ('44444444-0000-0000-0000-000000000006', 'Saturday', '17:30:00', '22:30:00', '22222222-0000-0000-0000-000000000002'),
    -- Job 3
    ('44444444-0000-0000-0000-000000000007', 'Monday', '13:00:00', '18:00:00', '22222222-0000-0000-0000-000000000003'),
    ('44444444-0000-0000-0000-000000000008', 'Wednesday', '13:00:00', '18:00:00', '22222222-0000-0000-0000-000000000003'),
    ('44444444-0000-0000-0000-000000000009', 'Friday', '13:00:00', '18:00:00', '22222222-0000-0000-0000-000000000003'),
    -- Job 4
    ('44444444-0000-0000-0000-000000000010', 'Monday', '18:00:00', '22:30:00', '22222222-0000-0000-0000-000000000004'),
    ('44444444-0000-0000-0000-000000000011', 'Tuesday', '18:00:00', '22:30:00', '22222222-0000-0000-0000-000000000004'),
    ('44444444-0000-0000-0000-000000000012', 'Thursday', '18:00:00', '22:30:00', '22222222-0000-0000-0000-000000000004'),
    -- Job 5
    ('44444444-0000-0000-0000-000000000013', 'Tuesday', '19:00:00', '21:00:00', '22222222-0000-0000-0000-000000000005'),
    ('44444444-0000-0000-0000-000000000014', 'Thursday', '19:00:00', '21:00:00', '22222222-0000-0000-0000-000000000005'),
    -- Job 6
    ('44444444-0000-0000-0000-000000000015', 'Saturday', '08:30:00', '11:30:00', '22222222-0000-0000-0000-000000000006'),
    ('44444444-0000-0000-0000-000000000016', 'Sunday', '08:30:00', '11:30:00', '22222222-0000-0000-0000-000000000006'),
    -- Job 7
    ('44444444-0000-0000-0000-000000000017', 'Monday', '11:00:00', '14:00:00', '22222222-0000-0000-0000-000000000007'),
    ('44444444-0000-0000-0000-000000000018', 'Wednesday', '11:00:00', '14:00:00', '22222222-0000-0000-0000-000000000007'),
    ('44444444-0000-0000-0000-000000000019', 'Friday', '11:00:00', '14:00:00', '22222222-0000-0000-0000-000000000007'),
    -- Job 8
    ('44444444-0000-0000-0000-000000000020', 'Tuesday', '07:00:00', '11:30:00', '22222222-0000-0000-0000-000000000008'),
    ('44444444-0000-0000-0000-000000000021', 'Thursday', '07:00:00', '11:30:00', '22222222-0000-0000-0000-000000000008'),
    ('44444444-0000-0000-0000-000000000022', 'Saturday', '07:00:00', '11:30:00', '22222222-0000-0000-0000-000000000008'),
    -- Job 9
    ('44444444-0000-0000-0000-000000000023', 'Monday', '13:30:00', '17:30:00', '22222222-0000-0000-0000-000000000009'),
    ('44444444-0000-0000-0000-000000000024', 'Wednesday', '13:30:00', '17:30:00', '22222222-0000-0000-0000-000000000009'),
    ('44444444-0000-0000-0000-000000000025', 'Friday', '13:30:00', '17:30:00', '22222222-0000-0000-0000-000000000009'),
    -- Job 10
    ('44444444-0000-0000-0000-000000000026', 'Tuesday', '18:00:00', '21:00:00', '22222222-0000-0000-0000-000000000010'),
    ('44444444-0000-0000-0000-000000000027', 'Thursday', '18:00:00', '21:00:00', '22222222-0000-0000-0000-000000000010'),
    -- Job 11
    ('44444444-0000-0000-0000-000000000028', 'Saturday', '10:00:00', '15:00:00', '22222222-0000-0000-0000-000000000011'),
    ('44444444-0000-0000-0000-000000000029', 'Sunday', '10:00:00', '15:00:00', '22222222-0000-0000-0000-000000000011'),
    -- Job 12
    ('44444444-0000-0000-0000-000000000030', 'Friday', '08:00:00', '17:00:00', '22222222-0000-0000-0000-000000000012'),
    ('44444444-0000-0000-0000-000000000031', 'Saturday', '08:00:00', '17:00:00', '22222222-0000-0000-0000-000000000012'),
    -- Job 13
    ('44444444-0000-0000-0000-000000000032', 'Monday', '08:00:00', '12:00:00', '22222222-0000-0000-0000-000000000013'),
    ('44444444-0000-0000-0000-000000000033', 'Wednesday', '08:00:00', '12:00:00', '22222222-0000-0000-0000-000000000013'),
    -- Job 14
    ('44444444-0000-0000-0000-000000000034', 'Tuesday', '13:30:00', '17:30:00', '22222222-0000-0000-0000-000000000014'),
    ('44444444-0000-0000-0000-000000000035', 'Thursday', '13:30:00', '17:30:00', '22222222-0000-0000-0000-000000000014'),
    -- Job 15
    ('44444444-0000-0000-0000-000000000036', 'Monday', '13:30:00', '17:30:00', '22222222-0000-0000-0000-000000000015'),
    ('44444444-0000-0000-0000-000000000037', 'Wednesday', '13:30:00', '17:30:00', '22222222-0000-0000-0000-000000000015'),
    ('44444444-0000-0000-0000-000000000038', 'Friday', '13:30:00', '17:30:00', '22222222-0000-0000-0000-000000000015'),
    -- Job 16
    ('44444444-0000-0000-0000-000000000039', 'Tuesday', '08:00:00', '12:00:00', '22222222-0000-0000-0000-000000000016'),
    ('44444444-0000-0000-0000-000000000040', 'Thursday', '08:00:00', '12:00:00', '22222222-0000-0000-0000-000000000016'),
    -- Job 17
    ('44444444-0000-0000-0000-000000000041', 'Monday', '08:30:00', '12:00:00', '22222222-0000-0000-0000-000000000017'),
    ('44444444-0000-0000-0000-000000000042', 'Wednesday', '08:30:00', '12:00:00', '22222222-0000-0000-0000-000000000017'),
    -- Job 18
    ('44444444-0000-0000-0000-000000000043', 'Tuesday', '14:00:00', '18:00:00', '22222222-0000-0000-0000-000000000018'),
    ('44444444-0000-0000-0000-000000000044', 'Thursday', '14:00:00', '18:00:00', '22222222-0000-0000-0000-000000000018'),
    -- Job 19
    ('44444444-0000-0000-0000-000000000045', 'Monday', '13:30:00', '17:30:00', '22222222-0000-0000-0000-000000000019'),
    ('44444444-0000-0000-0000-000000000046', 'Wednesday', '13:30:00', '17:30:00', '22222222-0000-0000-0000-000000000019'),
    -- Job 20
    ('44444444-0000-0000-0000-000000000047', 'Tuesday', '11:30:00', '15:30:00', '22222222-0000-0000-0000-000000000020'),
    ('44444444-0000-0000-0000-000000000048', 'Thursday', '11:30:00', '15:30:00', '22222222-0000-0000-0000-000000000020'),
    -- Job 21
    ('44444444-0000-0000-0000-000000000049', 'Monday', '05:30:00', '10:30:00', '22222222-0000-0000-0000-000000000021'),
    ('44444444-0000-0000-0000-000000000050', 'Wednesday', '05:30:00', '10:30:00', '22222222-0000-0000-0000-000000000021'),
    ('44444444-0000-0000-0000-000000000051', 'Friday', '05:30:00', '10:30:00', '22222222-0000-0000-0000-000000000021'),
    -- Job 22
    ('44444444-0000-0000-0000-000000000052', 'Saturday', '07:30:00', '17:00:00', '22222222-0000-0000-0000-000000000022'),
    ('44444444-0000-0000-0000-000000000053', 'Sunday', '07:30:00', '17:00:00', '22222222-0000-0000-0000-000000000022'),
    -- Job 23
    ('44444444-0000-0000-0000-000000000054', 'Tuesday', '14:00:00', '19:00:00', '22222222-0000-0000-0000-000000000023'),
    ('44444444-0000-0000-0000-000000000055', 'Thursday', '14:00:00', '19:00:00', '22222222-0000-0000-0000-000000000023'),
    -- Job 24
    ('44444444-0000-0000-0000-000000000056', 'Wednesday', '13:00:00', '18:00:00', '22222222-0000-0000-0000-000000000024'),
    ('44444444-0000-0000-0000-000000000057', 'Friday', '13:00:00', '18:00:00', '22222222-0000-0000-0000-000000000024'),
    -- Job 25
    ('44444444-0000-0000-0000-000000000058', 'Monday', '17:00:00', '22:30:00', '22222222-0000-0000-0000-000000000025'),
    ('44444444-0000-0000-0000-000000000059', 'Wednesday', '17:00:00', '22:30:00', '22222222-0000-0000-0000-000000000025'),
    -- Job 26
    ('44444444-0000-0000-0000-000000000060', 'Monday', '22:00:00', '06:00:00', '22222222-0000-0000-0000-000000000026'),
    ('44444444-0000-0000-0000-000000000061', 'Wednesday', '22:00:00', '06:00:00', '22222222-0000-0000-0000-000000000026'),
    -- Job 27
    ('44444444-0000-0000-0000-000000000062', 'Monday', '06:30:00', '08:30:00', '22222222-0000-0000-0000-000000000027'),
    ('44444444-0000-0000-0000-000000000063', 'Wednesday', '06:30:00', '08:30:00', '22222222-0000-0000-0000-000000000027'),
    ('44444444-0000-0000-0000-000000000064', 'Friday', '06:30:00', '08:30:00', '22222222-0000-0000-0000-000000000027'),
    -- Job 28
    ('44444444-0000-0000-0000-000000000065', 'Saturday', '11:30:00', '14:30:00', '22222222-0000-0000-0000-000000000028'),
    ('44444444-0000-0000-0000-000000000066', 'Sunday', '11:30:00', '14:30:00', '22222222-0000-0000-0000-000000000028'),
    -- Job 29
    ('44444444-0000-0000-0000-000000000067', 'Monday', '16:30:00', '21:00:00', '22222222-0000-0000-0000-000000000029'),
    ('44444444-0000-0000-0000-000000000068', 'Wednesday', '16:30:00', '21:00:00', '22222222-0000-0000-0000-000000000029'),
    -- Job 30
    ('44444444-0000-0000-0000-000000000069', 'Tuesday', '14:30:00', '18:30:00', '22222222-0000-0000-0000-000000000030'),
    ('44444444-0000-0000-0000-000000000070', 'Thursday', '14:30:00', '18:30:00', '22222222-0000-0000-0000-000000000030'),
    -- Job 31
    ('44444444-0000-0000-0000-000000000071', 'Monday', '08:30:00', '12:00:00', '22222222-0000-0000-0000-000000000031'),
    ('44444444-0000-0000-0000-000000000072', 'Wednesday', '08:30:00', '12:00:00', '22222222-0000-0000-0000-000000000031');

    -- =====================================================================================
    -- 5. NẠP ĐƠN ỨNG TUYỂN (JOB APPLICATIONS) TỪ USER 0000000001 VÀ 0000000002
    -- =====================================================================================
    IF @Candidate1Id IS NOT NULL AND @Candidate2Id IS NOT NULL
    BEGIN
        INSERT INTO JobApplications (Id, Status, AppliedAt, JobId, CandidateId) VALUES
        -- Ứng viên 0000000001 nộp đơn
        ('55555555-0000-0000-0000-000000000001', 'Accepted', DATEADD(DAY, -5, GETUTCDATE()), '22222222-0000-0000-0000-000000000001', @Candidate1Id),
        ('55555555-0000-0000-0000-000000000002', 'Pending',  DATEADD(DAY, -3, GETUTCDATE()), '22222222-0000-0000-0000-000000000003', @Candidate1Id),
        ('55555555-0000-0000-0000-000000000003', 'Pending',  DATEADD(DAY, -1, GETUTCDATE()), '22222222-0000-0000-0000-000000000005', @Candidate1Id),
        ('55555555-0000-0000-0000-000000000004', 'Accepted', DATEADD(DAY, -4, GETUTCDATE()), '22222222-0000-0000-0000-000000000009', @Candidate1Id),
        ('55555555-0000-0000-0000-000000000005', 'Pending',  DATEADD(DAY, -2, GETUTCDATE()), '22222222-0000-0000-0000-000000000016', @Candidate1Id),
        ('55555555-0000-0000-0000-000000000006', 'Rejected', DATEADD(DAY, -8, GETUTCDATE()), '22222222-0000-0000-0000-000000000029', @Candidate1Id),

        -- Ứng viên 0000000002 nộp đơn
        ('55555555-0000-0000-0000-000000000007', 'Pending',  DATEADD(DAY, -2, GETUTCDATE()), '22222222-0000-0000-0000-000000000001', @Candidate2Id),
        ('55555555-0000-0000-0000-000000000008', 'Accepted', DATEADD(DAY, -6, GETUTCDATE()), '22222222-0000-0000-0000-000000000004', @Candidate2Id),
        ('55555555-0000-0000-0000-000000000009', 'Rejected', DATEADD(DAY, -9, GETUTCDATE()), '22222222-0000-0000-0000-000000000007', @Candidate2Id),
        ('55555555-0000-0000-0000-000000000010', 'Pending',  DATEADD(DAY, -3, GETUTCDATE()), '22222222-0000-0000-0000-000000000013', @Candidate2Id),
        ('55555555-0000-0000-0000-000000000011', 'Accepted', DATEADD(DAY, -7, GETUTCDATE()), '22222222-0000-0000-0000-000000000022', @Candidate2Id),
        ('55555555-0000-0000-0000-000000000012', 'Pending',  DATEADD(DAY, -1, GETUTCDATE()), '22222222-0000-0000-0000-000000000034', @Candidate2Id);
    END;

    -- =====================================================================================
    -- 6. NẠP BÁO CÁO CÔNG VIỆC (JOB REPORTS) TỪ USER 0000000001 VÀ 0000000002
    -- =====================================================================================
    IF @Candidate1Id IS NOT NULL AND @Candidate2Id IS NOT NULL
    BEGIN
        INSERT INTO JobReports (Id, Reason, Status, CreatedAt, JobId, ReporterId, ResolverId) VALUES
        -- Báo cáo từ 0000000001: Quán ăn ca tối bắt làm quá giờ không tính thêm lương (Pending)
        ('66666666-0000-0000-0000-000000000001',
         N'Mô tả công việc là bưng bê phục vụ nhưng khi vào làm thực tế bị ép dọn dẹp vệ sinh khu vực bếp quá 1 tiếng sau ca mà không tính thêm tiền làm thêm giờ.',
         'Pending', DATEADD(DAY, -3, GETUTCDATE()), '22222222-0000-0000-0000-000000000002', @Candidate1Id, NULL),

        -- Báo cáo từ 0000000002: Shipper bị yêu cầu đóng tiền đặt cọc giữ chỗ (Resolved)
        ('66666666-0000-0000-0000-000000000002',
         N'Nhà tuyển dụng yêu cầu ứng viên phải đóng tiền cọc đồng phục và thế chân 500.000 VNĐ trước khi nhận việc, không đúng quy định.',
         'Resolved', DATEADD(DAY, -7, GETUTCDATE()), '22222222-0000-0000-0000-000000000007', @Candidate2Id, @CensorId),

        -- Báo cáo từ 0000000001: Telesales có dấu hiệu lừa đảo tài chính (Resolved)
        ('66666666-0000-0000-0000-000000000003',
         N'Công việc yêu cầu gọi điện thoại lôi kéo người khác nạp tiền tham gia sàn đầu tư tài chính quốc tế không rõ nguồn gốc, sai lệch hoàn toàn so với mô tả trực tổng đài.',
         'Resolved', DATEADD(DAY, -5, GETUTCDATE()), '22222222-0000-0000-0000-000000000014', @Candidate1Id, @CensorId),

        -- Báo cáo từ 0000000002: Cửa hàng in ấn sai lệch thông tin liên lạc (Pending)
        ('66666666-0000-0000-0000-000000000004',
         N'Địa chỉ cửa hàng không có thật tại thực địa, số điện thoại liên hệ trên tin tuyển dụng luôn trong tình trạng thuê bao không liên lạc được.',
         'Pending', DATEADD(DAY, -1, GETUTCDATE()), '22222222-0000-0000-0000-000000000020', @Candidate2Id, NULL),

        -- Báo cáo từ 0000000001: Bảo vệ ca đêm bị trừ các khoản phí vô lý (Dismissed)
        ('66666666-0000-0000-0000-000000000005',
         N'Báo cáo nhà tuyển dụng tính lương không đúng thỏa thuận ban đầu, tuy nhiên ứng viên chưa nắm rõ quy định thử việc 3 ngày đầu đã ghi rõ trong thỏa thuận.',
         'Dismissed', DATEADD(DAY, -4, GETUTCDATE()), '22222222-0000-0000-0000-000000000026', @Candidate1Id, @CensorId);
    END;
END;
