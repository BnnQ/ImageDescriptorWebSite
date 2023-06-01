using AutoMapper;
using WebSite.Models.Entities;
using WebSite.ViewModels.Account;

namespace WebSite.Services.MapperProfiles;

public class UserRegistrationProfile : Profile
{
    public UserRegistrationProfile()
    {
        CreateMap<RegistrationViewModel, User>().ReverseMap();
    }
}