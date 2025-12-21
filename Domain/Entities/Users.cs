using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    [Table("users")]
    public partial class Users
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("userid")]
        public int userid { get; set; }

        [ForeignKey("citizencategories")]
        [Column("citizencategoryid")]
        public int citizencategoryid { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(100)]
        public string name { get; set; }

        [Required]
        [Column("email")]
        [MaxLength(100)]
        public string email { get; set; }

        [Required]
        [Column("passwordhash")]
        public string passwordhash { get; set; }

        [Required]
        [Column("role")]
        [MaxLength(50)]
        public string role { get; set; }

        public virtual Citizencategories citizencategory { get; set; }

        public virtual ICollection<Orders> orders { get; set; }
    }
}
