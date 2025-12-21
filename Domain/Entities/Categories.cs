using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    [Table("categories")]  
    public class Categories
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("categoryid")]
        public int categoryid { get; set; }

        [Required]
        [Column("categoryname")]
        [MaxLength(50)]
        public string categoryname { get; set; }

        public virtual ICollection<Servicecategories> servicecategories { get; set; }
    }
}
