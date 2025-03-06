using System.Collections;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SUIA.API.Configuration;
using SUIA.API.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connection = GetConnectionString();

builder.Services.AddEntityFrameworkNpgsql()
    .AddDbContext<ApplicationDbContext>(opt => 
        {
            opt.UseNpgsql(connection); 
            opt.EnableSensitiveDataLogging(); 
        }
    );

// builder.Services.AddDbContext<ApplicationDbContext>(o
//     => o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddServices();
builder.Services.AddEndpoints();
builder.Services.AddProblemDetails();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // ✅ Enables detailed error messages

    var scope = app.Services.CreateScope();
    var dbc = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbc?.Database.EnsureCreated();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(o => o.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/identity").MapIdentityApi<ApplicationUser>().WithTags("Identity");

app.UseEndpoints();

app.Run();

static string? GetConnectionString()
{
    foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
    {
        Console.WriteLine($"{env.Key}: {env.Value}");
    }
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
    {
        return Environment.GetEnvironmentVariable("HomxlyDevDb3");
    }
    else if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
    {
        return Environment.GetEnvironmentVariable("HomxlyProdDb");
    }
    else if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Local")
    {
        return Environment.GetEnvironmentVariable("HomxlyLocalDb3");
    }
    else
    {
        return null;
    }

}
