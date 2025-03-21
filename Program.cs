using Microsoft.EntityFrameworkCore;
using Backend.Db;
using Backend.Services;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// Specifying where to find static files when using Angular.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot", // Ensures wwwroot is used for serving Angular
    ApplicationName = typeof(Program).Assembly.FullName,
    ContentRootPath = AppContext.BaseDirectory
});

// Getting password and connection data from EV
var mysqlPassword = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
var connectionString = builder.Configuration.GetConnectionString("MySQLConnection").Replace("%MYSQL_PASSWORD%", mysqlPassword);

// Creating the JWT token
var jwtkey = builder.Configuration["JwtSettings:SecretKey"];
var key = Encoding.ASCII.GetBytes(jwtkey);

/*
    Adding authentication to test login functionality
*/
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

/*
    This allows me to mock logging in to the app by registering a token after
    fake user logsin. 
*/
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

/*
    Add DB context to builder.
    Scoped lifetime, new instance created per HTTP request
    The same instance is shared across services within that request
    DbContext is disposed after request is completed
*/
builder.Services.AddDbContext<NoteDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 42)))
);

// Add Controllers and other services to Builder
builder.Services.AddControllers();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TokenService>();

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

app.UseHttpsRedirection();

// Use the CORS policy before other middleware
app.UseCors("AllowAngularApp");

// Serve default files (like index.html)
// Automatically serve index.html if the request matches
app.UseDefaultFiles();  
// Serve Angular static files from wwwroot
// Serve static files (JS, CSS, images, etc.)
app.UseStaticFiles();  

// Routing for API controllers
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map API controllers
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

// Ensure Angular handles routing for SPA (Single Page Application)
// Handle any unknown routes by serving index.html
app.MapFallbackToFile("index.html");  

// Run the app.
app.Run();
