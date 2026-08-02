using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PersonalTaskManagement.Models
{
    public class BoardColumn
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public int BoardId { get; set; }
        public Board? Board { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public List<TaskItem> Tasks { get; set; } = new();
    }
}
