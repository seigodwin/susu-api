using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Susu.Application.Interfaces;
using Susu.Domain.Entities;

namespace Susu.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly IBaseRepository<Group> _groupRepo;

        public GroupsController(IGroupService groupService, IBaseRepository<Group> groupRepo)
        {
            _groupService = groupService;
            _groupRepo = groupRepo;
        }

        public sealed class CreateGroupDto
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal WeeklyContribution { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateGroupDto dto)
        {
            if (dto.WeeklyContribution <= 0) return BadRequest("Weekly contribution must be positive.");
            var group = await _groupService.CreateGroupAsync(dto.Name, dto.Description, dto.WeeklyContribution);
            return CreatedAtAction(nameof(Get), new { id = group.Id }, group);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var group = await _groupRepo.GetByIdAsync(id);
            if (group == null) return NotFound();
            return Ok(group);
        }

        public sealed class AddMemberDto
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public Guid? Id { get; set; }
        }

        [HttpPost("{groupId:guid}/members")]
        public async Task<IActionResult> AddMember(Guid groupId, [FromBody] AddMemberDto dto)
        {
            var member = Member.Create(dto.Name, dto.Email, groupId);
            try
            {
                await _groupService.AddMemberAsync(groupId, member);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(member);
        }

        public sealed class ContributionDto
        {
            public Guid MemberId { get; set; }
            public decimal Amount { get; set; }
            public int? Week { get; set; }
        }

        [HttpPost("{groupId:guid}/contributions")]
        public async Task<IActionResult> RecordContribution(Guid groupId, [FromBody] ContributionDto dto)
        {
            try
            {
                await _groupService.RecordContributionAsync(groupId, dto.MemberId, dto.Amount, dto.Week);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{groupId:guid}/close")]
        public async Task<IActionResult> CloseWeek(Guid groupId, [FromQuery] int? week = null)
        {
            try
            {
                var result = await _groupService.CloseWeekAsync(groupId, week);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{groupId:guid}/report")]
        public async Task<IActionResult> Report(Guid groupId, [FromQuery] int? week = null)
        {
            try
            {
                var report = await _groupService.GetReportAsync(groupId, week);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
