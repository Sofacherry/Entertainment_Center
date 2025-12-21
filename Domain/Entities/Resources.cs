using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    [Table("resources")]
    public class Resources
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("resourceid")]
        public int resourceid { get; set; }

        [Required]
        [ForeignKey("services")]
        [Column("serviceid")]
        public int serviceid { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(100)]
        public string name { get; set; }

        [Required]
        [Column("capacity")]
        public int capacity { get; set; }

        // Навигационные свойства
        public virtual Services service { get; set; }
        public virtual ICollection<Orderresources> orderresources { get; set; }
    }
}
