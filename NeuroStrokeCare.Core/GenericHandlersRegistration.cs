using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Handlers;
using NeuroStrokeCare.Core.Features.BaseService.Commands.Models;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Handlers;
using NeuroStrokeCare.Core.Features.BaseService.Queries.Models;
using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.PageModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeuroStrokeCare.Core
{
    // ليه الملف ده موجود:
    // الـ Handlers العامة (Generic) بتاعت BaseService - زي GetListWithFiltersQueryHandler<T, TResult> -
    // بتاخد Generic Parameters وبترجعها جوه Type تاني (List<TResult> مثلًا)، مش نفس شكل
    // IRequestHandler<TRequest, TResponse> بالظبط. الـ DI الأساسي بتاع .NET (اللي MediatR بيستخدمه
    // في RegisterServicesFromAssembly) مش بيعرف يوصل بينهم صح في الحالة دي، وبيرمي وقت التشغيل:
    // "No service for type 'MediatR.IRequestHandler...' has been registered."
    //
    // الحل: نعمل الربط ده يدويًا مرة واحدة وقت ما السيرفر يشتغل، عن طريق Reflection - بندور على
    // كل الـ Entities الموجودة فعلاً في المشروع (اللي وارثة من BaseEntity) ونربطها تلقائيًا مع
    // كل الـ 13 Handler العامين. الميزة إن أي Entity جديدة نضيفها بعدين هتشتغل تلقائي من غير
    // ما نرجع نعدل هنا تاني.
    public static class GenericHandlersRegistration
    {
        public static IServiceCollection AddGenericBaseServiceHandlers(this IServiceCollection services)
        {
            var dataAssembly = typeof(BaseEntity).Assembly;
            var coreAssembly = typeof(GenericHandlersRegistration).Assembly;

            var entityTypes = dataAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(BaseEntity).IsAssignableFrom(t))
                .ToList();

            foreach (var entityType in entityTypes)
            {
                // ==== الأوامر والاستعلامات اللي باخدة Generic Parameter واحد بس (T) ====
                RegisterOneGeneric(services, typeof(AddAsyncGetIDCommand<>), typeof(AddAsyncGetIDCommandHandler<>), entityType, typeof(ValueTuple<bool, Guid>));
                RegisterOneGeneric(services, typeof(AddNewCommand<>), typeof(AddNewCommandHandler<>), entityType, typeof(bool));
                RegisterOneGeneric(services, typeof(ChangeStatusCommand<>), typeof(ChangeStatusCommandHandler<>), entityType, typeof(int));
                RegisterOneGeneric(services, typeof(DeleteCommand<>), typeof(DeleteCommandHandler<>), entityType, typeof(bool));
                RegisterOneGeneric(services, typeof(UpdateCommand<>), typeof(UpdateCommandHansler<>), entityType, typeof(int));
                RegisterOneGeneric(services, typeof(UpdateFieldCommand<>), typeof(UpdateFieldCommandHandler<>), entityType, typeof(bool));

                RegisterOneGeneric(services, typeof(GetEntityByIdQuery<>), typeof(GetEntityByIdQueryHandler<>), entityType, entityType);
                RegisterOneGeneric(services, typeof(GetFirstOrDefaultQuery<>), typeof(GetFirstOrDefaultQueryHandler<>), entityType, entityType);
                RegisterOneGeneric(services, typeof(GetEntityListQuery<>), typeof(GetEntityListQueryHandler<>), entityType, typeof(List<>).MakeGenericType(entityType));
                RegisterOneGeneric(services, typeof(GetListwithFilterQuery<>), typeof(GetListwithFilterQueryHandler<>), entityType, typeof(List<>).MakeGenericType(entityType));

                // ==== الاستعلامات اللي باخدة Generic Parameter اتنين (T + TResult) - محتاجين DTO بتاع الـ Response ====
                // بندور على DTO اسمه {EntityName}Response جوه مشروع Core (زي AdmissionResponse لـ Admission)
                var responseType = coreAssembly.GetTypes()
                    .FirstOrDefault(t => t.IsClass && t.Name == entityType.Name + "Response");

                if (responseType == null)
                    continue; // لسه معملناش DTO لل Entity دي - نتخطاها من غير ما نوقع السيرفر

                RegisterTwoGeneric(services, typeof(GetByIdWithFiltersQuery<,>), typeof(GetByIdWithFiltersQueryHandler<,>), entityType, responseType, responseType);
                RegisterTwoGeneric(services, typeof(GetListWithFiltersQuery<,>), typeof(GetListWithFiltersQueryHandler<,>), entityType, responseType, typeof(List<>).MakeGenericType(responseType));
                RegisterTwoGeneric(services, typeof(GetPagedListQuery<,>), typeof(GetPagedListQueryHandler<,>), entityType, responseType, typeof(PagedResult<>).MakeGenericType(responseType));
            }

            return services;
        }

        private static void RegisterOneGeneric(IServiceCollection services, Type openRequestType, Type openHandlerType, Type entityType, Type responseType)
        {
            var closedRequestType = openRequestType.MakeGenericType(entityType);
            var closedHandlerType = openHandlerType.MakeGenericType(entityType);
            var serviceType = typeof(IRequestHandler<,>).MakeGenericType(closedRequestType, responseType);
            services.AddTransient(serviceType, closedHandlerType);
        }

        private static void RegisterTwoGeneric(IServiceCollection services, Type openRequestType, Type openHandlerType, Type entityType, Type resultType, Type responseType)
        {
            var closedRequestType = openRequestType.MakeGenericType(entityType, resultType);
            var closedHandlerType = openHandlerType.MakeGenericType(entityType, resultType);
            var serviceType = typeof(IRequestHandler<,>).MakeGenericType(closedRequestType, responseType);
            services.AddTransient(serviceType, closedHandlerType);
        }
    }
}
