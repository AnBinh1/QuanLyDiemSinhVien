
/* =========================================================
   CHATBOT - TRỢ LÝ ẢO ĐẠI HỌC VINH
   File: wwwroot/js/chatbot.js

   Chức năng:
   - Mở / đóng chatbot
   - Gửi câu hỏi đến Backend
   - Kết nối Groq API thông qua ChatbotController
   - Hiển thị Markdown an toàn
   - Quản lý lịch sử hội thoại
   - Xử lý lỗi và trạng thái chờ
   ========================================================= */

(function () {

    "use strict";


    // =====================================================
    // 1. KHỞI TẠO CHATBOT
    // =====================================================

    const chatbot = document.getElementById("vinh-chatbot");

    if (!chatbot) return;

    const toggleButton = chatbot.querySelector(".vinh-chatbot-btn");
    const chatBody = chatbot.querySelector(".vinh-chatbot-body");
    const input = chatbot.querySelector(".vinh-chatbot-input input");
    const sendButton = chatbot.querySelector(".vinh-chatbot-input button");

    if (!toggleButton || !chatBody || !input || !sendButton) return;


    // =====================================================
    // 2. LỊCH SỬ HỘI THOẠI
    // =====================================================

    const chatHistory = [];

    let isSending = false;


    // =====================================================
    // 3. CẤU HÌNH MARKDOWN
    // =====================================================

    if (window.marked) {

        window.marked.setOptions({
            gfm: true,
            breaks: true
        });

    }


    // =====================================================
    // 4. MÃ HÓA HTML
    // =====================================================

    function escapeHTML(value) {

        return String(value).replace(/[&<>"']/g, function (char) {

            return {
                "&": "&amp;",
                "<": "&lt;",
                ">": "&gt;",
                '"': "&quot;",
                "'": "&#039;"
            }[char];

        });

    }


    // =====================================================
    // 5. CHUYỂN MARKDOWN THÀNH HTML AN TOÀN
    // =====================================================

    function renderMarkdown(content) {

        if (!content) return "";

        // Hiển thị văn bản an toàn nếu thư viện chưa tải được.

        if (!window.marked || !window.DOMPurify) {

            return escapeHTML(content).replace(/\r?\n/g, "<br>");

        }

        // Chuyển Markdown thành HTML.

        const html = window.marked.parse(content, {
            gfm: true,
            breaks: true
        });

        // Làm sạch HTML để hạn chế XSS.

        return window.DOMPurify.sanitize(html, {
            USE_PROFILES: { html: true }
        });

    }


    // =====================================================
    // 6. CUỘN CHAT XUỐNG CUỐI
    // =====================================================

    function scrollToBottom() {

        chatBody.scrollTop = chatBody.scrollHeight;

    }


    // =====================================================
    // 7. TẠO TIN NHẮN
    // =====================================================

    function addMessage(content, role, isError = false) {

        const message = document.createElement("div");

        message.className =
            "vinh-chatbot-message " +
            (role === "user"
                ? "vinh-chatbot-user"
                : "vinh-chatbot-assistant");

        const messageContent = document.createElement("div");

        messageContent.className = "vinh-chatbot-message-content";

        if (role === "user") {

            // Tin nhắn người dùng chỉ hiển thị văn bản thuần.

            messageContent.textContent = content;

        } else {

            // Tin nhắn AI được hiển thị dưới dạng Markdown.

            messageContent.innerHTML = renderMarkdown(content);

        }

        if (isError) {

            messageContent.classList.add("vinh-chatbot-error");

        }

        message.appendChild(messageContent);

        chatBody.appendChild(message);

        scrollToBottom();

        return message;

    }


    // =====================================================
    // 8. HIỂN THỊ TRẠNG THÁI ĐANG SUY NGHĨ
    // =====================================================

    function showTyping() {

        const typing = document.createElement("div");

        typing.className =
            "vinh-chatbot-message vinh-chatbot-assistant";

        const content = document.createElement("div");

        content.className =
            "vinh-chatbot-message-content vinh-chatbot-typing";

        content.textContent = "Trợ lý đang suy nghĩ...";

        typing.appendChild(content);

        chatBody.appendChild(typing);

        scrollToBottom();

        return typing;

    }


    // =====================================================
    // 9. GỌI BACKEND CHATBOT
    // =====================================================

    async function askChatbot(message) {

        const response = await fetch("/api/Chatbot/ask", {

            method: "POST",

            headers: {
                "Content-Type": "application/json"
            },

            body: JSON.stringify({

                message: message,

                history: chatHistory.slice(-12)

            })

        });

        let data;

        try {

            data = await response.json();

        } catch {

            throw new Error("Máy chủ trả về dữ liệu không hợp lệ.");

        }

        if (!response.ok) {

            throw new Error(
                data.error || "Không thể gửi câu hỏi. Bạn vui lòng thử lại."
            );

        }

        if (typeof data.reply !== "string" || !data.reply.trim()) {

            throw new Error("Trợ lý chưa có câu trả lời.");

        }

        // Chỉ lưu lịch sử khi nhận được phản hồi hợp lệ.

        chatHistory.push(

            { role: "user", content: message },

            { role: "assistant", content: data.reply }

        );

        return data.reply;

    }


    // =====================================================
    // 10. XỬ LÝ GỬI TIN NHẮN
    // =====================================================

    async function sendMessage() {

        const message = input.value.trim();

        if (!message || isSending) return;

        if (message.length > 2000) {

            addMessage(
                "Câu hỏi quá dài. Bạn vui lòng nhập tối đa 2.000 ký tự.",
                "assistant",
                true
            );

            return;

        }

        isSending = true;

        input.value = "";

        input.disabled = true;

        sendButton.disabled = true;

        // Hiển thị câu hỏi người dùng.

        addMessage(message, "user");

        // Hiển thị trạng thái chờ.

        const typing = showTyping();

        try {

            // Gửi câu hỏi đến Backend.

            const reply = await askChatbot(message);

            typing.remove();

            // Hiển thị câu trả lời AI.

            addMessage(reply, "assistant");

        } catch (error) {

            typing.remove();

            console.error("Chatbot error:", error);

            addMessage(
                error.message || "Đã xảy ra lỗi. Bạn vui lòng thử lại.",
                "assistant",
                true
            );

        } finally {

            isSending = false;

            input.disabled = false;

            sendButton.disabled = false;

            input.focus();

        }

    }


    // =====================================================
    // 11. SỰ KIỆN GỬI TIN NHẮN
    // =====================================================

    sendButton.addEventListener("click", sendMessage);

    input.addEventListener("keydown", function (event) {

        if (event.key === "Enter" && !event.shiftKey) {

            event.preventDefault();

            sendMessage();

        }

    });


    // =====================================================
    // 12. ĐÓNG / MỞ CHATBOT
    // =====================================================

    toggleButton.addEventListener("click", function () {

        const isOpen = chatbot.classList.toggle("is-open");

        toggleButton.setAttribute(
            "aria-expanded",
            String(isOpen)
        );

        if (isOpen) {

            input.focus();

        }

    });

})();
