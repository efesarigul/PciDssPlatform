using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PciDss.Business.Services;
using PciDss.DataAccess;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(cfg.GetConnectionString("Default")));
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IScopeService, ScopeService>();
builder.Services.AddScoped<ISurveyService, SurveyService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IGapService, GapService>();
builder.Services.AddScoped<IDemoService, DemoService>();

// Faz 2: Python RAG servisi (ai-service). Kapalıysa belgeler "Analiz bekliyor" durumunda kalır.
if (cfg.GetValue("AiService:Enabled", true))
{
    builder.Services.AddHttpClient<AnalysisService>(c =>
    {
        c.BaseAddress = new Uri(cfg["AiService:Url"] ?? "http://localhost:8000/");
        c.Timeout = TimeSpan.FromMinutes(3);
    });
    builder.Services.AddHostedService<AnalysisWorker>();
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = cfg["Jwt:Issuer"], ValidAudience = cfg["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!))
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddPolicy("spa", p =>
    p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Veritabanı yoksa oluşturur ve bekleyen migration'ları uygular (yeni kurulumda dotnet ef gerekmez)
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

// BusinessException -> 400
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (BusinessException ex)
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
