using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MissaoBackend.Data;
using MissaoBackend.Services;
using MissaoBackend.Utils;
using MissaoBackend.Models;

var builder = WebApplication.CreateBuilder(args);

const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins(
                "http://localhost:5018",
                "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// DbContext + MySQL
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(cs, new MySqlServerVersion(new Version(8, 0, 34)));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger + JWT
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Missão API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token no formato: Bearer {token}",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key is missing.");

if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Jwt:Issuer or Jwt:Audience is missing.");

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseRouting();

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed + Teste de conexão
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Database.CanConnectAsync())
            Console.WriteLine("✓ Ligação à base de dados bem-sucedida!");

        // Seed Tópicos Umbundu
        if (!await db.TopicosUmb.AnyAsync())
        {
            var topicosPt = await db.Topicos.ToListAsync();
            foreach (var topicoPt in topicosPt)
            {
                db.TopicosUmb.Add(new TopicoUmb
                {
                    Nome = topicoPt.Nome,
                    Slug = topicoPt.Slug
                });
            }

            if (topicosPt.Count > 0)
            {
                await db.SaveChangesAsync();
                Console.WriteLine($"✓ {topicosPt.Count} tópicos copiados para Umbundu!");
            }
        }

        // Seed Gestor admin
        var adminEmail = "ad@exemplo.com";
        var adminPassword = "19101989";

        if (!await db.Gestores.AnyAsync(g => g.Email == adminEmail))
        {
            var hash = PasswordHasher.Hash(adminPassword);

            db.Gestores.Add(new Gestor
            {
                Nome = "Administrador",
                Email = adminEmail,
                PasswordHash = hash
            });

            await db.SaveChangesAsync();
            Console.WriteLine($"✓ Administrador criado: {adminEmail}");
        }

        // Seed CanticoUmb
        if (!await db.CanticosUmb.AnyAsync())
        {
            var topico = await db.TopicosUmb.FirstOrDefaultAsync();
            if (topico != null)
            {
                var cantico = new CanticoUmb
                {
                    Titulo = "Cantico de Teste",
                    Slug = SlugHelper.Slugify("Cantico de Teste"),
                    Letra = "Letra de teste...",
                    TopicoId = topico.Id
                };

                db.CanticosUmb.Add(cantico);
                await db.SaveChangesAsync();

                Console.WriteLine($"✓ CanticoUmb seeded: {cantico.Titulo} (id={cantico.Id})");
            }
            else
            {
                Console.WriteLine("✗ Nenhum tópico Umbundu encontrado para associar ao CanticoUmb.");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("✗ Erro ao ligar à base de dados: " + ex.Message);
    }
}

app.Run();
