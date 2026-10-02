using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NeuroStrokeCare.api.OpenApi;
using NeuroStrokeCare.Core;
using NeuroStrokeCare.Data.Exceptions;
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
//
// FINAL RELEASE-CANDIDATE PASS (sections 1(f), 4, 8): before this, EVERY unhandled exception -
// no matter what actually went wrong underneath - fell through to the exact same flat 500.
// ErrorBehaviorTests.Admission_ChangeStatus_OnNonExistentId_Returns500NotFriendly404 caught
// this concretely: ChangeStatus on a missing id throws a plain KeyNotFoundException, but the
// generic repository (TableRepository.cs) wraps *every* exception it catches - a genuine
// "not found", a genuine optimistic-concurrency conflict (DbUpdateConcurrencyException, see
// the new Admission.RowVersion concurrency token), anything - into the same DataAccessException
// type, so the real exception to branch on is usually one level down, in .InnerException, not
// the outer DataAccessException itself. Only a short, explicit allow-list of KNOWN, safe-to-
// expose domain failures gets its own status code below; anything not on that list still falls
// through to the original flat 500 (detail hidden outside Development) exactly as before - this
// deliberately does not try to guess a status code for every possible exception type.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        var toInspect = exception is DataAccessException dataAccessEx && dataAccessEx.InnerException != null
            ? dataAccessEx.InnerException
            : exception;

        int statusCode;
        string message;
        switch (toInspect)
        {
            // GenericRepository.ChangeStatus throws this directly (not a sentinel) when the id
            // doesn't exist - was previously indistinguishable from a real server error.
            case KeyNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                message = "The requested resource was not found.";
                break;

            // Thrown by SaveChangesAsync when Admission.RowVersion no longer matches what this
            // request read - someone else updated the same admission in the meantime.
            case DbUpdateConcurrencyException:
                statusCode = StatusCodes.Status409Conflict;
                message = "This record was changed by someone else since it was loaded. Reload and try again.";
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                message = "حصل خطأ غير متوقع أثناء تنفيذ الطلب.";
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new
        {
            error = message,
            details = app.Environment.IsDevelopment() ? exception?.Message : null
        });
    });
});

app.UseHttpsRedirection();

// Serves uploaded staff photos from a dedicated folder outside wwwroot (not from the
// published app's own files), so it survives redeploys and maps cleanly to a single
// Docker volume (see docker-compose.*.yml: neurostroke_uploads -> /app/App_Data/uploads).
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "App_Data", "uploads");
Directory.CreateDirectory(Path.Combine(uploadsPath, "staff-photos"));
app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Phase 9 (Tests): minimal testability change - top-level-statement Program is internal by
// default, which blocks WebApplicationFactory<Program> from NeuroStrokeCare.Tests. This
// partial class only makes it public; it adds no behavior.
public partial class Program { }
