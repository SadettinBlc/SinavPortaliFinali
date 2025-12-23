using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SinavPortaliFinal.Models
{
    public class Exam
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Sınav Başlığı")]
        public string Title { get; set; } = "";

        [Display(Name = "Sınav Tarihi")]
        public DateTime StartDate { get; set; } // Başlangıç (Örn: 25.12.2023 10:00)
        public DateTime EndDate { get; set; }

        [Display(Name = "Süre (Dakika)")]
        public int Duration { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // İlişki: Hangi Ders?
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}