#nullable disable
using X.PagedList;

namespace WebSite.ViewModels.Image;

public class HomeViewModel
{
    public IEnumerable<Models.Entities.Image> UserImages { get; set; }
    public IPagedList<Models.Entities.Image> CommunityImages { get; set; }
}