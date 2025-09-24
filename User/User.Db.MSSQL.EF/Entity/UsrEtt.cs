using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace User.Db.MSSQL.EF.Entity
{
    
    [Table("Users")]
    public class UsrEtt
    {
        [Key]
        [MaxLength(64)]
        public required string Id { get; set; }

        [Required]
        [MaxLength(200)]
        public required string FullName { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public DateTime? BirthDay { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public bool Active { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}