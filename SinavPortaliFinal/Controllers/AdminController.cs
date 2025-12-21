using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SinavPortaliFinal.Models;
using SinavPortaliFinal.Repositories;
using System.IO; // Dosya işlemleri için

namespace SinavPortaliFinal.Controllers
{
    // Genel Giriş İzni: Hem Müdür hem Öğretmen girebilir
    [Authorize(Roles = "Müdür,Öğretmen")]
    public class AdminController : Controller
    {
        private readonly IGenericRepository<Category> _categoryRepo;
        private readonly IGenericRepository<Exam> _examRepo;
        private readonly IGenericRepository<Question> _questionRepo;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context; // Filtreleme işlemleri için

        public AdminController(IGenericRepository<Category> categoryRepo,
                               IGenericRepository<Exam> examRepo,
                               IGenericRepository<Question> questionRepo,
                               UserManager<AppUser> userManager,
                               AppDbContext context)
        {
            _categoryRepo = categoryRepo;
            _examRepo = examRepo;
            _questionRepo = questionRepo;
            _userManager = userManager;
            _context = context;
        }

        // ==========================================
        //             DASHBOARD (ANA SAYFA)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            ViewBag.CategoryCount = _categoryRepo.GetAll().Count;
            ViewBag.ExamCount = _examRepo.GetAll().Count;
            ViewBag.QuestionCount = _questionRepo.GetAll().Count;

            var students = await _userManager.GetUsersInRoleAsync("Öğrenci");
            ViewBag.StudentCount = students.Count;

            var allUsers = _userManager.Users.Count();
            ViewBag.StaffCount = allUsers - students.Count;

            return View();
        }

        // ==========================================
        //      1. DERS (KATEGORİ) İŞLEMLERİ
        //      (SADECE MÜDÜR YÖNETEBİLİR)
        // ==========================================
        [Authorize(Roles = "Müdür")]
        public IActionResult Categories() => View(_categoryRepo.GetAll());

        [Authorize(Roles = "Müdür")]
        [HttpGet] public IActionResult CreateCategory() => View();

        [Authorize(Roles = "Müdür")]
        [HttpPost]
        public IActionResult CreateCategory(Category p)
        {
            _categoryRepo.Add(p);
            TempData["BasariliMesaj"] = "Ders başarıyla eklendi.";
            return RedirectToAction("Categories");
        }

        [Authorize(Roles = "Müdür")]
        [HttpGet] public IActionResult UpdateCategory(int id) => View(_categoryRepo.GetById(id));

        [Authorize(Roles = "Müdür")]
        [HttpPost]
        public IActionResult UpdateCategory(Category p)
        {
            _categoryRepo.Update(p);
            TempData["BasariliMesaj"] = "Ders güncellendi.";
            return RedirectToAction("Categories");
        }

        [Authorize(Roles = "Müdür")]
        [HttpPost]
        public IActionResult DeleteCategoryAjax(int id)
        {
            _categoryRepo.Delete(id);
            return Json(new { success = true });
        }

        // ==========================================
        //             2. SINAV İŞLEMLERİ
        //      (Öğretmen sadece kendi sınavını görür)
        // ==========================================
        public IActionResult Exams()
        {
            // Tüm sınavları ders bilgisiyle çek
            var allExams = _context.Exams.Include(x => x.Category).ToList();

            // MÜDÜR ise hepsini görsün
            if (User.IsInRole("Müdür"))
            {
                return View(allExams);
            }

            // ÖĞRETMEN ise sadece kendi derslerinin sınavlarını görsün
            if (User.IsInRole("Öğretmen"))
            {
                var teacherId = int.Parse(_userManager.GetUserId(User) ?? "0");

                // Öğretmenin atandığı ders ID'leri
                var teacherCategoryIds = _context.UserCategories
                                                 .Where(x => x.AppUserId == teacherId)
                                                 .Select(x => x.CategoryId)
                                                 .ToList();

                // Sadece bu derslere ait sınavları filtrele
                var myExams = allExams.Where(x => teacherCategoryIds.Contains(x.CategoryId)).ToList();
                return View(myExams);
            }

            return View(new List<Exam>());
        }

        [HttpGet]
        public IActionResult CreateExam()
        {
            // Dropdown Listesi Doldurma (Yetkiye Göre)
            if (User.IsInRole("Müdür"))
            {
                // Müdür tüm dersleri görebilir
                ViewBag.Dersler = _categoryRepo.GetAll();
            }
            else
            {
                // Öğretmen sadece atandığı dersleri görebilir
                var teacherId = int.Parse(_userManager.GetUserId(User) ?? "0");
                var myCategoryIds = _context.UserCategories
                                            .Where(x => x.AppUserId == teacherId)
                                            .Select(x => x.CategoryId)
                                            .ToList();

                ViewBag.Dersler = _context.Categories.Where(x => myCategoryIds.Contains(x.Id)).ToList();
            }

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
            // Eğer ID gelmediyse (link bozuksa vs) Sınavlar sayfasına geri at
            if (examId == null)
            {
                return RedirectToAction("Exams");
            }

            // Başlıkta göstermek için Sınavın Adını buluyoruz
            var exam = _examRepo.GetById(examId.Value);

            if (exam != null)
            {
                ViewBag.ExamName = exam.Title; // Senin tabloda Name ise burayı exam.Name yap
            }

            ViewBag.SelectedExamId = examId;

            // SADECE bu sınava ait soruları filtreleyip gönderiyoruz
            var questions = _questionRepo.GetListByFilter(x => x.ExamId == examId, "Exam");

            return View(questions);
        }

        [HttpGet]
        public IActionResult CreateQuestion(int? id)
        {
            // Eğer sınav ID'si gelmemişse Sınavlar sayfasına geri gönder
            if (id == null)
            {
                TempData["Hata"] = "Lütfen önce sınavlar sayfasından bir sınav seçiniz.";
                return RedirectToAction("Exams");
            }

            ViewBag.SelectedExamId = id;

            // Sınavın adını bulup ekrana yazalım
            var exam = _examRepo.GetById(id.Value);
            if (exam != null) ViewBag.ExamName = exam.Title;

            return View();
        }

        [HttpPost]
        public IActionResult CreateQuestion(Question p)
        {
            p.Id = 0;
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
        //      4. PERSONEL YÖNETİMİ (Sadece Müdür)
        // ==========================================
        [Authorize(Roles = "Müdür")]
        public IActionResult Users()
        {
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
                TempData["BasariliMesaj"] = "Personel başarıyla eklendi.";
                return RedirectToAction("Users");
            }
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(p);
        }

        [Authorize(Roles = "Müdür")]
        [HttpGet] public async Task<IActionResult> UpdateUser(string id) => View(await _userManager.FindByIdAsync(id));

        [Authorize(Roles = "Müdür")]
        [HttpPost]
        public async Task<IActionResult> UpdateUser(AppUser p, string? Password, string Role)
        {
            var user = await _userManager.FindByIdAsync(p.Id.ToString());
            if (user == null) return NotFound();

            user.Name = p.Name; user.Surname = p.Surname; user.UserName = p.UserName;

            // --- ŞİFRE DEĞİŞTİRME (Token Hatası Çözüldü) ---
            if (!string.IsNullOrEmpty(Password))
            {
                if (await _userManager.HasPasswordAsync(user))
                {
                    await _userManager.RemovePasswordAsync(user);
                }
                await _userManager.AddPasswordAsync(user, Password);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, Role);

            await _userManager.UpdateAsync(user);
            TempData["BasariliMesaj"] = "Personel güncellendi.";
            return RedirectToAction("Users");
        }

        [Authorize(Roles = "Müdür")]
        [HttpPost]
        public async Task<IActionResult> DeleteUserAjax(string id)
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
        //           5. ÖĞRENCİ YÖNETİMİ
        //    (Öğretmen Sadece Kendi Öğrencisini Görür)
        // ==========================================
        public async Task<IActionResult> Students()
        {
            // 1. Tüm öğrencileri getir
            var allStudents = await _userManager.GetUsersInRoleAsync("Öğrenci");

            // 2. MÜDÜR ise hepsini göster
            if (User.IsInRole("Müdür"))
            {
                return View(allStudents);
            }

            // 3. ÖĞRETMEN ise filtrele
            if (User.IsInRole("Öğretmen"))
            {
                var teacherId = int.Parse(_userManager.GetUserId(User) ?? "0");

                // Öğretmenin ders ID'leri
                var teacherCategoryIds = _context.UserCategories
                                                 .Where(x => x.AppUserId == teacherId)
                                                 .Select(x => x.CategoryId)
                                                 .ToList();

                // Bu dersleri alan öğrencilerin ID'leri
                var studentIdsInMyLessons = _context.UserCategories
                                                    .Where(x => teacherCategoryIds.Contains(x.CategoryId))
                                                    .Select(x => x.AppUserId)
                                                    .Distinct()
                                                    .ToList();

                // Listeyi filtrele
                var myStudents = allStudents.Where(s => studentIdsInMyLessons.Contains(s.Id)).ToList();
                return View(myStudents);
            }

            return View(allStudents);
        }

        [HttpGet] public IActionResult CreateStudent() => View();

        [HttpPost]
        public async Task<IActionResult> CreateStudent(AppUser p, string Password)
        {
            var result = await _userManager.CreateAsync(p, Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(p, "Öğrenci");
                TempData["BasariliMesaj"] = "Öğrenci eklendi.";
                return RedirectToAction("Students");
            }
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(p);
        }

        [HttpGet] public async Task<IActionResult> UpdateStudent(string id) => View(await _userManager.FindByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> UpdateStudent(AppUser p, string? Password)
        {
            var user = await _userManager.FindByIdAsync(p.Id.ToString());
            if (user == null) return NotFound();

            user.Name = p.Name; user.Surname = p.Surname; user.UserName = p.UserName;

            // --- ŞİFRE DEĞİŞTİRME (Token Hatası Çözüldü) ---
            if (!string.IsNullOrEmpty(Password))
            {
                if (await _userManager.HasPasswordAsync(user))
                {
                    await _userManager.RemovePasswordAsync(user);
                }
                await _userManager.AddPasswordAsync(user, Password);
            }

            await _userManager.UpdateAsync(user);
            TempData["BasariliMesaj"] = "Öğrenci güncellendi.";
            return RedirectToAction("Students");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStudentAjax(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null) { await _userManager.DeleteAsync(user); return Json(new { success = true }); }
            return Json(new { success = false });
        }

        // ==========================================
        //             6. PROFİL İŞLEMLERİ
        // ==========================================
        [HttpGet] public async Task<IActionResult> MyProfile() => View(await _userManager.GetUserAsync(User));

        [HttpPost]
        public async Task<IActionResult> MyProfile(AppUser p, IFormFile? file, string? NewPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // --- RESİM YÜKLEME (Dizin hatası çözüldü) ---
            if (file != null)
            {
                var ext = Path.GetExtension(file.FileName);
                var name = Guid.NewGuid() + ext;

                // Güvenli klasör yolu birleştirme
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                var path = Path.Combine(folder, name);
                using (var stream = new FileStream(path, FileMode.Create)) await file.CopyToAsync(stream);
                user.ProfileImageUrl = "/img/" + name;
            }

            user.Name = p.Name; user.Surname = p.Surname; user.UserName = p.UserName;

            // --- ŞİFRE DEĞİŞTİRME (Token Hatası Çözüldü) ---
            if (!string.IsNullOrEmpty(NewPassword))
            {
                if (await _userManager.HasPasswordAsync(user))
                {
                    await _userManager.RemovePasswordAsync(user);
                }
                await _userManager.AddPasswordAsync(user, NewPassword);
            }

            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            TempData["BasariliMesaj"] = "Profil güncellendi. Lütfen tekrar giriş yapın.";
            return RedirectToAction("Index", "Login");
        }

        // ==========================================
        //      DERS ATAMA EKRANI (SADECE MÜDÜR)
        // ==========================================
        [Authorize(Roles = "Müdür")]
        [HttpGet]
        public async Task<IActionResult> AssignLesson(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            var allCategories = _categoryRepo.GetAll();
            var userCategories = _context.UserCategories
                                         .Where(x => x.AppUserId == id)
                                         .Select(x => x.CategoryId)
                                         .ToList();

            var model = new List<AssignCategoryViewModel>();

            foreach (var item in allCategories)
            {
                model.Add(new AssignCategoryViewModel
                {
                    CategoryId = item.Id,
                    CategoryName = item.Name, // Modelindeki isim neyse o
                    Exists = userCategories.Contains(item.Id)
                });
            }

            ViewBag.UserId = id;
            ViewBag.UserName = user.Name + " " + user.Surname;

            return View(model);
        }

        [Authorize(Roles = "Müdür")]
        [HttpPost]
        public async Task<IActionResult> AssignLesson(int id, List<AssignCategoryViewModel> model)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            foreach (var item in model)
            {
                if (item.Exists)
                {
                    var isExist = _context.UserCategories.Any(x => x.AppUserId == id && x.CategoryId == item.CategoryId);
                    if (!isExist)
                    {
                        _context.UserCategories.Add(new UserCategory
                        {
                            AppUserId = id,
                            CategoryId = item.CategoryId
                        });
                    }
                }
                else
                {
                    var categoryToRemove = _context.UserCategories.FirstOrDefault(x => x.AppUserId == id && x.CategoryId == item.CategoryId);
                    if (categoryToRemove != null)
                    {
                        _context.UserCategories.Remove(categoryToRemove);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}