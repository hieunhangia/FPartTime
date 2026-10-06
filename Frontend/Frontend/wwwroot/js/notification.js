document.addEventListener('DOMContentLoaded', function () {
    const dropdownBtn = document.getElementById('notificationDropdown');
    const badge = document.getElementById('notificationBadge');
    const notificationList = document.getElementById('notificationList');
    const btnMarkAllRead = document.getElementById('btnMarkAllRead');
    const btnLoadMore = document.getElementById('btnLoadMoreNotifications');
    const endElem = document.getElementById('notificationEnd');

    if (!dropdownBtn || !notificationList || !btnLoadMore || !endElem) {
        return;
    }

    let skip = 0;
    const top = 5;
    let isLoadedOnce = false;
    let isLoading = false;
    let hasMore = true;

    // Cập nhật hiển thị badge trên quả chuông và nút "Đã đọc tất cả"
    function renderUnreadBadge(count) {
        const unreadCount = Math.max(0, count);
        if (badge) {
            if (unreadCount > 0) {
                badge.textContent = unreadCount > 9 ? '9+' : unreadCount.toString();
                badge.style.display = 'inline-block';
            } else {
                badge.style.display = 'none';
            }
        }
        if (btnMarkAllRead) {
            btnMarkAllRead.style.display = unreadCount > 0 ? 'inline-block' : 'none';
        }
    }

    // Luôn query lại từ API để đảm bảo số lượng chính xác 100% với database
    async function fetchUnreadCount() {
        try {
            const response = await fetch('/api/notifications/unread-count');
            let result;
            try {
                result = await response.json();
            } catch {
                return;
            }

            if (result.success) {
                renderUnreadBadge(result.data ?? 0);
            }
        } catch {
            // Âm thầm bỏ qua nếu lỗi kết nối
        }
    }

    // Kiểm tra số lượng khi vừa vào trang
    fetchUnreadCount();

    // Mỗi lần mở dropdown quả chuông: query lại số lượng chưa đọc mới nhất
    dropdownBtn.addEventListener('show.bs.dropdown', function () {
        fetchUnreadCount();
        if (!isLoadedOnce && !isLoading) {
            loadNotifications(true);
        }
    });

    dropdownBtn.addEventListener('click', function () {
        fetchUnreadCount();
        if (!isLoadedOnce && !isLoading) {
            loadNotifications(true);
        }
    });

    // Nút tải thêm 5 thông báo tiếp theo
    btnLoadMore.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        if (!isLoading && hasMore) {
            loadNotifications(false);
        }
    });

    // Nút "Đã đọc tất cả"
    if (btnMarkAllRead) {
        btnMarkAllRead.addEventListener('click', async function (e) {
            e.preventDefault();
            e.stopPropagation();

            if (btnMarkAllRead.disabled) return;
            btnMarkAllRead.disabled = true;
            const originalText = btnMarkAllRead.textContent;
            btnMarkAllRead.textContent = 'Đang xử lý...';

            try {
                const response = await fetch('/api/notifications/read-all', {
                    method: 'PUT'
                });

                let result;
                try {
                    result = await response.json();
                } catch {
                    throw new Error('Invalid response from server');
                }

                if (result.success) {
                    const unreadItems = notificationList.querySelectorAll('.notification-item.unread');
                    unreadItems.forEach(function (item) {
                        item.classList.remove('unread');
                        const dot = item.querySelector('.unread-dot');
                        if (dot) dot.remove();
                        const btn = item.querySelector('.btn-mark-read');
                        if (btn) btn.remove();
                    });

                    // Query lại số lượng từ server để đồng bộ chuẩn xác
                    await fetchUnreadCount();
                } else {
                    if (result.message) toastr.error(result.message);
                }
            } catch {
                toastr.error('Đã xảy ra lỗi kết nối. Vui lòng thử lại sau.');
            } finally {
                btnMarkAllRead.disabled = false;
                btnMarkAllRead.textContent = originalText;
            }
        });
    }

    // Đánh dấu đã đọc cho từng thông báo
    notificationList.addEventListener('click', async function (e) {
        const btn = e.target.closest('.btn-mark-read');
        if (!btn) return;

        e.preventDefault();
        e.stopPropagation();

        const id = btn.dataset.id;
        if (!id || btn.disabled) return;

        btn.disabled = true;
        btn.textContent = '...';

        try {
            const response = await fetch(`/api/notifications/${id}/read`, {
                method: 'PUT'
            });

            let result;
            try {
                result = await response.json();
            } catch {
                throw new Error('Invalid response from server');
            }

            if (result.success) {
                const itemElem = btn.closest('.notification-item');
                if (itemElem) {
                    itemElem.classList.remove('unread');
                    const dot = itemElem.querySelector('.unread-dot');
                    if (dot) dot.remove();
                    btn.remove();
                }

                // Query lại số lượng từ server để đồng bộ chuẩn xác
                await fetchUnreadCount();
            } else {
                if (result.message) toastr.error(result.message);
                btn.disabled = false;
                btn.textContent = 'Đã đọc';
            }
        } catch {
            toastr.error('Đã xảy ra lỗi kết nối. Vui lòng thử lại sau.');
            btn.disabled = false;
            btn.textContent = 'Đã đọc';
        }
    });

    async function loadNotifications(isFirstLoad) {
        isLoading = true;

        if (isFirstLoad) {
            skip = 0;
            notificationList.innerHTML = '<div class="notification-status">Đang tải...</div>';
            btnLoadMore.style.display = 'none';
            endElem.style.display = 'none';
        } else {
            btnLoadMore.disabled = true;
            btnLoadMore.textContent = 'Đang tải...';
        }

        try {
            const response = await fetch(`/api/notifications?skip=${skip}&top=${top}`);
            let result;
            try {
                result = await response.json();
            } catch {
                throw new Error('Invalid response from server');
            }

            if (result.success) {
                const items = result.data || [];

                if (isFirstLoad) {
                    notificationList.innerHTML = '';
                    isLoadedOnce = true;

                    if (items.length === 0) {
                        notificationList.innerHTML = '<div class="notification-status">Không có thông báo mới.</div>';
                        hasMore = false;
                        btnLoadMore.style.display = 'none';
                        endElem.style.display = 'none';
                        return;
                    }
                }

                // Render các thông báo
                items.forEach(function (item) {
                    const itemDiv = document.createElement('div');
                    const isUnread = !item.isRead;
                    itemDiv.className = 'notification-item' + (isUnread ? ' unread' : '');
                    itemDiv.dataset.id = item.id;

                    let topHtml = '<div class="notification-item-top">';
                    topHtml += '<div class="notification-title-wrap">';
                    if (isUnread) {
                        topHtml += '<span class="unread-dot" title="Chưa đọc"></span>';
                    }
                    if (item.title) {
                        topHtml += '<span class="title">' + escapeHtml(item.title) + '</span>';
                    }
                    topHtml += '</div>';

                    if (isUnread && item.id) {
                        topHtml += '<button type="button" class="btn-mark-read" data-id="' + item.id + '" title="Đánh dấu đã đọc">Đã đọc</button>';
                    }
                    topHtml += '</div>';

                    let html = topHtml;
                    html += '<div class="content">' + escapeHtml(item.content || '') + '</div>';
                    if (item.createdAt) {
                        html += '<div class="time">' + formatTime(item.createdAt) + '</div>';
                    }

                    itemDiv.innerHTML = html;
                    notificationList.appendChild(itemDiv);
                });

                skip += items.length;

                // Nếu trả về ít hơn `top` (5), coi như đã hết dữ liệu
                if (items.length < top) {
                    hasMore = false;
                    btnLoadMore.style.display = 'none';
                    endElem.style.display = 'block';
                } else {
                    hasMore = true;
                    btnLoadMore.style.display = 'block';
                    endElem.style.display = 'none';
                }

                // Tự động đưa con lăn xuống đáy khi tải thêm
                if (!isFirstLoad) {
                    setTimeout(function () {
                        notificationList.scrollTo({
                            top: notificationList.scrollHeight,
                            behavior: 'smooth'
                        });
                    }, 50);
                }
            } else {
                if (result.message) toastr.error(result.message);
                if (isFirstLoad) {
                    notificationList.innerHTML = '<div class="notification-status text-danger">' + escapeHtml(result.message || 'Không thể tải thông báo.') + '</div>';
                }
            }
        } catch {
            toastr.error('Đã xảy ra lỗi kết nối. Vui lòng thử lại sau.');
            if (isFirstLoad) {
                notificationList.innerHTML = '<div class="notification-status text-danger">Không thể tải thông báo.</div>';
            }
        } finally {
            isLoading = false;
            btnLoadMore.disabled = false;
            btnLoadMore.textContent = 'Tải thêm';
        }
    }

    function formatTime(dateStr) {
        try {
            const d = new Date(dateStr);
            if (isNaN(d.getTime())) return '';
            const day = String(d.getDate()).padStart(2, '0');
            const month = String(d.getMonth() + 1).padStart(2, '0');
            const year = d.getFullYear();
            const hours = String(d.getHours()).padStart(2, '0');
            const minutes = String(d.getMinutes()).padStart(2, '0');
            return `${hours}:${minutes} ${day}/${month}/${year}`;
        } catch {
            return '';
        }
    }

    function escapeHtml(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }
});
