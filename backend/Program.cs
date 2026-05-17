using ArangoDBNetStandard;
using ArangoDBNetStandard.Transport.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

using backend.Middlewares; 
using backend.Services;
using backend.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình CORS
builder.Services.AddCors(options => {
    options.AddPolicy("AllowReactApp", policy => {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// 2. Cấu hình DB
var arangodUri = builder.Configuration["ArangoDB:Url"];
var arangoDb = builder.Configuration["ArangoDB:Database"];
var arangoUser = builder.Configuration["ArangoDB:User"];
var arangoPassword = builder.Configuration["ArangoDB:Password"];
var transport = HttpApiTransport.UsingBasicAuth(new System.Uri(arangodUri), arangoDb, arangoUser, arangoPassword);
builder.Services.AddSingleton<IArangoDBClient>(new ArangoDBClient(transport));

builder.Services.AddHostedService<DatabaseInitializerService>();

// 3. Cấu hình Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// ✅ 2 DÒNG QUYẾT ĐỊNH SỰ SỐNG CÒN CỦA API NẰM Ở ĐÂY:
builder.Services.AddAuthorization(); 
builder.Services.AddControllers(); 

// 4. Cấu hình Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }, Scheme = "oauth2", Name = "Bearer", In = ParameterLocation.Header },
            new System.Collections.Generic.List<string>()
        }
    });
});

// 5. ĐĂNG KÝ KIẾN TRÚC 3 TẦNG (REPO & SERVICE)
builder.Services.AddScoped<SystemRepository>();
builder.Services.AddScoped<SystemService>();

builder.Services.AddScoped<LogsRepository>();
builder.Services.AddScoped<LogsService>();

builder.Services.AddScoped<SearchRepository>();
builder.Services.AddScoped<SearchService>();

builder.Services.AddScoped<DashboardRepository>();
builder.Services.AddScoped<DashboardService>();

builder.Services.AddScoped<UsersRepository>();
builder.Services.AddScoped<UsersService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<IocGraphsRepository>();
builder.Services.AddScoped<IocGraphsService>();

builder.Services.AddScoped<IocNodesRepository>();
builder.Services.AddScoped<IocNodesService>();

builder.Services.AddScoped<IocIngestRepository>();
builder.Services.AddScoped<IocIngestService>();

// 6. Cấu hình AlienVault Client
builder.Services.AddHttpClient("AlienVaultClient", client =>
{
    var baseUrl = builder.Configuration["AlienVault:BaseUrl"];
    var apiKey = builder.Configuration["AlienVault:ApiKey"];
    if (!string.IsNullOrEmpty(baseUrl)) client.BaseAddress = new System.Uri(baseUrl);
    if (!string.IsNullOrEmpty(apiKey)) client.DefaultRequestHeaders.Add("X-OTX-API-KEY", apiKey);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

// Đăng ký Middleware
app.UseMiddleware<LogsMiddleware>();
app.UseMiddleware<UserMiddleware>();

app.MapControllers();
app.Run();