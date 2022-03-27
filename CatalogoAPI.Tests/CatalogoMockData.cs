using CatalogoApi.Context;
using CatalogoApi.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace CatalogoAPI.Tests;

public class CatalogoMockData
{
    public static async Task CreateCategories(CatalogoApiApplication application, bool criar)
    {
        using (var scope = application.Services.CreateScope())
        {
            var provider = scope.ServiceProvider;
            using (var catalogoDbContext = provider.GetRequiredService<AppDbContext>())
            {
                await catalogoDbContext.Database.EnsureCreatedAsync();

                if (criar)
                {
                    await catalogoDbContext.Categorias.AddAsync(new Categoria
                    { Nome = "Categoria 1", Descricao = "Descricao Categoria 1" });
                    
                    await catalogoDbContext.Categorias.AddAsync(new Categoria 
                    { Nome = "Categoria 2", Descricao = "Descricao Categoria 2" });
                    
                    await catalogoDbContext.SaveChangesAsync();
                }
            }
        }
    }
}
