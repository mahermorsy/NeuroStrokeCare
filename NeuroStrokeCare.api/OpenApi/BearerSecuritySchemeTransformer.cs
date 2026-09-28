using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace NeuroStrokeCare.api.OpenApi
{
    // بيضيف زرار "Authorize" في واجهة الاختبار (Scalar) عشان تقدر تحط الـ Bearer Token
    // وتختبر الـ Endpoints المحمية زي api/auth/me من غير ما تضطر تضيفه يدوي في كل Request
    //
    // ملحوظة: Microsoft.OpenApi (اللي بييجي مع .NET 10) بقى بيستخدم Interfaces
    // (IOpenApiSecurityScheme) بدل الـ Concrete Types زي المتعرف عليه في أمثلة .NET 9 القديمة،
    // فـ Dictionary لازم يكون IOpenApiSecurityScheme مش OpenApiSecurityScheme،
    // وبدل ما نحط Reference جوه OpenApiSecurityScheme بنستخدم OpenApiSecuritySchemeReference.
    public class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var authSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();

            if (authSchemes.Any(s => s.Name == "Bearer"))
            {
                var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "اعمل Login أو Register الأول وهات الـ Token، وحطه هنا (من غير كلمة Bearer)."
                    }
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = securitySchemes;

                foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
                {
                    operation.Value.Security ??= [];
                    operation.Value.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                    });
                }
            }
        }
    }
}
