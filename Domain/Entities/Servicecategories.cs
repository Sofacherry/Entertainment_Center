using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    [Table("servicecategories")]
    public class Servicecategories
    {
        [Key, Column("serviceid")] 
        [ForeignKey("services")]
        public int serviceid { get; set; }

        [Key, Column("categoryid")] 
        [ForeignKey("categories")]
        public int categoryid { get; set; }

        public virtual Services service { get; set; }
        public virtual Categories category { get; set; }
    }
}
