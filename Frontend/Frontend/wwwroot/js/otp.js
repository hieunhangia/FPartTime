function initOtpHandler(options) {
    if (!options || !options.btnId || !options.handlerUrl || typeof options.cooldownSeconds === 'undefined') {
        console.error("initOtpHandler requires btnId, handlerUrl, and cooldownSeconds.");
        return;
    }
    
    const settings = options;
    
    const btnSendOtp = document.getElementById(settings.btnId);
    if (!btnSendOtp) return;

    btnSendOtp.addEventListener('click', async function () {
        const btn = this;
        const form = btn.closest('form');
        if (!form) return;

        const formData = new FormData(form);
        
        btn.disabled = true;
        const originalText = btn.innerText;
        btn.innerText = 'Đang gửi...';

        try {
            const response = await fetch(settings.handlerUrl, {
                method: 'POST',
                body: formData
            });

            let result;
            try {
                result = await response.json();
            } catch(e) {
                throw new Error('Invalid response from server');
            }

            if (result.success) {
                if (result.message) toastr.success(result.message);
                let timeLeft = settings.cooldownSeconds;
                const timer = setInterval(() => {
                    btn.innerText = `Gửi lại (${timeLeft}s)`;
                    timeLeft--;
                    if (timeLeft < 0) {
                        clearInterval(timer);
                        btn.disabled = false;
                        btn.innerText = originalText;
                    }
                }, 1000);
            } else {
                if (result.message) toastr.error(result.message);
                btn.disabled = false;
                btn.innerText = originalText;
            }

        } catch (error) {
            toastr.error('Đã xảy ra lỗi kết nối. Vui lòng thử lại sau.');
            btn.disabled = false;
            btn.innerText = originalText;
        }
    });
}
