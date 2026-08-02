using System.Linq;
using Microsoft.EntityFrameworkCore;
using PersonalTaskManagement.Models;

namespace PersonalTaskManagement.Data
{
    public static class DatabaseInitializer
    {
        public static void Initialize()
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            SeedIfEmpty(db);
        }

        private static void SeedIfEmpty(AppDbContext db)
        {
            if (db.Boards.Any()) return;

            var bug = new Tag { Name = "Bug", ColorHex = "#E53935" };
            var feature = new Tag { Name = "Feature", ColorHex = "#1E88E5" };
            var chore = new Tag { Name = "Chore", ColorHex = "#8E24AA" };
            db.Tags.AddRange(bug, feature, chore);

            var work = new Board
            {
                Name = "Work",
                SortOrder = 0,
                Columns =
                {
                    new BoardColumn { Name = "To Do", SortOrder = 0,
                        Tasks =
                        {
                            new TaskItem
                            {
                                Title = "Welcome to AgileFlow",
                                DescriptionPlain = "Drag this card between columns. Right-click for more.",
                                Priority = Priority.Medium,
                                SortOrder = 0,
                                Tags = { feature }
                            },
                            new TaskItem
                            {
                                Title = "Fix the login spinner",
                                DescriptionPlain = "Spinner stays after auth completes on slow networks.",
                                Priority = Priority.High,
                                DueDate = System.DateTime.Today.AddDays(-1),
                                SortOrder = 1,
                                Tags = { bug }
                            }
                        }
                    },
                    new BoardColumn { Name = "In Progress", SortOrder = 1,
                        Tasks =
                        {
                            new TaskItem
                            {
                                Title = "Refactor settings module",
                                DescriptionPlain = "Split into smaller view models.",
                                Priority = Priority.Low,
                                DueDate = System.DateTime.Today.AddDays(7),
                                SortOrder = 0,
                                Tags = { chore }
                            }
                        }
                    },
                    new BoardColumn { Name = "Done", SortOrder = 2 }
                }
            };

            var personal = new Board
            {
                Name = "Personal",
                SortOrder = 1,
                Columns =
                {
                    new BoardColumn { Name = "To Do", SortOrder = 0 },
                    new BoardColumn { Name = "In Progress", SortOrder = 1 },
                    new BoardColumn { Name = "Done", SortOrder = 2 }
                }
            };

            db.Boards.AddRange(work, personal);
            db.SaveChanges();
        }
    }
}
