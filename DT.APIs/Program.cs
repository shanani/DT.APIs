using DT.APIs.Helpers;
using DT.APIs.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using System.Text;


var builder = WebApplication.CreateBuilder(args);

// REMOVED: AppDbContext since we don't need it for email queue only
// Email queue uses direct SQL connection via EmailQueueService

builder.Services.AddSingleton<IWebHostEnvironment>(builder.Environment);
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IEmailQueueService, EmailQueueService>();

// Read the JWT key from appsettings.json
var jwtKey = builder.Configuration.GetSection("Jwt:Key").Value;

// Configure authentication
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // FIXED: Only require HTTPS in production
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
    };

    // Add logging for token validation events
    x.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("Authentication failed: " + context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token validated successfully.");
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            Console.WriteLine("Received token: " + context.Token);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine("Challenge: " + context.Error + " - " + context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

// Add controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "DT APIs", Version = "v2.1" });

    // Add security definition for JWT
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \n\n" +
                      "Enter 'Bearer' [space] and then your token in the text input below. \n\n" +
                      "Example: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hub API V1"); });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/hub/swagger/v1/swagger.json", "Hub API V1"); });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();