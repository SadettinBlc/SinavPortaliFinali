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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // 1. Öğrenciye atanan dersleri bul
            var assignedCategoryIds = _context.UserCategories
                                              .Where(uc => uc.AppUserId == user.Id)
                                              .Select(uc => uc.CategoryId)
                                              .ToList();

            // 2. Bu derslerin sınavlarını getir
            var myExams = _context.Exams
                                  .Include(x => x.Category)
                                  .Where(exam => assignedCategoryIds.Contains(exam.CategoryId))
                                  .ToList();

            // 3. Öğrencinin daha önce girdiği sınavların ID'lerini buluyoruz
            var takenExamIds = _context.ExamResults
                                       .Where(x => x.AppUserId == user.Id)
                                       .Select(x => x.ExamId)
                                       .ToList();

            // Bu listeyi View tarafına gönderiyoruz ki butonu değiştirelim
            ViewBag.TakenExamIds = takenExamIds;

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
                // Eski şifreyi sil, yenisini ekle
                if (await _userManager.HasPasswordAsync(user))
                {
                    await _userManager.RemovePasswordAsync(user);
                }

                var result = await _userManager.AddPasswordAsync(user, NewPassword);

                if (result.Succeeded)
                {
                    // Güvenlik damgasını güncelle
                    await _userManager.UpdateSecurityStampAsync(user);

                    TempData["BasariliMesaj"] = "Şifreniz başarıyla güncellendi.";
                    return RedirectToAction("MyProfile");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }

            return RedirectToAction("MyProfile");
        }

        // ==========================================
        //          SINAV EKRANI (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> JoinExam(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            // Eğer kullanıcı oturumu düşmüşse Login'e gönder
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // --- GÜVENLİK KONTROLÜ 1: Daha önce girmiş mi? ---
            var existingResult = _context.ExamResults.FirstOrDefault(x => x.ExamId == id && x.AppUserId == user.Id);

            if (existingResult != null)
            {
                // Eğer girmişse, direkt sonuç sayfasına yönlendir
                return View("Result", existingResult);
            }

            // Sınavı getir
            var exam = _context.Exams
                               .Include(x => x.Questions)
                               .Include(x => x.Category)
                               .FirstOrDefault(x => x.Id == id);

            if (exam == null) return RedirectToAction("Index");

            // ===============================================
            //        TARİH ARALIĞI KONTROLÜ (YENİ EKLENDİ)
            // ===============================================
            var simdi = DateTime.Now;

            // 1. Sınav henüz başlamadıysa
            if (simdi < exam.StartDate)
            {
                TempData["Hata"] = $"Sınav henüz başlamadı! Başlangıç: {exam.StartDate.ToString("dd.MM.yyyy HH:mm")}";
                return RedirectToAction("Index");
            }

            // 2. Sınav süresi bittiyse
            if (simdi > exam.EndDate)
            {
                TempData["Hata"] = $"Sınav süresi doldu! Bitiş: {exam.EndDate.ToString("dd.MM.yyyy HH:mm")}";
                return RedirectToAction("Index");
            }
            // ===============================================

            return View(exam);
        }

        // ==========================================
        //      SINAVI BİTİRME / PUANLAMA (POST)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> FinishExam(int examId, Dictionary<int, string> answers)
        {
            // 1. Giriş yapan öğrenciyi bul
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // 2. Sınavı ve Doğru Cevapları Çek
            var exam = _context.Exams
                               .Include(x => x.Questions)
                               .FirstOrDefault(x => x.Id == examId);

            if (exam == null) return RedirectToAction("Index");

            // 3. Puanlama Mantığı
            int dogruSayisi = 0;
            int yanlisSayisi = 0;

            foreach (var question in exam.Questions)
            {
                // Öğrenci bu soruya cevap vermiş mi?
                if (answers.ContainsKey(question.Id))
                {
                    string verilenCevap = answers[question.Id];

                    if (verilenCevap == question.CorrectAnswer)
                    {
                        dogruSayisi++;
                    }
                    else
                    {
                        yanlisSayisi++;
                    }
                }
                else
                {
                    yanlisSayisi++;
                }
            }

            // 4. Puan Hesapla
            int toplamSoru = exam.Questions.Count;
            int puan = 0;
            if (toplamSoru > 0)
            {
                puan = (int)((double)dogruSayisi / toplamSoru * 100);
            }

            // 5. Sonucu Veritabanına Kaydet
            var result = new ExamResult
            {
                AppUserId = user.Id,
                ExamId = examId,
                CorrectCount = dogruSayisi,
                WrongCount = yanlisSayisi,
                Score = puan,
                Date = DateTime.Now
            };

            _context.ExamResults.Add(result);
            await _context.SaveChangesAsync();

            // 6. Sonuç Ekranına Gönder
            return View("Result", result);
        }
    }
}