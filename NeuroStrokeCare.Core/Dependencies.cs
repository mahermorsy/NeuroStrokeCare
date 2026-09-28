using Microsoft.Extensions.DependencyInjection;

namespace NeuroStrokeCare.Core
{
    public static class Dependencies
    {
        public static IServiceCollection AddCoreDependencies(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Dependencies).Assembly));
            // من AutoMapper 13 وطالع بقى الـ Assembly بتتبعت كـ Parameter منفصل بدل ما تتحط
            // جوه الـ cfg نفسه (cfg.AddMaps اتشالت)
            services.AddAutoMapper(cfg => { }, typeof(Dependencies).Assembly);

            // بيسجل كل الـ Generic Handlers بتاعت BaseService لكل Entity موجودة فعلاً في المشروع -
            // شوف الشرح الكامل في GenericHandlersRegistration.cs
            services.AddGenericBaseServiceHandlers();

            return services;
        }
    }
}
