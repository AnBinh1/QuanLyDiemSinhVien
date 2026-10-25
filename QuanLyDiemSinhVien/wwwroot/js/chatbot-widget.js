/* =========================================================
   VINH UNIVERSITY - CHATBOT WIDGET (AI - tra loi tu nhien)
   Goi API backend /api/chatbot/ask, backend se goi Anthropic Claude.
   ========================================================= */

(function () {
    "use strict";

    var API_ENDPOINT = "/api/chatbot/ask";

    var SUGGESTED_QUESTIONS = [
        "Thông tin tuyển sinh năm nay như thế nào?",
        "Làm sao để đăng ký học phần?",
        "Học phí đóng ở đâu?",
        "Quên mật khẩu Cổng sinh viên thì làm sao?"
    ];

    var WELCOME_MESSAGE =
        "Xin chào! 👋 Mình là trợ lý ảo của Trường Đại học Vinh.\n" +
        "Bạn cứ hỏi mình bất cứ điều gì về tuyển sinh, học phí, đăng ký học phần... như đang trò chuyện bình thường nhé!";

    var ERROR_MESSAGE =
        "Xin lỗi, hiện mình đang gặp sự cố kết nối. Bạn vui lòng thử lại sau ít phút, " +
        "hoặc liên hệ hotline 0238 3855 452 để được hỗ trợ trực tiếp.";

    document.addEventListener("DOMContentLoaded", function () {
        var root = document.getElementById("vinh-chatbot");
        if (!root) return;

        var toggleBtn = root.querySelector(".vinh-chatbot-btn");
        var body = root.querySelector(".vinh-chatbot-body");
        var input = root.querySelector(".vinh-chatbot-input input");
        var sendBtn = root.querySelector(".vinh-chatbot-input button");
        var badge = root.querySelector(".vinh-chatbot-badge");

        var started = false;
        var isSending = false;
        var history = [];

        function scrollToBottom() {
            body.scrollTop = body.scrollHeight;
        }

        function addMessage(text, sender) {
            var msg = document.createElement("div");
            msg.className = "vc-msg " + sender;
            msg.textContent = text;
            body.appendChild(msg);
            scrollToBottom();
            return msg;
        }

        function addSuggestedQuestions() {
            var wrap = document.createElement("div");
            wrap.className = "vc-quick-replies";
            SUGGESTED_QUESTIONS.forEach(function (q) {
                var chip = document.createElement("button");
                chip.type = "button";
                chip.className = "vc-chip";
                chip.textContent = q;
                chip.addEventListener("click", function () {
                    sendMessage(q);
                });
                wrap.appendChild(chip);
            });
            body.appendChild(wrap);
            scrollToBottom();
        }

        function showTyping() {
            var typing = document.createElement("div");
            typing.className = "vc-typing";
            typing.innerHTML = "<span></span><span></span><span></span>";
            body.appendChild(typing);
            scrollToBottom();
            return typing;
        }

        function setSending(state) {
            isSending = state;
            sendBtn.disabled = state;
            input.disabled = state;
        }

        function sendMessage(text) {
            text = (text || "").trim();
            if (!text || isSending) return;

            addMessage(text, "user");
            input.value = "";
            setSending(true);

            var typingEl = showTyping();

            fetch(API_ENDPOINT, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ message: text, history: history })
            })
                .then(function (res) {
                    return res.json().then(function (data) {
                        if (!res.ok) throw new Error(data.error || "Request failed");
                        return data;
                    });
                })
                .then(function (data) {
                    typingEl.remove();
                    var reply = data.reply || ERROR_MESSAGE;
                    addMessage(reply, "bot");

                    history.push({ role: "user", content: text });
                    history.push({ role: "assistant", content: reply });
                })
                .catch(function () {
                    typingEl.remove();
                    addMessage(ERROR_MESSAGE, "bot");
                })
                .finally(function () {
                    setSending(false);
                    input.focus();
                });
        }

        function startConversation() {
            if (started) return;
            started = true;
            addMessage(WELCOME_MESSAGE, "bot");
            addSuggestedQuestions();
        }

        function openPanel() {
            root.classList.add("open");
            if (badge) badge.style.display = "none";
            startConversation();
            setTimeout(function () { input.focus(); }, 200);
        }

        function closePanel() {
            root.classList.remove("open");
        }

        toggleBtn.addEventListener("click", function () {
            if (root.classList.contains("open")) {
                closePanel();
            } else {
                openPanel();
            }
        });

        sendBtn.addEventListener("click", function () {
            sendMessage(input.value);
        });

        input.addEventListener("keydown", function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
                sendMessage(input.value);
            }
        });
    });
})();