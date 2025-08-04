using System.ComponentModel.DataAnnotations;

namespace Tumbleweed.Data.Models;

public class Episode
{
    [Key]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public string Id { get; set; }

    public string FeedId { get; set; }

    public string Title { get; set; }
 
    public string Link { get; set; }

    public DateOnly PublishedDate { get; set; }

    public string? Description { get; set; }

    public string Content { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public bool IsRead { get; set; }

    public bool IsStarred { get; set; }
}