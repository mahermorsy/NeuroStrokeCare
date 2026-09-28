using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NeuroStrokeCare.api.OpenApi;
using NeuroStrokeCare.Core;
using NeuroStrokeCare.Data;
using NeuroStrokeCare.infrastructure;
using NeuroStrokeCare.infrastructure.Context;
using NeuroStrokeCare.Service;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

#region dependencies
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
builder.Services.AddInfrastructureDependencies(builder.Configuration)
                .AddServicesDependencies().AddCoreDependencies();
#endregion

#region JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key غير موجود في appsettings.json");

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
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization(options =>
{
    // أي Endpoint من غير [Authorize]/[AllowAnonymous] صريح لازم يبقى فيه توكن صحيح -
    // كل الكنترولرز (Patient/Admission/Assessments/...) كانت مكشوفة من غير تسجيل دخول قبل كده
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // واجهة اختبار احترافية على /scalar/v1 - فيها زرار Authorize عشان تحط الـ JWT Token
    app.MapScalarApiReference();

    // بيانات تجريبية (admin/doctor) عشان تقدر تسجل دخول من الواجهة وانت بتبنيها -
    // شغالة في Development بس، شوف NeuroStrokeCare.infrastructure/DataSeeder.cs
    using (var seedScope = app.Services.CreateScope())
    {
        await NeuroStrokeCare.infrastructure.DataSeeder.SeedAsync(seedScope.ServiceProvider);
    }
}

// معالجة موحّدة لأي Exception غير متوقعة (زي DataAccessException) بدل ما تطلع Stack Trace خام للـ Client
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        await context.Response.WriteAsJsonAsync(new
        {
            error = "حصل خطأ غير متوقع أثناء تنفيذ الطلب.",
            details = app.Environment.IsDevelopment() ? exception?.Message : null
        });
    });
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
