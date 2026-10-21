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
     1000.00, 30, 20, 50, 1, 1, 1),
    ('Ultra', 'Yearly', N'Gói Ultra (1 Năm)',
     N'Tăng thêm 20 tin đăng hoạt động, tặng 100 lượt đẩy tin, gợi ý ứng viên cho công việc và chủ động tìm kiếm ứng viên linh hoạt, gói toàn diện nhất.',
     1000000.00, 365, 20, 100, 1, 1, 1);

-- =====================================================================================
-- Khởi tạo gói đăng ký hoạt động cho Nhà tuyển dụng mẫu (0000000010)
-- =====================================================================================

DECLARE @EmployerId UNIQUEIDENTIFIER = (SELECT TOP 1 Id
                                        FROM AspNetUsers
                                        WHERE UserName = '0000000010');

IF @EmployerId IS NOT NULL AND NOT EXISTS (SELECT 1
                                           FROM EmployerSubscriptions
                                           WHERE EmployerId = @EmployerId)
    BEGIN
        INSERT INTO EmployerSubscriptions (Id,
                                           EmployerId,
                                           Tier,
                                           Duration,
                                           StartDate,
                                           EndDate,
                                           Status,
                                           CreatedAt,
                                           ExtraActivePosts,
                                           CanRecommendCandidates,
                                           CanQueryCandidates)
        VALUES (NEWID(),
                @EmployerId,
                'Ultra',
                'Monthly',
                GETUTCDATE(),
                DATEADD(month, 1, GETUTCDATE()),
                'Active',
                GETUTCDATE(),
                20,
                1,
                1);
    END;
