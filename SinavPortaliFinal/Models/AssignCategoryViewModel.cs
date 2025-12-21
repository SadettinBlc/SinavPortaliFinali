namespace SinavPortaliFinal.Models
{
    public class AssignCategoryViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public bool Exists { get; set; } // Bu ders kullanıcıda var mı? (İşaretli mi gelsin?)
    }
}