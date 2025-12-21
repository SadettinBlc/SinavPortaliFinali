using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SinavPortaliFinal.Models;

namespace SinavPortaliFinal.Controllers
{
    public class LoginController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<AppRole> _roleManager;

        // Identity servislerini içeri alıyoruz
        public LoginController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<AppRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password)
        {
            // Identity ile Şifre Kontrolü
            var result = await _signInManager.PasswordSignInAsync(username, password, false, false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(username);

                // Rolüne göre yönlendir
                if (await _userManager.IsInRoleAsync(user, "Öğrenci"))
                {
                    // Öğrenci Paneli (Henüz yapmadık ama yönlendirmesi hazır olsun)
                    return RedirectToAction("Index", "StudentPanel");
                }

                // Müdür ve Öğretmen Admin Paneline gider
                return RedirectToAction("Index", "Admin");
            }

            ViewBag.Hata = "Kullanıcı adı veya şifre yanlış!";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index");
        }

        public IActionResult AccessDenied()
        {
            return Content("Bu sayfaya erişim yetkiniz yok! (Access Denied)");
        }

        // --- İLK KURULUM İÇİN SİHİRLİ METOT ---
        public async Task<IActionResult> CreateDemo()
        {
            // 1. Rolleri Oluştur
            if (!await _roleManager.RoleExistsAsync("Müdür")) await _roleManager.CreateAsync(new AppRole { Name = "Müdür" });
            if (!await _roleManager.RoleExistsAsync("Öğretmen")) await _roleManager.CreateAsync(new AppRole { Name = "Öğretmen" });
            if (!await _roleManager.RoleExistsAsync("Öğrenci")) await _roleManager.CreateAsync(new AppRole { Name = "Öğrenci" });

            // 2. Kullanıcıları Oluştur (Şifreler: 123)
            // Müdür
            if (await _userManager.FindByNameAsync("mudur") == null)
            {
                var u = new AppUser { UserName = "mudur", Name = "Mahmut", Surname = "Hoca" };
                await _userManager.CreateAsync(u, "123");
                await _userManager.AddToRoleAsync(u, "Müdür");
            }
            // Öğretmen
            if (await _userManager.FindByNameAsync("ogretmen") == null)
            {
                var u = new AppUser { UserName = "ogretmen", Name = "Badi", Surname = "Ekrem" };
                await _userManager.CreateAsync(u, "123");
                await _userManager.AddToRoleAsync(u, "Öğretmen");
            }
            // Öğrenci
            if (await _userManager.FindByNameAsync("ogrenci") == null)
            {
                var u = new AppUser { UserName = "ogrenci", Name = "İnek", Surname = "Şaban" };
                await _userManager.CreateAsync(u, "123");
                await _userManager.AddToRoleAsync(u, "Öğrenci");
            }

            return Content("Demo Kullanıcılar Oluşturuldu! Giriş yapabilirsiniz.");
        }
    }
}