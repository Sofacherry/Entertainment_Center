using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    [Table("order_resources")]
    public class Orderresources
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int order_resource_id { get; set; }

        [Required]
        [ForeignKey("orders")]
        [Column("orderid")]
        public int orderid { get; set; }

        [Required]
        [ForeignKey("resources")]
        [Column("resourceid")]
        public int resourceid { get; set; }

        public virtual Orders order { get; set; }
        public virtual Resources resource { get; set; }
    }
}
