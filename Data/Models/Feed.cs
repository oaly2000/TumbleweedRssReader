using System.ComponentModel.DataAnnotations;

namespace Tumbleweed.Data.Models;

public class Feed
{
    [Key]
    required public string Id { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public string Title { get; set; }

    public string Link { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public string? Description { get; set; }

    public string? IconUrl { get; set; }

    public bool UseIFrame { get; set; }
}
