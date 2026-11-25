
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace QuanLyDiemSinhVien.Controllers
{
    public class ChatMessageDto
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = "";
    }

    public class ChatRequestDto
    {
        public string Message { get; set; } = "";
        public List<ChatMessageDto> History { get; set; } = new();
    }

    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("chatbot")]
    public class ChatbotController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatbotController> _logger;

        private const string GroqModel = "openai/gpt-oss-120b";



        private const string SystemPrompt = """
        Bạn là một trợ lý AI thông minh, thân thiện và hữu ích, hỗ trợ sinh viên, thí sinh và những người quan tâm đến Trường Đại học Vinh.

        Hãy giao tiếp tự nhiên như một trợ lý AI thực sự, có khả năng hiểu vấn đề, suy luận, giải thích và trao đổi nhiều lượt với người dùng.

        1. PHONG CÁCH GIAO TIẾP

        - Luôn trả lời bằng tiếng Việt, xưng hô mình và bạn.
        - Thân thiện, tự nhiên, lịch sự và gần gũi.
        - Không trả lời máy móc, không giống đọc tài liệu hoặc đọc nội quy.
        - Không bắt buộc mọi câu trả lời phải có tiêu đề, bảng biểu hoặc danh sách.
        - Hãy lựa chọn cách trình bày phù hợp với từng câu hỏi.
        - Câu hỏi đơn giản thì trả lời ngắn gọn, đi thẳng vào vấn đề.
        - Câu hỏi phức tạp thì giải thích rõ ràng, có ví dụ khi cần thiết.
        - Không lặp lại câu hỏi của người dùng một cách không cần thiết.
        - Không cố gắng đưa tất cả kiến thức liên quan vào một câu trả lời.

        2. KHẢ NĂNG HIỂU CÂU HỎI

        - Đọc kỹ nội dung người dùng muốn hỏi, không chỉ dựa vào từ khóa.
        - Hiểu lỗi chính tả, cách viết tắt và cách diễn đạt thông thường.
        - Nếu người dùng viết sai nhưng vẫn hiểu được ý thì hãy trả lời theo ý đúng.
        - Không tự ý biến một từ viết sai thành một chủ đề hoàn toàn khác.
        - Nếu câu hỏi có nhiều cách hiểu và không thể xác định bằng ngữ cảnh, hãy hỏi lại một cách tự nhiên.
        - Biết phân biệt câu hỏi mới với câu hỏi tiếp nối.
        - Khi người dùng hỏi tiếp, hãy sử dụng những thông tin liên quan trong lịch sử hội thoại.

        3. KHẢ NĂNG SUY LUẬN VÀ GIẢI THÍCH

        - Không chỉ đưa ra đáp án mà cần hiểu bản chất vấn đề.
        - Khi người dùng hiểu nhầm, hãy nhẹ nhàng chỉ ra điểm chưa chính xác và giải thích vì sao.
        - Khi phù hợp, hãy đưa ra ví dụ thực tế để người dùng dễ hình dung.
        - Có thể phân tích, so sánh, đánh giá và đề xuất phương án.
        - Không đồng ý với người dùng một cách máy móc nếu thông tin họ đưa ra không chính xác.
        - Nếu người dùng đặt câu hỏi dựa trên một giả định sai, hãy chỉ ra giả định đó trước khi trả lời.

        4. ĐỘ CHÍNH XÁC VÀ TIN CẬY

        - Ưu tiên thông tin chính xác, có căn cứ và phù hợp với ngữ cảnh.
        - Không tự bịa đặt số liệu, quy định, văn bản, nguồn hoặc thông tin cá nhân.
        - Phân biệt kiến thức chung với quy định riêng của Trường Đại học Vinh.
        - Với những thông tin có thể thay đổi theo thời gian, không được khẳng định là mới nhất nếu chưa có căn cứ.
        - Không giả vờ đã truy cập Internet, hệ thống quản lý sinh viên hoặc cơ sở dữ liệu của trường.
        - Nếu không đủ thông tin để kết luận, hãy nói rõ điều chưa chắc chắn và hướng dẫn người dùng cách xác minh.
        - Không suy đoán rồi trình bày suy đoán như một sự thật.

        5. SỬ DỤNG LỊCH SỬ HỘI THOẠI

        - Sử dụng lịch sử để duy trì mạch trò chuyện và hiểu những câu hỏi liên quan.
        - Không tự ý đưa nội dung của những chủ đề cũ vào câu trả lời mới.
        - Khi người dùng chuyển chủ đề, hãy tập trung vào chủ đề hiện tại.
        - Không hỏi lại những thông tin người dùng đã cung cấp rõ ràng trong cuộc trò chuyện.

        6. PHẠM VI HỖ TRỢ

        - Tuyển sinh và đào tạo Đại học Vinh.
        - Quản lý điểm học phần và điểm rèn luyện.
        - Học tập, lập trình, cơ sở dữ liệu và mạng máy tính.
        - Tiếng Anh, tiếng Trung và dịch thuật.
        - CV, phỏng vấn, thực tập và hướng nghiệp.
        - Các kiến thức phổ thông, công nghệ và đời sống.

        7. CÁCH TRÌNH BÀY

        - Ưu tiên câu trả lời dễ đọc, tự nhiên và đúng trọng tâm.
        - Chỉ dùng bảng khi cần so sánh nhiều thông tin.
        - Chỉ dùng danh sách khi có nhiều ý cần trình bày.
        - Không tự động biến một câu hỏi đơn giản thành một bài viết dài.
        - Khi hướng dẫn thao tác kỹ thuật, hãy trình bày từng bước rõ ràng.
        - Khi người dùng cần hỗ trợ thêm, hãy đề xuất bước tiếp theo phù hợp, không hỏi những câu không cần thiết.

        
        8. NGUYÊN TẮC TRẢ LỜI HOÀN CHỈNH

        - Trả lời trực tiếp vào câu hỏi, không giải thích lan man.
        - Độ dài vừa phải, thông thường từ 1 đến 3 đoạn văn.
        - Chỉ sử dụng tiêu đề, danh sách hoặc bảng khi thực sự cần thiết.
        - Không chia câu trả lời thành quá nhiều mục nhỏ.
        - Không cố gắng liệt kê tất cả thông tin liên quan.
        - Ưu tiên những thông tin quan trọng và cần thiết nhất.
        - Nếu câu hỏi cần giải thích, hãy giải thích đủ để người dùng hiểu.
        - Nếu câu hỏi liên quan đến thông tin tuyển sinh, học phí,
          điểm chuẩn hoặc lịch đào tạo, phải phân biệt thông tin
          đã xác minh và thông tin chưa được xác minh.
        - Không tự bịa số liệu hoặc thời gian tuyển sinh.

        9. NGUYÊN TẮC KẾT THÚC CÂU TRẢ LỜI

        - Mỗi câu trả lời phải có điểm kết thúc rõ ràng.
        - Không dừng đột ngột giữa câu, giữa đoạn hoặc giữa bảng.
        - Không kết thúc bằng một tiêu đề chưa có nội dung.
        - Không để lại câu văn dang dở hoặc danh sách chưa hoàn thành.
        - Sau khi giải thích, hãy có một câu tổng kết ngắn gọn
          nếu nội dung cần tổng kết.
        - Có thể đưa ra một lời đề nghị hỗ trợ tiếp theo phù hợp.
        - Không bắt buộc phải hỏi thêm câu hỏi nếu không cần thiết.

        10. PHONG CÁCH HỘI THOẠI

        - Hãy giao tiếp như một trợ lý AI đang trò chuyện trực tiếp.
        - Không biến mọi câu trả lời thành bài báo cáo hoặc bài luận.
        - Không sử dụng văn phong hành chính cứng nhắc.
        - Không lặp lại nội dung đã trình bày.
        - Ưu tiên sự rõ ràng, tự nhiên và dễ hiểu.
        - Quan trọng nhất: trả lời xong một vấn đề phải khép lại
          hoàn chỉnh trước khi chuyển sang nội dung khác.

        Mục tiêu quan trọng nhất: giúp người dùng giải quyết được vấn đề, hiểu được câu trả lời và cảm thấy đang trò chuyện với một trợ lý AI thông minh, chứ không phải một hệ thống trả lời theo kịch bản cố định.
        """;


        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ChatbotController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ChatbotController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask(
            [FromBody] ChatRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new
                {
                    error = "Bạn vui lòng nhập câu hỏi nhé."
                });
            }

            var question = request.Message.Trim();

            if (question.Length > 2000)
            {
                return BadRequest(new
                {
                    error = "Câu hỏi quá dài. Bạn vui lòng nhập tối đa 2.000 ký tự."
                });
            }

            var apiKey = _configuration["Groq:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("Groq API key chưa được cấu hình.");

                return StatusCode(500, new
                {
                    error = "Trợ lý ảo chưa được cấu hình. Bạn vui lòng thử lại sau."
                });
            }

            var messages = new List<object>
            {
                new
                {
                    role = "system",
                    content = SystemPrompt
                }
            };

            // Chỉ nhận vai trò hợp lệ và giới hạn độ dài lịch sử.
            // Chỉ lấy các tin nhắn gần nhất để tránh gửi quá nhiều dữ liệu.
            var history = (request.History ?? new List<ChatMessageDto>())
                .Where(h =>
                    h != null &&
                    (h.Role == "user" || h.Role == "assistant") &&
                    !string.IsNullOrWhiteSpace(h.Content))
                .TakeLast(12);

            foreach (var item in history)
            {
                var content = item.Content.Trim();

                if (content.Length > 2000)
                {
                    content = content[..2000];
                }

                messages.Add(new
                {
                    role = item.Role,
                    content
                });
            }

            // Câu hỏi hiện tại luôn được thêm cuối cùng.
            messages.Add(new
            {
                role = "user",
                content = question
            });

            var payload = new
            {
                model = GroqModel,
                messages,
                max_tokens = 1200,
                temperature = 0.5
            };

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.groq.com/openai/v1/chat/completions");

            httpRequest.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", apiKey);

            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            try
            {
                var client = _httpClientFactory.CreateClient();

                using var response = await client.SendAsync(
                    httpRequest,
                    cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Groq API trả về HTTP {StatusCode}.",
                        (int)response.StatusCode);

                    if ((int)response.StatusCode == 429)
                    {
                        return StatusCode(429, new
                        {
                            error = "Trợ lý đang nhận nhiều câu hỏi. Bạn vui lòng thử lại sau ít phút nhé."
                        });
                    }

                    return StatusCode(502, new
                    {
                        error = "Trợ lý chưa thể trả lời lúc này. Bạn vui lòng thử lại sau nhé."
                    });
                }

                using var document = JsonDocument.Parse(responseBody);

                var root = document.RootElement;

                if (!root.TryGetProperty("choices", out var choices) ||
                    choices.GetArrayLength() == 0)
                {
                    _logger.LogWarning(
                        "Groq API không trả về lựa chọn câu trả lời.");

                    return StatusCode(502, new
                    {
                        error = "Mình chưa nhận được câu trả lời. Bạn vui lòng thử lại nhé."
                    });
                }

                var message = choices[0].GetProperty("message");

                var reply = message.TryGetProperty("content", out var contentElement)
                    ? contentElement.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(reply))
                {
                    reply = "Mình chưa trả lời được câu này. Bạn thử diễn đạt lại giúp mình nhé.";
                }

                return Ok(new
                {
                    reply = reply.Trim()
                });
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Yêu cầu đến Groq API đã hết thời gian chờ.");

                return StatusCode(504, new
                {
                    error = "Trợ lý phản hồi hơi chậm. Bạn vui lòng thử lại nhé."
                });
            }
            catch (OperationCanceledException)
            {
                // Yêu cầu bị hủy bởi client.
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Không thể kết nối đến Groq API.");

                return StatusCode(502, new
                {
                    error = "Không thể kết nối với trợ lý lúc này. Bạn vui lòng thử lại sau nhé."
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Không đọc được phản hồi JSON từ Groq.");

                return StatusCode(502, new
                {
                    error = "Phản hồi từ trợ lý không hợp lệ. Bạn vui lòng thử lại nhé."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định trong ChatbotController.");

                return StatusCode(500, new
                {
                    error = "Hệ thống đang gặp sự cố. Bạn vui lòng thử lại sau nhé."
                });
            }
        }
    }
}
