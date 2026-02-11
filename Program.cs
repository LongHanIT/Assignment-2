using HarvestHavenSecurePortal.Data;
using HarvestHavenSecurePortal.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages
builder.Services.AddRazorPages();

// EF Core + SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Data Protection + app services
builder.Services.AddDataProtection();
builder.Services.AddScoped<CryptoService>();
builder.Services.AddScoped<PasswordPolicyService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<CaptchaV3Service>();

// HttpClient for reCAPTCHA
builder.Services.AddHttpClient();

var app = builder.Build();

// ✅ Apply EF migrations on startup (better than EnsureCreated)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// ✅ Error handling / HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Errors/Error"); // custom 500 page
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ Session BEFORE endpoints
app.UseSession();

// Authorization (fine even if you're using session auth)
app.UseAuthorization();

// ✅ 404/403 handling
app.UseStatusCodePagesWithReExecute("/Errors/{0}");

app.MapRazorPages();

app.Run();