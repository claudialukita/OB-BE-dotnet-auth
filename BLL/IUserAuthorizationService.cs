using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityModel.Client;
using DAL.Model;

namespace BLL
{
    public interface IUserAuthorizationService
    {
        Task<TokenResponse> LoginAsync(string userName, string password);
        Task<User> GetUserAsync(string userName);
        Guid GetUserId();
        string GetUserName();
        string GetEmail();
        List<string> GetRole();
    }
}
