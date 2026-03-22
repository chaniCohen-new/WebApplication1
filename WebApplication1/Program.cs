using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

// --- הייבוא של הפרויקטים שלך ---
using c__nRepository_2026.Interfaces;
using c__nRepository_2026.Repositories;
using c__nRepository_2026.Entities; // נוסף כדי למצוא את IdentificationContext
using c__service_2026.Interfaces;
using c__service_2026.Services;
using c__service_2026.Profiles;
using c__2026_data;

var builder = WebApplication.CreateBuilder(args);

// --- 1. חיבור למסד נתונים ---
// אם ה-Context שלך נמצא בתיקייה אחרת, הקלידי את המילה IdentificationContext, לחצי עליה, ולחצי Alt+Enter כדי שהוא יוסיף את ה-using הנכון אוטומטית.
builder.Services.AddDbContext<IdentificationContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- 2. רישום AutoMapper ---
builder.Services.AddAutoMapper(typeof(MappingProfile));

// --- 3. הזרקת תלויות (Dependency Injection) - Repositories ---
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICharacterRepository, CharacterRepository>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
// שגיאה CS0311: אם השורה הבאה אדומה, ראי את "צעד 3" למטה!
builder.Services.AddScoped<IGalleryRepository, GalleryRepository>();
// תיקנתי את השם ל-DetectedCharacterRepository לפי ההיסטוריה שלך
builder.Services.AddScoped<IDetectedRepository, DetectedCharacterRepository>();

// --- 4. הזרקת תלויות (Dependency Injection) - Services ---
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IGalleryService, GalleryService>();
builder.Services.AddScoped<IDetectedService, DetectedCharacterService>();

// --- 5. הזרקת שירות Azure AI ---
var azureEndpoint = builder.Configuration["AzureVision:Endpoint"] ?? "";
var azureKey = builder.Configuration["AzureVision:ApiKey"] ?? "";
builder.Services.AddScoped<IAzureVisionService>(sp => new AzureVisionService(azureEndpoint, azureKey));

// --- 6. הגדרות JWT Authentication ---
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKeyString = jwtSettings["Secret"] ?? "MySuperSecretKeyForGalleryProject2026WhichNeedsToBeVeryLongAndSecure!";
var secretKey = Encoding.UTF8.GetBytes(secretKeyString);

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
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "GalleryApi2026",
        ValidAudience = jwtSettings["Audience"] ?? "GalleryClients",
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };
});

// --- 7. הגדרות Controllers ו-Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gallery API 2026", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "הכניסי את הטוקן בצורה הבאה: Bearer {your_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

// --- 8. הגדרת CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// --- בניית ה-Middleware Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();