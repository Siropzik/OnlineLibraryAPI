using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OnlineLibraryAPI.Data;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1) ϳ��������� �� PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2) JWT-��������������
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

// 3) ���������� (+ ������������ ReferenceHandler.IgnoreCycles ��� �������� JSON-����������)
builder.Services
    .AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; // ������� Preserve
        opts.JsonSerializerOptions.WriteIndented = true;
    });

// 4) Swagger � ��������� Bearer-������
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
        Description = "JWT �����������. ������� 'Bearer <token>'",
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

// 5) ���������� ������� ��� ���������� ������ � ������� ��
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
            Console.WriteLine("���� �� ���� 'admin' ��� 'client'.");
            return;
        }

        var user = context.Users.FirstOrDefault(u => u.Email == email);
        if (user == null)
        {
            Console.WriteLine($"����������� {email} �� ��������.");
            return;
        }

        user.Role = newRole;
        context.SaveChanges();
        Console.WriteLine($"���� ����������� {email} ������ �� {newRole}.");
        return;
    }
    else if (args[0] == "clear-db")
    {
        Console.WriteLine(" �������� ��� ������������ �� ����...");
        context.Users.RemoveRange(context.Users);
        context.Books.RemoveRange(context.Books);
        context.Authors.RemoveRange(context.Authors);
        context.Genres.RemoveRange(context.Genres);
        context.Favorites.RemoveRange(context.Favorites);
        context.SaveChanges();
        Console.WriteLine(" ���� ����� �������.");
        return;
    }
}

// �������� Swagger (� dev-�������)
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
