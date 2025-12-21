using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    [Table("orders")]
    public class Orders
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("orderid")]
        public int orderid { get; set; }

        [Required]
        [ForeignKey("users")]
        [Column("userid")]
        public int userid { get; set; }

        [Required]
        [ForeignKey("services")]
        [Column("serviceid")]
        public int serviceid { get; set; }

        [Required]
        [Column("orderdate")]
        public DateTime orderdate { get; set; }

        [Required]
        [Column("totalprice")]
        public decimal totalprice { get; set; }

        [Column("peoplecount")]
        public int peoplecount { get; set; }

        [Required]
        [Column("status")]
        [MaxLength(20)]
        public string status { get; set; } = "создан";

        [Required]
        [Column("created_at")]
        public DateTime created_at { get; set; } = DateTime.UtcNow;

        // Навигационные свойства
        public virtual Users user { get; set; }
        public virtual Services service { get; set; }
        public virtual ICollection<Orderresources> orderresources { get; set; }
    }
}
