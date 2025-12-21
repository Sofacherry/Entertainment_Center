using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    [Table("services")]
    public partial class Services
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("serviceid")]
        public int serviceid { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(200)]
        public string name { get; set; } 

        [Required]
        [Column("duration")]
        public int duration { get; set; }

        [Required]
        [Column("weekdayprice")]
        public decimal weekdayprice { get; set; }

        [Required]
        [Column("weekendprice")]
        public decimal weekendprice { get; set; }

        [Required]
        [Column("starttime")]
        public TimeSpan starttime { get; set; }

        [Required]
        [Column("endtime")]
        public TimeSpan endtime { get; set; }

        public virtual ICollection<Orders> orders { get; set; }
        public virtual ICollection<Servicecategories> servicecategories { get; set; }
        public virtual ICollection<Resources> resources { get; set; }
    }
}
