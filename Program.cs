using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MissaoBackend.Data;
using MissaoBackend.Models;
using MissaoBackend.Services;
using MissaoBackend.Utils;

var builder = WebApplication.CreateBuilder(args);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:5018", "http://localhost:3000") // ou "*"
                  .AllowAnyHeader()
                  .AllowAnyMethod();
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
builder.Services.AddSwaggerGen();

// JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Configuration error: Jwt:Key is missing.");
}
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("Configuration error: Jwt:Issuer or Jwt:Audience is missing.");
}

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Insira o token JWT no formato: Bearer {seu_token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// servir PDFs (wwwroot/partituras)
app.UseStaticFiles();

app.UseRouting();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseCors(MyAllowSpecificOrigins);


// Teste de conexão à base de dados e seed automático
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.CanConnectAsync();
        Console.WriteLine("✓ Ligação à base de dados bem-sucedida!");

        // Seed automático: copiar tópicos PT → Umbundu
        var topicosUmbCount = await db.TopicosUmb.CountAsync();
        if (topicosUmbCount == 0)
        {
            var topicosPt = await db.Topicos.ToListAsync();
            foreach (var topicoPt in topicosPt)
            {
                var topicoUmb = new TopicoUmb
                {
                    Nome = topicoPt.Nome,
                    Slug = topicoPt.Slug
                };
                db.TopicosUmb.Add(topicoUmb);
            }
            if (topicosPt.Count > 0)
            {
                await db.SaveChangesAsync();
                Console.WriteLine($"✓ {topicosPt.Count} tópicos copiados para Umbundu!");
            }
        }

        // Seed de um Gestor para testes (apenas se não existir)
            // Seed do administrador solicitado
            var adminEmail = "ad@exemplo.com";
            var adminPassword = "19101989";
            var adminGestor = await db.Gestores.FirstOrDefaultAsync(g => g.Email == adminEmail);
            if (adminGestor == null)
            {
                var hash = PasswordHasher.Hash(adminPassword);
                var gestor = new Gestor
                {
                    Nome = "Administrador",
                    Email = adminEmail,
                    PasswordHash = hash
                };
                db.Gestores.Add(gestor);
                await db.SaveChangesAsync();
                Console.WriteLine($"✓ Administrador criado: {adminEmail} / {adminPassword}");
            }

        // ...existing code...

        // Seed de um CanticoUmb de teste (apenas se não existir)
        var canticosUmbCount = await db.CanticosUmb.CountAsync();
        if (canticosUmbCount == 0)
        {
            // garante um tópico Umbundu existente para o cântico de teste
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
            } else {
                Console.WriteLine("✗ Nenhum tópico Umbundu encontrado para associar ao CanticoUmb de teste. Nenhum cântico seedado.");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("✗ Erro ao ligar à base de dados: " + ex.Message);
    }
}

app.Run();
