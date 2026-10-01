using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using NeuroStrokeCare.api.OpenApi;
using NeuroStrokeCare.Core;
using NeuroStrokeCare.Data.UserApplication;
using NeuroStrokeCare.infrastructure;
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
    app.MapScalarApiReference();

    using (var seedScope = app.Services.CreateScope())
    {
        await NeuroStrokeCare.infrastructure.DataSeeder.SeedAsync(seedScope.ServiceProvider);
    }
}


// Production Admin Bootstrap
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var configuration = scope.ServiceProvider
        .GetRequiredService<IConfiguration>();

    const string adminRole = "Admin";
    const string adminUserName = "admin";
    const string adminEmail = "admin@neurostrokecare.local";

    if (!await roleManager.RoleExistsAsync(adminRole))
    {
        await roleManager.CreateAsync(
            new IdentityRole<Guid>(adminRole)
        );
    }

    var admin = await userManager.FindByNameAsync(adminUserName);

    if (admin == null)
    {
        var password = configuration["SeedUsers:DefaultPassword"];

        if (!string.IsNullOrWhiteSpace(password))
        {
            admin = new ApplicationUser
            {
                UserName = adminUserName,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Admin",
                IsApproved = true,
                IsRootSuperAdmin = true
            };

            var result = await userManager.CreateAsync(admin, password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, adminRole);
            }
        }
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
