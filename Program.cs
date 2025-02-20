using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OnlineLibraryAPI.Data;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1) Підключення до PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2) JWT-автентифікація
var secret = builder.Configuration["Jwt:Secret"]
             ?? throw new Exception("JWT Secret is missing in appsettings.json!");

var key = Encoding.UTF8.GetBytes(secret);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// 3) Контролери (+ налаштування ReferenceHandler.IgnoreCycles для коректної JSON-серіалізації)
builder.Services
    .AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; // Убираем Preserve
        opts.JsonSerializerOptions.WriteIndented = true;
    });

// 4) Swagger з підтримкою Bearer-токена
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OnlineLibraryAPI",
        Version = "v1"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT авторизация. Введите 'Bearer <token>'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
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

var app = builder.Build();

// 5) Консольные команды для управления ролями и очистки БД
if (args.Length > 0)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (args[0] == "set-role" && args.Length >= 3)
    {
        var email = args[1];
        var newRole = args[2].ToLower();

        if (newRole != "admin" && newRole != "client")
        {
            Console.WriteLine("Роль має бути 'admin' або 'client'.");
            return;
        }

        var user = context.Users.FirstOrDefault(u => u.Email == email);
        if (user == null)
        {
            Console.WriteLine($"користувача {email} не знайдено.");
            return;
        }

        user.Role = newRole;
        context.SaveChanges();
        Console.WriteLine($"Роль користувача {email} змінена на {newRole}.");
        return;
    }
    else if (args[0] == "clear-db")
    {
        Console.WriteLine(" Очищення всіх користувачів та книг...");
        context.Users.RemoveRange(context.Users);
        context.Books.RemoveRange(context.Books);
        context.Authors.RemoveRange(context.Authors);
        context.Genres.RemoveRange(context.Genres);
        context.Favorites.RemoveRange(context.Favorites);
        context.SaveChanges();
        Console.WriteLine(" База даних очищена.");
        return;
    }
}

// Включаємо Swagger (у dev-оточенні)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OnlineLibraryAPI v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
