namespace WebSite.Models.Entities;

public class Image
{
    public int Id { get; set; }
    public string Url { get; set; } = null!;
    public string Description { get; set; } = null!;
    
    public string? UserId { get; set; }
    public virtual User? User { get; set; }
}