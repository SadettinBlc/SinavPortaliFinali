using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SinavPortaliFinal.Models
{
    // Bu tablo, hangi kullanıcının hangi derse atandığını tutacak
    public class UserCategory
    {
        [Key]
        public int Id { get; set; }

        // Kullanıcı (Öğrenci veya Öğretmen)
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        // Ders (Kategori)
        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}