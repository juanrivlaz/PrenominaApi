using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto.Input.ApproverDelegation;
using PrenominaApi.Models.Dto.Output.ApproverDelegation;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Repositories.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class ApproverDelegationController : ControllerBase
    {
        private readonly IBaseRepositoryPrenomina<ApproverDelegation> _repository;
        private readonly IBaseRepositoryPrenomina<User> _userRepository;

        public ApproverDelegationController(
            IBaseRepositoryPrenomina<ApproverDelegation> repository,
            IBaseRepositoryPrenomina<User> userRepository)
        {
            _repository = repository;
            _userRepository = userRepository;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ApproverDelegationOutput>> Get()
        {
            var delegations = _repository.GetContextEntity().ToList();
            var userIds = delegations.Select(d => d.UserId)
                .Concat(delegations.Select(d => d.DelegateUserId))
                .Distinct()
                .ToList();
            var names = _userRepository.GetByFilter(u => userIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.Name);
            var today = DateOnly.FromDateTime(DateTime.Today);

            var result = delegations.Select(d => new ApproverDelegationOutput
            {
                Id = d.Id,
                UserId = d.UserId,
                UserName = names.TryGetValue(d.UserId, out var un) ? un : string.Empty,
                DelegateUserId = d.DelegateUserId,
                DelegateUserName = names.TryGetValue(d.DelegateUserId, out var dn) ? dn : string.Empty,
                FromDate = d.FromDate,
                ToDate = d.ToDate,
                IsActive = d.FromDate <= today && (d.ToDate == null || d.ToDate >= today),
            });

            return Ok(result);
        }

        [HttpPost]
        public ActionResult<ApproverDelegation> Store([FromBody] SaveApproverDelegation input)
        {
            if (input.UserId == input.DelegateUserId)
            {
                throw new BadHttpRequestException("El titular y el suplente no pueden ser el mismo usuario.");
            }

            if (input.ToDate != null && input.ToDate < input.FromDate)
            {
                throw new BadHttpRequestException("La fecha fin no puede ser anterior a la fecha inicio.");
            }

            var entity = new ApproverDelegation
            {
                UserId = input.UserId,
                DelegateUserId = input.DelegateUserId,
                FromDate = input.FromDate,
                ToDate = input.ToDate,
            };

            var result = _repository.Create(entity);
            _repository.Save();

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            var entity = _repository.GetById(Guid.Parse(id));
            if (entity == null)
            {
                throw new BadHttpRequestException("La suplencia no existe.");
            }

            entity.DeletedAt = DateTime.UtcNow;
            _repository.Update(entity);
            _repository.Save();

            return Ok();
        }
    }
}
