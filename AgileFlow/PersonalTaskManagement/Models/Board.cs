using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PersonalTaskManagement.Models
{
    public class Board
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public List<BoardColumn> Columns { get; set; } = new();
    }
}
