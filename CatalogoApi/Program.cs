using CatalogoApi.Context;
using CatalogoApi.Models;
using CatalogoApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CatalogoApi", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = @"JWT Authorization header using the Bearer scheme.
                    Enter 'Bearer'[space].Example: \'Bearer 12345abcdef\'",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecurityScheme
                          {
                              Reference = new OpenApiReference
                              {
                                  Type = ReferenceType.SecurityScheme,
                                  Id = "Bearer"
                              }
                          },
                         new string[] {}
                    }
                });
});


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
                  options.UseNpgsql(connectionString));

builder.Services.AddSingleton<ITokenService>(new TokenService());

builder.Services.AddAuthentication
      (JwtBearerDefaults.AuthenticationScheme)
                 .AddJwtBearer(options =>
                 {
                     options.TokenValidationParameters = new TokenValidationParameters
                     {
                         ValidateIssuer = true,
                         ValidateAudience = true,
                         ValidateLifetime = true,
                         ValidateIssuerSigningKey = true,

                         ValidIssuer = builder.Configuration["Jwt:Issuer"],
                         ValidAudience = builder.Configuration["Jwt:Audience"],
                         IssuerSigningKey = new SymmetricSecurityKey
                         (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                     };
                 });

builder.Services.AddAuthorization();


var app = builder.Build();

//endpoint para login
app.MapPost("/login", [AllowAnonymous] (UserModel userModel, ITokenService tokenService) =>
{
    if (userModel == null)
    {
        return Results.BadRequest("Login Inválido");
    }
    if (userModel.UserName == "macoratti" && userModel.Password == "numsey#123")
    {
        var tokenString = tokenService.GerarToken(app.Configuration["Jwt:Key"],
            app.Configuration["Jwt:Issuer"],
            app.Configuration["Jwt:Audience"],
            userModel);
        return Results.Ok(new { token = tokenString });
    }
    else
    {
        return Results.BadRequest("Login Inválido");
    }
}).Produces(StatusCodes.Status400BadRequest)
              .Produces(StatusCodes.Status200OK)
              .WithName("Login")
              .WithTags("Autenticacao");

//definir endpoints categorias

app.MapGet("/categoriaprodutos", async (AppDbContext db) =>
  await db.Categorias.Include(c => c.Produtos).ToListAsync()
 )
 .Produces<List<Categoria>>(StatusCodes.Status200OK)
 .WithTags("Categorias");

app.MapPost("/categorias", async (Categoria categoria, AppDbContext db) =>
 {
     db.Categorias.Add(categoria);
     await db.SaveChangesAsync();

     return Results.Created($"/categorias/{categoria.CategoriaId}", categoria);
 }).WithTags("Categorias");

app.MapGet("/categorias", async (AppDbContext db) =>
   await db.Categorias.ToListAsync())
    .WithTags("Categorias");

app.MapGet("/categorias/{id:int}", async (int id, AppDbContext db) =>
{
    return await db.Categorias.FindAsync(id)
      is Categoria categoria
               ? Results.Ok(categoria) :
                Results.NotFound();
}).WithTags("Categorias");

app.MapPut("/categorias/{id:int}", async (int id, Categoria categoria, AppDbContext db) =>
{
    if (categoria.CategoriaId != id)
    {
        return Results.BadRequest();
    }
    
    var categoriaDB = await db.Categorias.FindAsync(id);

    if (categoriaDB is null) return Results.NotFound();

    categoriaDB.Nome = categoria.Nome;
    categoriaDB.Descricao = categoria.Descricao;

    await db.SaveChangesAsync();
    return Results.Ok(categoriaDB);
}).WithTags("Categorias");

app.MapDelete("/categorias/{id:int}", async (int id, AppDbContext db) =>
{
    var categoria = await db.Categorias.FindAsync(id);

    if (categoria is null)
        return Results.NotFound();

    db.Categorias.Remove(categoria);
    await db.SaveChangesAsync();
    return Results.NoContent();

}).WithTags("Categorias");
//--------------Endpoints para Produtos

app.MapPost("/produtos", async (Produto produto, AppDbContext db)
 =>
{
    db.Produtos.Add(produto);
    await db.SaveChangesAsync();

    return Results.Created($"/produtos/{produto.ProdutoId}", produto);
})
  .Produces<Produto>(StatusCodes.Status201Created)
  .WithName("CriarNovoProduto")
  .WithTags("Produtos");

app.MapGet("/produtos", async (AppDbContext db) =>
     await db.Produtos.ToListAsync())
        .Produces<List<Produto>>(StatusCodes.Status200OK)
        .WithTags("Produtos")
        .RequireAuthorization();

app.MapGet("/produtos/{id:int}", async (int id, AppDbContext db)
    => {
        return await db.Produtos.FindAsync(id)
                     is Produto produto
                     ? Results.Ok(produto)
                     : Results.NotFound("Produto não encontrado");
    })
    .Produces<Produto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithTags("Produtos");

app.MapPut("/produtos", async (int produtoId, string produtoNome,
 AppDbContext db) =>
{
    var produtoDB = db.Produtos.SingleOrDefault(s => s.ProdutoId == produtoId);

    if (produtoDB == null) return Results.NotFound("Produto não encontrado");

    produtoDB.Nome = produtoNome;

    await db.SaveChangesAsync();
    return Results.Ok(produtoDB);

}).Produces<Produto>(StatusCodes.Status200OK)
  .Produces(StatusCodes.Status404NotFound)
  .WithName("AtualizaNomeProduto")
  .WithTags("Produtos");

app.MapPut("/produtos/{id:int}", async (int id, Produto produto, AppDbContext db) =>
{
    if (produto.ProdutoId != id)
    {
        return Results.BadRequest("Ids não conferem");
    }

    var produtoDB = await db.Produtos.FindAsync(id);

    if (produtoDB is null) return Results.NotFound("Produto não encontrado");

    produtoDB.Nome = produto.Nome;
    produtoDB.Descricao = produto.Descricao;
    produtoDB.Preco = produto.Preco;
    produtoDB.DataCompra = produto.DataCompra;
    produtoDB.Estoque = produto.Estoque;
    produtoDB.Imagem = produto.Imagem;
    produtoDB.CategoriaId = produto.CategoriaId;

    await db.SaveChangesAsync();
    return Results.Ok(produtoDB);
})
 .Produces<Produto>(StatusCodes.Status200OK)
 .Produces(StatusCodes.Status400BadRequest)
 .Produces(StatusCodes.Status404NotFound)
 .WithName("AtualizaProduto")
 .WithTags("Produtos");

app.MapDelete("/produtos/{id:int}", async (int id, AppDbContext db) =>
{
    var produtoDB = await db.Produtos.FindAsync(id);
    if (produtoDB is null)
    {
        return Results.NotFound("Produto não encontraro");
    }
    db.Produtos.Remove(produtoDB);
    await db.SaveChangesAsync();
    return Results.Ok(produtoDB);
})
 .Produces<Produto>(StatusCodes.Status200OK)
 .Produces(StatusCodes.Status404NotFound)
 .WithName("DeletaProduto")
 .WithTags("Produtos");

app.MapGet("/produtos/nome/{criterio}", (string criterio, AppDbContext db) =>
{
    var produtosSelecionados = db.Produtos.Where(x => x.Nome
                              .ToLower().Contains(criterio.ToLower()))
                              .ToList();

    return produtosSelecionados.Count > 0
           ? Results.Ok(produtosSelecionados)
           : Results.NotFound(Array.Empty<Produto>());
})
  .Produces<List<Produto>>(StatusCodes.Status200OK)
  .WithName("FiltrarPorNome")
  .WithTags("Produtos");

app.MapGet("/produtosporpagina", async (int numeroPagina, int tamanhoPagina, AppDbContext db) =>
await db.Produtos
               .Skip((numeroPagina - 1) * tamanhoPagina)
               .Take(tamanhoPagina)
               .ToListAsync()
)
 .Produces<List<Produto>>(StatusCodes.Status200OK)
 .WithName("ProdutosPorPagina")
 .WithTags("Produtos");


// Configure the HTTP request pipeline.- Configure
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.Run();
public partial class Program { }