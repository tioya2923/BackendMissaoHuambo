using DotNetEnv;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MissaoBackend.Data;
using MissaoBackend.Services;
using MissaoBackend.Utils;
using MissaoBackend.Models;

// Carrega variáveis do arquivo .env automaticamente
Env.Load();

var builder = WebApplication.CreateBuilder(args);

const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// --- CONFIGURAÇÃO DE SERVIÇOS ---

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins(
            "https://localhost:7170",
            "https://localhost:3000",
            "https://www.missaonohuambo.org",
            "https://missaonohuambo.org",
            "https://missao-no-huambo-frontend-b3583f0178f6.herokuapp.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// DbContext + MySQL
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var cs = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
        ?? builder.Configuration.GetConnectionString("DefaultConnection");

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

// JWT Configuration
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? builder.Configuration["Jwt:Key"];
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["Jwt:Issuer"];
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key is missing.");

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

// HTTPS (produção)
if (builder.Environment.IsProduction())
{
    builder.WebHost.ConfigureKestrel((context, options) =>
    {
        options.Configure(context.Configuration.GetSection("Kestrel"));
    });
}

var app = builder.Build();

// --- PIPELINE DE MIDDLEWARE ---

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Missão API v1");
    c.RoutePrefix = string.Empty;
});

app.UseStaticFiles();
app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// --- MIGRATIONS E SEED DATA ---
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Console.WriteLine("→ Aplicando migrations...");
        await db.Database.MigrateAsync();

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
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL")
            ?? builder.Configuration["Admin:Email"];
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
            ?? builder.Configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            throw new InvalidOperationException("Admin email ou password não definidos.");

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
                Console.WriteLine($"✓ CanticoUmb seeded: {cantico.Titulo}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("✗ Erro durante a inicialização: " + ex.Message);
        if (ex.InnerException != null)
            Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
    }
}

app.Run();
