using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SinavPortaliFinal.Models;

namespace SinavPortaliFinal.Controllers
{
    // Sadece 'Öğrenci' rolü olanlar girebilir
    [Authorize(Roles = "Öğrenci")]
    public class StudentController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public StudentController(UserManager<AppUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // ==========================================
        //       ÖĞRENCİ ANA SAYFASI (SINAVLAR)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            // 1. Giriş yapan öğrenciyi bul
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // 2. Bu öğrenciye atanan derslerin ID'lerini bul (UserCategory tablosundan)
            var assignedCategoryIds = _context.UserCategories
                                              .Where(uc => uc.AppUserId == user.Id)
                                              .Select(uc => uc.CategoryId)
                                              .ToList();

            // 3. Sadece bu derslere ait sınavları getir
            // (Include ile ders adını da çekiyoruz ki ekranda yazabilelim)
            var myExams = _context.Exams
                                  .Include(x => x.Category)
                                  .Where(exam => assignedCategoryIds.Contains(exam.CategoryId))
                                  .ToList();

            // 4. Öğrencinin daha önce girdiği sınav sonuçlarını da çekelim (İleride kullanacağız)
            // Bu kısım şimdilik sadece sınav listesi için yeterli.

            return View(myExams);
        }

        // ==========================================
        //           PROFİLİM SAYFASI
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> MyProfile(string? NewPassword)
        {
            // Giriş yapan öğrenciyi bul
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Sadece şifre alanı doluysa işlem yapıyoruz
            if (!string.IsNullOrEmpty(NewPassword))
            {
                // Eski şifreyi sil, yenisini ekle (Token derdi olmadan)
                if (await _userManager.HasPasswordAsync(user))
                {
                    await _userManager.RemovePasswordAsync(user);
                }

                var result = await _userManager.AddPasswordAsync(user, NewPassword);

                if (result.Succeeded)
                {
                    // Güvenlik damgasını güncelle (Oturum güvenliği için)
                    await _userManager.UpdateSecurityStampAsync(user);

                    TempData["BasariliMesaj"] = "Şifreniz başarıyla güncellendi.";
                    // Şifre değişince genelde tekrar giriş yapılması istenir ama 
                    // şimdilik sayfada kalsın istiyorsan:
                    return RedirectToAction("MyProfile");
                }
                else
                {
                    // Şifre kurallara uymuyorsa (çok kısaysa vs.)
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }

            // Eğer şifre boşsa hiçbir şey yapmadan sayfayı yenile
            return RedirectToAction("MyProfile");
        }
        // ==========================================
        //          SINAV EKRANI (GET)
        // ==========================================
        [HttpGet]
        public IActionResult JoinExam(int id)
        {
            // 1. Sınavı ve SORULARINI çekiyoruz (Include şart!)
            var exam = _context.Exams
                               .Include(x => x.Questions) // Soruları da getir
                               .Include(x => x.Category)  // Ders adını da getir
                               .FirstOrDefault(x => x.Id == id);

            // 2. Sınav yoksa ana sayfaya at
            if (exam == null)
            {
                return RedirectToAction("Index");
            }

            return View(exam);
        }

        // ==========================================
        //      SINAVI BİTİRME / PUANLAMA (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> FinishExam(int examId, Dictionary<int, string> answers)
        {
            // BU KISMI BİR SONRAKİ ADIMDA KODLAYACAĞIZ
            // Şimdilik sadece "Sınav Bitti" desin yeter.
            return Content("Sınav bitti! Cevaplarınız alındı. Puanlama sistemi bir sonraki adımda yapılacak.");
        }
    }
}