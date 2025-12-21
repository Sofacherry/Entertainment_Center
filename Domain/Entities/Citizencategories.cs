using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    [Table("citizencategories")]
    public class Citizencategories
    {
        [Key]  // Первичный ключ 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]  // Автоинкремент
        [Column("categoryid")] 
        public int categoryid { get; set; }

        [Required]  // NOT NULL constraint
        [Column("categoryname")]
        [MaxLength(50)]
        public string categoryname { get; set; }

        [Column("discount")] 
        public decimal discount { get; set; }

        public virtual ICollection<Users> users { get; set; }
    }
}
