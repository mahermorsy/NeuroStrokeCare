using NeuroStrokeCare.Data.Entities;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.infrastructure.Intertfaces;
using NeuroStrokeCare.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.Service.Implementation
{
    public class BaseService<T, DTO> : IBaseService<T, DTO> where T : BaseEntity
    {
        #region Fields
        protected readonly IGenericRepository<T> _GenericRepo;
        protected private IMapper _mapper;
        protected readonly IUserService _UserService;
        protected readonly IUnitOfWork _UnitOfWork;
        #endregion

        #region constructor
        public BaseService(IGenericRepository<T> GenericRepo, IMapper mapper, IUserService userService)
        {
            _GenericRepo = GenericRepo;
            _mapper = mapper;
            _UserService = userService;
        }
        public BaseService(IUnitOfWork unitOfWork, IMapper mapper, IUserService userService)
        {
            _UnitOfWork = unitOfWork;
            _GenericRepo = _UnitOfWork.Repository<T>();
            _mapper = mapper;
            _UserService = userService;
        }

        #endregion

        #region Methods
        public async Task<List<DTO>> GetAllAsync()
        {
            var entities = await _GenericRepo.GetAllAsync();//Only Active Entities will be returned
            return _mapper.Map<List<T>, List<DTO>>(entities);
        }
        public async Task<DTO> GetByIdAsync(Guid id)
        {
            var entity = await _GenericRepo.GetByIdAsync(id);
            return _mapper.Map<T, DTO>(entity);
        }
        public async Task<bool> AddAsync(DTO entity)
        {
            return await AddAsync(entity, await _UserService.GetLoggedInUser());
        }
        public async Task<bool> AddAsync(DTO entity, Guid auditUserId)
        {
            var mappedEntity = _mapper.Map<DTO, T>(entity);
            return await _GenericRepo.AddAsync(mappedEntity, auditUserId);
        }
        public async Task<(bool Success, Guid EntityId)> AddAsyncWithID(DTO entity)
        {
            return await AddAsyncWithID(entity, await _UserService.GetLoggedInUser());
        }
        public async Task<(bool Success, Guid EntityId)> AddAsyncWithID(DTO entity, Guid auditUserId)
        {
            var mappedEntity = _mapper.Map<DTO, T>(entity);
            return await _GenericRepo.AddAsyncGetID(mappedEntity, auditUserId);
        }
        public async Task<bool> ChangeStatus(Guid id, int Status = (int)CurrentStatusType.Active)
        {
            try
            {
                await _GenericRepo.ChangeStatus(id, await _UserService.GetLoggedInUser(), Status);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task UpdateAsync(DTO entity)
        {
            var mappedEntity = _mapper.Map<DTO, T>(entity);
            await _GenericRepo.UpdateAsync(mappedEntity, await _UserService.GetLoggedInUser());
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            return await _GenericRepo.DeleteAsync(id);
        }
        #endregion
    }
}
