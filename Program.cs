using Microsoft.EntityFrameworkCore;
using Backend.Db;
using Backend.Services;
using NuGet.Common;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddEndpointsApiExplorer();

var mysqlPassword = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");

var connectionString = builder.Configuration.GetConnectionString("MySQLConnection").Replace("%MYSQL_PASSWORD%", mysqlPassword);

builder.Services.AddDbContext<NoteDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 42)))
);

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUi(options =>
    {
        options.DocumentPath = "/openapi/v1.json";
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
