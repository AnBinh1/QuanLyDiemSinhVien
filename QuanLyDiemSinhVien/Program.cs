using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyDiemSinhVien.Repository;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration["ConnectionStrings:ConnectedDb"]);
});

// Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<DataContext>();

// Cookie login
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// MVC
builder.Services.AddControllersWithViews();

// THÊM MỚI: cho phép gọi HTTP ra ngoài (để gọi Gemini API)
builder.Services.AddHttpClient();

// THÊM MỚI: giới hạn số câu hỏi/phút mỗi IP cho chatbot, tránh bị spam
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("chatbot", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseRouting();

// THÊM MỚI: bật rate limiting
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();



// Area routes
app.MapControllerRoute(
    name: "Areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// THÊM MỚI: bật route cho ChatbotController (kiểu API)
app.MapControllers();

app.Run();