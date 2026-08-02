using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PersonalTaskManagement.Models
{
    public class Tag
    {
        public int Id { get; set; }

        [Required, MaxLength(60)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string ColorHex { get; set; } = "#607D8B";

        public List<TaskItem> Tasks { get; set; } = new();
    }
}
