using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SinavPortaliFinal.Models
{
    public class ExamResult
    {
        [Key]
        public int Id { get; set; }

        public int AppUserId { get; set; } // Öğrenci
        [ForeignKey("AppUserId")]
        public virtual AppUser? AppUser { get; set; }

        public int ExamId { get; set; } // Sınav
        [ForeignKey("ExamId")]
        public virtual Exam? Exam { get; set; }

        public int Score { get; set; } // Puan (0-100)
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
    }
}