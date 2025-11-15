using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using NewWeb.Data;
using NewWeb.Services.AUTH;
using NewWeb.Services.EMAILOTP;
using NewWeb.Services.MOMO;
using NewWeb.Services.VNPAY;
using NewWeb.Services.SIMPLECHAT;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Cấu hình Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Google";
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
})
.AddGoogle("Google", options =>
{
    options.ClientId = builder.Configuration["GoogleKeys:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["GoogleKeys:ClientSecret"] ?? "";
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
});

// Cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout 30 phút
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".TechStore.Session";
});

// Cấu hình Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký HttpContextAccessor (cần cho Session và các services khác)
builder.Services.AddHttpContextAccessor();

// Đăng ký các Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IMomoService, MomoService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<NewWeb.Services.SIMPLECHAT.LearningService>();
builder.Services.AddScoped<NewWeb.Services.SIMPLECHAT.SimpleChatService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Session middleware (phải đặt trước UseRouting)
app.UseSession();

app.UseRouting();

// Authentication & Authorization middleware (phải đặt theo thứ tự này)
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Map Google OAuth callback route
app.MapGet("/signin-google", async (HttpContext context) =>
{
    var returnUrl = context.Request.Query["returnUrl"].ToString();
    if (string.IsNullOrEmpty(returnUrl))
    {
        returnUrl = "/Account/GoogleLogin?handler=Callback";
    }
    return Results.Redirect(returnUrl);
});

// Map Chat API endpoint
app.MapPost("/api/chat/send", async (HttpContext context, SimpleChatService chatService) =>
{
    try
    {
        var request = await context.Request.ReadFromJsonAsync<ChatRequest>();
        
        if (request == null || string.IsNullOrEmpty(request.UserMessage))
        {
            return Results.BadRequest(new { error = "Tin nhắn không được để trống" });
        }

        var reply = await chatService.ChatAsync(request.UserMessage);

        return Results.Ok(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = reply }
                        }
                    }
                }
            }
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = "Có lỗi xảy ra khi gọi API", details = ex.Message });
    }
});

app.Run();

public class ChatRequest
{
    public string UserMessage { get; set; } = string.Empty;
}
