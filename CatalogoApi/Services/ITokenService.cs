using CatalogoApi.Models;

namespace CatalogoApi.Services;

public interface ITokenService
{
    string GerarToken(string key, string issuer,string audience, UserModel user);
}
