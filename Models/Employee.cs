using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Webstore.Models
{
    [Table("Employees")]
    public class Employee
    {
        [Key]
        [Column("employee_id")]
        public int EmployeeId { get; set; }

        [Column("account_id")]
        public int? AccountId { get; set; }

        [Required]
        [StringLength(10)]
        [Column("employee_code")]
        public string EmployeeCode { get; set; } = string.Empty;

        [StringLength(50)]
        [Column("position")]
        public string? Position { get; set; }

        [StringLength(50)]
        [Column("department")]
        public string? Department { get; set; }

        // Navigation properties
        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }
    }
}
