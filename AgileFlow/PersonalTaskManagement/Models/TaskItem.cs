using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PersonalTaskManagement.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string DescriptionXaml { get; set; } = string.Empty;

        public string DescriptionPlain { get; set; } = string.Empty;

        public Priority Priority { get; set; } = Priority.Medium;

        public DateTime? DueDate { get; set; }

        public int SortOrder { get; set; }

        public int BoardColumnId { get; set; }
        public BoardColumn? BoardColumn { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public List<Tag> Tags { get; set; } = new();
    }
}
