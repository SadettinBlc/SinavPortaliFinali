using System.ComponentModel.DataAnnotations;

namespace SinavPortaliFinal.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ders adı zorunludur.")]
        [Display(Name = "Ders Adı")]
        public string Name { get; set; } = "";

        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        // Bir dersin çok sınavı olabilir
        public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}