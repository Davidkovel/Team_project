using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebApplication1.Data;
using WebApplication1.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles;
    });


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 6;
}).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "very_long_default_dev_key_change_me";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

var app = builder.Build();

app.UseCors("frontend");

using (var scope = app.Services.CreateScope())
{
    var svc = scope.ServiceProvider;

    var db = svc.GetRequiredService<ApplicationDbContext>();

    db.Database.EnsureCreated();

    var roleMgr = svc.GetRequiredService<RoleManager<IdentityRole>>();

    var userMgr = svc.GetRequiredService<UserManager<ApplicationUser>>();

    // Roles
    if (!await roleMgr.RoleExistsAsync("Admin"))
        await roleMgr.CreateAsync(new IdentityRole("Admin"));

    if (!await roleMgr.RoleExistsAsync("User"))
        await roleMgr.CreateAsync(new IdentityRole("User"));

    // Admin
    var adminEmail = "admin@example.com";

    var admin = await userMgr.FindByEmailAsync(adminEmail);

    if (admin == null)
    {
        admin = new ApplicationUser
        {
            UserName = "admin",
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userMgr.CreateAsync(admin, "Admin123!");

        if (result.Succeeded)
        {
            await userMgr.AddToRoleAsync(admin, "Admin");
        }
    }

    // Seed survey
    if (!db.Surveys.Any())
    {
        var survey = new Survey
        {
            Title = "Улюблена мова програмування",
            Description = "Оберіть мову програмування",
            IsPublished = true,
            AllowRepeatVoting = false,
            StartAt = DateTime.UtcNow.AddDays(-1),
            EndAt = DateTime.UtcNow.AddDays(7),

            CreatedById = admin.Id, // ВОТ ЭТО ГЛАВНОЕ

            Questions = new List<Question>
            {
                new Question
                {
                    Text = "Яка ваша улюблена мова?",
                    QuestionType = QuestionType.SingleChoice,
                    IsRequired = true,

                    Options = new List<Option>
                    {
                        new Option { Text = "C#" },
                        new Option { Text = "JavaScript" },
                        new Option { Text = "Python" },
                        new Option { Text = "Java" }
                    }
                }
            }
        };

        db.Surveys.Add(survey);

        db.SaveChanges();
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// keep the simple weather endpoint for compatibility
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
