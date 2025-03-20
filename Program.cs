using Microsoft.EntityFrameworkCore;
using Backend.Db;
using Backend.Services;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot", // Ensures wwwroot is used for serving Angular
    ApplicationName = typeof(Program).Assembly.FullName,
    ContentRootPath = AppContext.BaseDirectory
});

var mysqlPassword = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
var connectionString = builder.Configuration.GetConnectionString("MySQLConnection").Replace("%MYSQL_PASSWORD%", mysqlPassword);

var jwtkey = builder.Configuration["JwtSettings:SecretKey"];
var key = Encoding.ASCII.GetBytes(jwtkey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "NoteTakingApp",
            ValidAudience = "NoteTakingApp",
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] { }
        }
    });
});

builder.Services.AddDbContext<NoteDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 42)))
);

builder.Services.AddControllers();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TokenService>();

builder.Logging.AddConsole();

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")  // Allow your Angular app to make requests
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "NoteTaking API v1");
    });
}

app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Path}");
    await next();
});

app.UseHttpsRedirection();

// Use the CORS policy before other middleware
app.UseCors("AllowAngularApp");

// Serve default files (like index.html)
app.UseDefaultFiles();  // Automatically serve index.html if the request matches
// Serve static files (JS, CSS, images, etc.)
app.UseStaticFiles();  // Serve Angular static files from wwwroot

// Routing for API controllers
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map your API controllers
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

// Ensure Angular handles routing for SPA (Single Page Application)
app.MapFallbackToFile("index.html");  // Handle any unknown routes by serving index.html

app.Run();
