using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SinavPortaliFinal.Models
{
    // ID'si sayı (int) olan gelişmiş kullanıcı
    public class AppUser : IdentityUser<int>
    {
        [Display(Name = "Ad")]
        public string Name { get; set; } = "";

        [Display(Name = "Soyad")]
        public string Surname { get; set; } = "";

        [Display(Name = "Profil Resmi")]
        public string? ProfileImageUrl { get; set; }
        
        public List<UserCategory> UserCategories { get; set; }
    }

}