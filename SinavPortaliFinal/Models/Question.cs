using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SinavPortaliFinal.Models
{
    public class Question
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ID Otomatik artar
        public int Id { get; set; }

        [Required] public string QuestionText { get; set; } = "";
        [Required] public string OptionA { get; set; } = "";
        [Required] public string OptionB { get; set; } = "";
        [Required] public string OptionC { get; set; } = "";
        [Required] public string OptionD { get; set; } = "";
        [Required] public string CorrectAnswer { get; set; } = ""; // "A", "B" gibi

        // İlişki: Hangi Sınav?
        public int ExamId { get; set; }
        [ForeignKey("ExamId")]
        public virtual Exam? Exam { get; set; }
    }
}