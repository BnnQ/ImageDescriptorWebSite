using System.Security.Claims;
using Google.Apis.Auth.OAuth2;
using Google.Apis.PeopleService.v1;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace WebSite.Authentication;

public class GoogleOAuthEvents : OAuthEvents
{
    public override async Task CreatingTicket(OAuthCreatingTicketContext context)
    {
        await base.CreatingTicket(context);

        var accessToken = context.AccessToken;
        var credential = GoogleCredential.FromAccessToken(accessToken);
        var service = new PeopleServiceService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "SeeSay"
        });

        var request = service.People.Get("people/me");
        request.PersonFields = "names";

        var person = await request.ExecuteAsync();
        var personNames = person.Names.FirstOrDefault();
        if (personNames is not null)
        {
            var identity = (context.Principal!.Identity as ClaimsIdentity)!;
            if (!string.IsNullOrWhiteSpace(personNames.GivenName))
            {
                identity.AddClaim(new Claim(ClaimTypes.GivenName, personNames.GivenName));
            }

            if (!string.IsNullOrWhiteSpace(personNames.FamilyName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Surname, personNames.FamilyName));
            }
        }
    }
}