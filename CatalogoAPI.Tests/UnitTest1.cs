using CatalogoApi.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CatalogoAPI.Tests
{
    public class ApiCatalogoIntegrationTests
    {
        [Test]
        public async Task GET_Retorna_Todas_Categorias()
        {
            await using var application = new CatalogoApiApplication();

            await CatalogoMockData.CreateCategories(application, true);
            var url = "/categorias";

            var client = application.CreateClient();

            var result = await client.GetAsync(url);
            var categorias = await client.GetFromJsonAsync<List<Categoria>>("/categorias");

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            Assert.IsNotNull(categorias);
            Assert.IsTrue(categorias.Count == 2);
        }

        [Test]
        public async Task GET_Retorna_Categorias_Vazia()
        {
            await using var application = new CatalogoApiApplication();

            await CatalogoMockData.CreateCategories(application, false);

            var client = application.CreateClient();
            var categorias = await client.GetFromJsonAsync<List<Categoria>>("/categorias");

            Assert.IsNotNull(categorias);
            Assert.IsTrue(categorias.Count == 0);
        }

        [Test]
        public async Task POST_Login_Com_Falha()
        {
            await using var application = new CatalogoApiApplication();

            var user = new UserModel { UserName = "macoratt", Password = "numsey#123" };

            var client = application.CreateClient();
            var result = await client.PostAsJsonAsync("/login", user);

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task POST_Login_Com_Sucesso()
        {
            await using var application = new CatalogoApiApplication();

            var user = new UserModel { UserName = "macoratti", Password = "numsey#123" };

            var client = application.CreateClient();
            var result = await client.PostAsJsonAsync("/login", user);

            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        }

        [Test]
        public async Task GET_Acesso_Nao_Autorizado()
        {
            await using var application = new CatalogoApiApplication();

            await CatalogoMockData.CreateCategories(application, true);

            var client = application.CreateClient();
            var response = await client.GetAsync("/produtos");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Test]
        public async Task POST_Cria_Nova_Categoria()
        {
            await using var application = new CatalogoApiApplication();

            await CatalogoMockData.CreateCategories(application, true);

            var categoria = new Categoria { Nome = "Categoria 3", 
                Descricao = "Descricao Categoria 3" };

            var client = application.CreateClient();

            var response = await client.PostAsJsonAsync("/categorias", categoria);

            var categorias = await client.GetFromJsonAsync<List<Categoria>>("/categorias");

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            Assert.IsTrue(categorias.Count == 3);
        }

        [Test]
        public async Task DELETE_Excluir_Categoria_Existente()
        {
            await using var application = new CatalogoApiApplication();

            await CatalogoMockData.CreateCategories(application, true);

            var client = application.CreateClient();
            var url = "/categorias/1";
            var response = await client.DeleteAsync(url);

            var categorias = await client.GetFromJsonAsync<List<Categoria>>("/categorias");

            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
            Assert.IsNotNull(categorias);
            Assert.IsTrue(categorias.Count == 1);
        }

        [Test]
        public async Task DELETE_Excluir_Categoria_Inexistente()
        {
            await using var application = new CatalogoApiApplication();

            await CatalogoMockData.CreateCategories(application, true);

            var client = application.CreateClient();
            var url = "/categorias/99";
            var response = await client.DeleteAsync(url);

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Test]
        public async Task PUT_Atualiza_Categoria_Existente()
        {
            await using var application = new CatalogoApiApplication();
            await CatalogoMockData.CreateCategories(application, true);

            var client = application.CreateClient();
            var url = "/categorias/1";

            var categoria = new Categoria
            {
                CategoriaId = 1,
                Nome = "Categoria alterada",
                Descricao = "Descricao Categoria alterada"
            };

            var response = await client.PutAsJsonAsync(url, categoria);

            var categoriaAlterada = await client.GetFromJsonAsync<Categoria>(url);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
        [Test]
        public async Task PUT_Atualiza_Categoria_Inexistente()
        {
            await using var application = new CatalogoApiApplication();
            await CatalogoMockData.CreateCategories(application, true);

            var client = application.CreateClient();
            var url = "/categorias/5";

            var categoria = new Categoria
            {
                CategoriaId = 5,
                Nome = "Categoria alterada",
                Descricao = "Descricao Categoria alterada"
            };

            var response = await client.PutAsJsonAsync(url, categoria);

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

    }
}