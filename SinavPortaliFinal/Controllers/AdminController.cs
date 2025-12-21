using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SinavPortaliFinal.Models;
using SinavPortaliFinal.Repositories;

namespace SinavPortaliFinal.Controllers
{
    [Authorize(Roles = "Müdür,Öğretmen")]
    public class AdminController : Controller
    {
        private readonly IGenericRepository<Category> _categoryRepo;
        private readonly IGenericRepository<Exam> _examRepo;
        private readonly IGenericRepository<Question> _questionRepo;
        private readonly UserManager<AppUser> _userManager;

        public AdminController(IGenericRepository<Category> categoryRepo,
                               IGenericRepository<Exam> examRepo,
                               IGenericRepository<Question> questionRepo,
                               UserManager<AppUser> userManager)
        {
            _categoryRepo = categoryRepo;
            _examRepo = examRepo;
            _questionRepo = questionRepo;
            _userManager = userManager;
        }

        // --- DASHBOARD ---
        public IActionResult Index()
        {
            ViewBag.CategoryCount = _categoryRepo.GetAll().Count;
            ViewBag.ExamCount = _examRepo.GetAll().Count;
            ViewBag.QuestionCount = _questionRepo.GetAll().Count;
            ViewBag.UserCount = _userManager.Users.Count();
            return View();
        }

        // ==========================================
        //           1. DERS İŞLEMLERİ
        // ==========================================
        public IActionResult Categories()
        {
            return View(_categoryRepo.GetAll());
        }

        [HttpGet] public IActionResult CreateCategory() => View();

        [HttpPost]
        public IActionResult CreateCategory(Category p)
        {
            _categoryRepo.Add(p);
            TempData["BasariliMesaj"] = "Ders eklendi.";
            return RedirectToAction("Categories");
        }

        [HttpGet] public IActionResult UpdateCategory(int id) => View(_categoryRepo.GetById(id));

        [HttpPost]
        public IActionResult UpdateCategory(Category p)
        {
            _categoryRepo.Update(p);
            TempData["BasariliMesaj"] = "Ders güncellendi.";
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public IActionResult DeleteCategoryAjax(int id)
        {
            _categoryRepo.Delete(id);
            return Json(new { success = true });
        }

        // ==========================================
        //             2. SINAV İŞLEMLERİ
        // ==========================================
        public IActionResult Exams()
        {
            return View(_examRepo.GetAll("Category"));
        }

        [HttpGet]
        public IActionResult CreateExam()
        {
            ViewBag.Dersler = _categoryRepo.GetAll();
            return View();
        }

        [HttpPost]
        public IActionResult CreateExam(Exam p)
        {
            p.CreatedDate = DateTime.Now;
            _examRepo.Add(p);
            TempData["BasariliMesaj"] = "Sınav oluşturuldu.";
            return RedirectToAction("Exams");
        }

        [HttpGet]
        public IActionResult UpdateExam(int id)
        {
            ViewBag.Dersler = _categoryRepo.GetAll();
            return View(_examRepo.GetById(id));
        }

        [HttpPost]
        public IActionResult UpdateExam(Exam p)
        {
            _examRepo.Update(p);
            TempData["BasariliMesaj"] = "Sınav güncellendi.";
            return RedirectToAction("Exams");
        }

        [HttpPost]
        public IActionResult DeleteExamAjax(int id)
        {
            _examRepo.Delete(id);
            return Json(new { success = true });
        }

        // ==========================================
        //             3. SORU İŞLEMLERİ
        // ==========================================
        public IActionResult Questions(int? examId)
        {
            ViewBag.Exams = _examRepo.GetAll();
            if (examId.HasValue)
            {
                ViewBag.SelectedExamId = examId;
                return View(_questionRepo.GetListByFilter(x => x.ExamId == examId, "Exam"));
            }
            return View(new List<Question>());
        }

        [HttpGet]
        public IActionResult CreateQuestion(int? id)
        {
            if (id.HasValue) ViewBag.SelectedExamId = id;
            ViewBag.Sinavlar = _examRepo.GetAll();
            return View();
        }

        [HttpPost]
        public IActionResult CreateQuestion(Question p)
        {
            p.Id = 0; // Identity hatası önleyici
            _questionRepo.Add(p);
            TempData["BasariliMesaj"] = "Soru eklendi.";
            if (p.ExamId > 0) return RedirectToAction("Questions", new { examId = p.ExamId });
            return RedirectToAction("Questions");
        }

        [HttpGet]
        public IActionResult UpdateQuestion(int id)
        {
            ViewBag.Sinavlar = _examRepo.GetAll();
            return View(_questionRepo.GetById(id));
        }

        [HttpPost]
        public IActionResult UpdateQuestion(Question p)
        {
            _questionRepo.Update(p);
            TempData["BasariliMesaj"] = "Soru güncellendi.";
            return RedirectToAction("Questions", new { examId = p.ExamId });
        }

        [HttpPost]
        public IActionResult DeleteQuestionAjax(int id)
        {
            _questionRepo.Delete(id);
            return Json(new { success = true });
        }

        // ==========================================
        //      4. KULLANICI YÖNETİMİ (Müdür Only)
        // ==========================================
        [Authorize(Roles = "Müdür")]
        public IActionResult Users()
        {
            // Sadece Müdür ve Öğretmenleri listele (Öğrenciler ayrı panelde)
            var users = _userManager.Users.ToList().Where(x =>
                _userManager.IsInRoleAsync(x, "Müdür").Result ||
                _userManager.IsInRoleAsync(x, "Öğretmen").Result).ToList();
            return View(users);
        }

        [Authorize(Roles = "Müdür")]
        [HttpGet] public IActionResult CreateUser() => View();

        [Authorize(Roles = "Müdür")]
        [HttpPost]
        public async Task<IActionResult> CreateUser(AppUser p, string Password, string Role)
        {
            var result = await _userManager.CreateAsync(p, Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(p, Role);
                TempData["BasariliMesaj"] = "Kullanıcı oluşturuldu.";
                return RedirectToAction("Users");
            }
            return View();
        }

        [Authorize(Roles = "Müdür")]
        [HttpPost]
        public async Task<IActionResult> DeleteUserAjax(string id) // Identity ID string olabilir ama biz int yaptık, yine de string alıp parse edelim garanti olsun
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // ==========================================
        //           5. PROFİL (MyProfile)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> MyProfile(AppUser p, IFormFile? file, string? NewPassword)
        {
            var user = await _userManager.GetUserAsync(User);

            if (file != null)
            {
                var ext = Path.GetExtension(file.FileName);
                var name = Guid.NewGuid() + ext;
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", name);
                using (var stream = new FileStream(path, FileMode.Create)) await file.CopyToAsync(stream);
                user.ProfileImageUrl = "/img/" + name;
            }

            user.Name = p.Name;
            user.Surname = p.Surname;
            user.UserName = p.UserName;

            if (!string.IsNullOrEmpty(NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, NewPassword);
            }

            await _userManager.UpdateAsync(user);
            TempData["BasariliMesaj"] = "Profil güncellendi. Lütfen tekrar giriş yapın.";
            await _userManager.UpdateSecurityStampAsync(user); // Güvenlik damgasını yenile
            return RedirectToAction("Index", "Login");
        }
    }
}