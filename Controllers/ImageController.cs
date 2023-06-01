using Microsoft.AspNetCore.Mvc;
using WebSite.Models.Contexts;
using WebSite.Utils.Extensions;
using WebSite.ViewModels.Image;
using X.PagedList;

namespace WebSite.Controllers;

public class ImageController : Controller
{
    private readonly SqlServerDatabaseContext context;
    private readonly ILogger<ImageController> logger;

    public ImageController(SqlServerDatabaseContext context, ILoggerFactory loggerFactory)
    {
        this.context = context;
	    logger = loggerFactory.CreateLogger<ImageController>();
    }

    [HttpGet]
    public async Task<IActionResult> Home(int page = 1, int pageSize = 21)
    {
        if (context.Images is null)
        {
            logger.LogWarning("[GET] Home: 'Images' db set is null, returning 404 Not Found");
            return NotFound();
        }

        var images = context.Images.ToList();
        var currentUserId = User.GetCurrentUserId();

        var communityImages = images.Where(image => string.IsNullOrWhiteSpace(image.UserId) || !image.UserId.Equals(currentUserId));
        var userImages = images.Where(image => !string.IsNullOrWhiteSpace(image.UserId) && image.UserId.Equals(currentUserId));
        var viewModel = new HomeViewModel { UserImages = userImages, CommunityImages = await communityImages.ToPagedListAsync(page, pageSize) };
        
        logger.LogInformation("[GET] Home: returning view");
        return View(viewModel);
    }

    public async Task<IActionResult> UploadImages(IFormFileCollection images, [FromServices] IHttpClientFactory httpClientFactory)
    {
        var currentUserId = User.GetCurrentUserId();

        foreach (var image in images)
        {
            using var httpClient = httpClientFactory.CreateClient("AzureFunctionClient");
            await using var imageStream = image.OpenReadStream();
            using var content = new StreamContent(imageStream);
            
            logger.LogInformation("[POST] UploadImages: sending uploaded image to Azure Function check");
	        var response = await httpClient.PostAsync($"/api/check/{currentUserId}", content);
            
            imageStream.Close();
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("[POST] UploadImages: azure function check doesnt return success status code, returning 400 Bad Request");
                return BadRequest(response);
            }
            
        }
        
        logger.LogInformation("[POST] UploadImages: successfully uploaded all images, redirecting to {ActionName}", nameof(Home));
        return RedirectToAction(actionName: nameof(Home));
    }

}