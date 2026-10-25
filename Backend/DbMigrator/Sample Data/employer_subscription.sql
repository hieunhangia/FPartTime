-- =====================================================================================
-- Seed Data: Gói dịch vụ nhà tuyển dụng (Employer Subscription Plans)
-- =====================================================================================

INSERT INTO EmployerSubscriptionPlans (Tier,
                                       Duration,
                                       Name,
                                       Description,
                                       Price,
                                       DurationDays,
                                       ExtraActivePosts,
                                       FreeBumps,
                                       CanRecommendCandidates,
                                       CanQueryCandidates,
                                       IsActive)
VALUES
    -- 1. Gói Plus
    ('Plus', 'Monthly', N'Gói Plus (1 Tháng)', N'Tăng thêm 5 tin đăng hoạt động, tặng 5 lượt đẩy tin.', 30000.00, 30, 5,
     5, 0, 0, 1),
    ('Plus', 'Yearly', N'Gói Plus (1 Năm)', N'Tăng thêm 5 tin đăng hoạt động, tặng 20 lượt đẩy tin, tiết kiệm hơn.',
     200000.00, 365, 5, 20, 0, 0, 1),

    -- 2. Gói Pro
    ('Pro', 'Monthly', N'Gói Pro (1 Tháng)',
     N'Tăng thêm 10 tin đăng hoạt động, tặng 10 lượt đẩy tin, gợi ý ứng viên phù hợp cho từng công việc.', 100000.00,
     30, 10, 10, 1, 0, 1),
    ('Pro', 'Yearly', N'Gói Pro (1 Năm)',
     N'Tăng thêm 10 tin đăng hoạt động, tặng 40 lượt đẩy tin, gợi ý ứng viên phù hợp cho từng công việc, tiết kiệm chi phí.',
     600000.00, 365, 10, 40, 1, 0, 1),

    -- 3. Gói Ultra
    ('Ultra', 'Monthly', N'Gói Ultra (1 Tháng)',
     N'Tăng thêm 20 tin đăng hoạt động, tặng 50 lượt đẩy tin, gợi ý ứng viên cho công việc và chủ động tìm kiếm ứng viên linh hoạt.',
     2000.00, 30, 20, 50, 1, 1, 1),
    ('Ultra', 'Yearly', N'Gói Ultra (1 Năm)',
     N'Tăng thêm 20 tin đăng hoạt động, tặng 100 lượt đẩy tin, gợi ý ứng viên cho công việc và chủ động tìm kiếm ứng viên linh hoạt, gói toàn diện nhất.',
     1000000.00, 365, 20, 100, 1, 1, 1);

-- =====================================================================================
-- Khởi tạo gói đăng ký hoạt động cho các Nhà tuyển dụng mẫu (Đa dạng mọi trạng thái)
-- =====================================================================================

DECLARE @Employer1Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000010'); -- Highlands Coffee
DECLARE @Employer2Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000020'); -- Circle K
DECLARE @Employer3Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000030'); -- The IELTS Workshop
DECLARE @Employer4Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0000000040'); -- Golden Gate
DECLARE @Employer5Id UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM AspNetUsers WHERE UserName = '0967890123'); -- Ánh Dương Event

-- 1. Highlands Coffee (0000000010): Gói Ultra - Đang hoạt động (Active), hạn còn 25 ngày
IF @Employer1Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM EmployerSubscriptions WHERE EmployerId = @Employer1Id)
BEGIN
    INSERT INTO EmployerSubscriptions (Id, EmployerId, Tier, StartDate, EndDate, Status, CreatedAt, ExtraActivePosts, CanRecommendCandidates, CanQueryCandidates)
    VALUES (NEWID(), @Employer1Id, 'Ultra', DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, 25, GETUTCDATE()), 'Active', DATEADD(DAY, -5, GETUTCDATE()), 20, 1, 1);
END;

-- 2. Circle K (0000000020): Gói Pro - Đang hoạt động (Active), hạn còn 14 ngày
IF @Employer2Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM EmployerSubscriptions WHERE EmployerId = @Employer2Id)
BEGIN
    INSERT INTO EmployerSubscriptions (Id, EmployerId, Tier, StartDate, EndDate, Status, CreatedAt, ExtraActivePosts, CanRecommendCandidates, CanQueryCandidates)
    VALUES (NEWID(), @Employer2Id, 'Pro', DATEADD(DAY, -16, GETUTCDATE()), DATEADD(DAY, 14, GETUTCDATE()), 'Active', DATEADD(DAY, -16, GETUTCDATE()), 10, 1, 0);
END;

-- 3. The IELTS Workshop (0000000030): Gói Plus - Đang hoạt động sắp hết hạn (Active), hạn còn 5 ngày
IF @Employer3Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM EmployerSubscriptions WHERE EmployerId = @Employer3Id)
BEGIN
    INSERT INTO EmployerSubscriptions (Id, EmployerId, Tier, StartDate, EndDate, Status, CreatedAt, ExtraActivePosts, CanRecommendCandidates, CanQueryCandidates)
    VALUES (NEWID(), @Employer3Id, 'Plus', DATEADD(DAY, -25, GETUTCDATE()), DATEADD(DAY, 5, GETUTCDATE()), 'Active', DATEADD(DAY, -25, GETUTCDATE()), 5, 0, 0);
END;

-- 4. Golden Gate (0000000040): Gói Pro - ĐÃ HẾT HẠN (Expired), hết hạn cách đây 10 ngày (Test cảnh báo gia hạn)
IF @Employer4Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM EmployerSubscriptions WHERE EmployerId = @Employer4Id)
BEGIN
    INSERT INTO EmployerSubscriptions (Id, EmployerId, Tier, StartDate, EndDate, Status, CreatedAt, ExtraActivePosts, CanRecommendCandidates, CanQueryCandidates)
    VALUES (NEWID(), @Employer4Id, 'Pro', DATEADD(DAY, -40, GETUTCDATE()), DATEADD(DAY, -10, GETUTCDATE()), 'Expired', DATEADD(DAY, -40, GETUTCDATE()), 10, 1, 0);
END;

-- 5. Ánh Dương Event (0967890123): Gói Plus - ĐÃ HỦY (Cancelled), do doanh nghiệp yêu cầu hủy gói
IF @Employer5Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM EmployerSubscriptions WHERE EmployerId = @Employer5Id)
BEGIN
    INSERT INTO EmployerSubscriptions (Id, EmployerId, Tier, StartDate, EndDate, Status, CreatedAt, ExtraActivePosts, CanRecommendCandidates, CanQueryCandidates)
    VALUES (NEWID(), @Employer5Id, 'Plus', DATEADD(DAY, -15, GETUTCDATE()), DATEADD(DAY, 15, GETUTCDATE()), 'Cancelled', DATEADD(DAY, -15, GETUTCDATE()), 5, 0, 0);
END;
