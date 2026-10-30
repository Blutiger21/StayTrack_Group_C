using Microsoft.AspNetCore.Authentication.Cookies;
using StayTrack.Models;
using StayTrack.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Razor View Engine to locate views in /web/Views as well as /Views
builder.Services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Insert(0, "/web/Views/{1}/{0}.cshtml");
    options.ViewLocationFormats.Insert(1, "/web/Views/Shared/{0}.cshtml");
});

// Bind Supabase configuration settings
builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection(SupabaseOptions.SectionName));

// Enable HttpContextAccessor for token resolution in services
builder.Services.AddHttpContextAccessor();

// Configure HttpClient for Supabase API interactions
builder.Services.AddHttpClient();

// Register StayTrack Authentication & Supabase Services
builder.Services.AddScoped<ISupabaseAuthService, SupabaseAuthService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();

// Enable In-Memory Cache and Secure Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "StayTrack.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Configure Security: HttpOnly Authentication Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "StayTrack.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Configure Anti-Forgery Token protection
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "StayTrack.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
