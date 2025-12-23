using Microsoft.EntityFrameworkCore;
using SinavPortaliFinal.Models;
using SinavPortaliFinal.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 1. Veritabaný Baðlantýsý
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Identity (Üyelik) Sistemi
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3; // Þifre 123 olabilsin
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AppDbContext>();

// REPOSITORY SERVÝSLERÝ
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// --- YETKÝ AYARLARI ---
builder.Services.ConfigureApplicationCookie(options =>
{
    // Giriþ yapmamýþsa buraya gitsin
    options.LoginPath = "/Login/Index";

    // !!! BURASI ÖNEMLÝ: Yetkisi yoksa buraya gitsin !!!
    options.AccessDeniedPath = "/Login/AccessDenied";
});

// 3. MVC
builder.Services.AddControllersWithViews();

builder.Services.AddSignalR(); // 1. Servisi Ekle

var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Kimlik Kontrolü
app.UseAuthorization();  // Yetki Kontrolü

app.MapHub<SinavPortaliFinal.Hubs.DashboardHub>("/dashboardHub"); // 2. Yolu Tanýt
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();