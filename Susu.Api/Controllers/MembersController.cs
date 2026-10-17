using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Susu.Application.Interfaces;
using Susu.Domain.Entities;

namespace Susu.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembersController : ControllerBase
    {
        private readonly IBaseRepository<Member> _memberRepo;

        public MembersController(IBaseRepository<Member> memberRepo)
        {
            _memberRepo = memberRepo;
        }

        public sealed class CreateMemberDto
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public Guid? GroupId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMemberDto dto)
        {
            var member = Member.Create(dto.Name, dto.Email, dto.GroupId);
            var created = await _memberRepo.AddAsync(member);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var m = await _memberRepo.GetByIdAsync(id);
            if (m == null) return NotFound();
            return Ok(m);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateMemberDto dto)
        {
            var existing = await _memberRepo.GetByIdAsync(id);
            if (existing == null) return NotFound();
            existing.UpdateMemberDetails(dto.Name, dto.Email);
            await _memberRepo.UpdateAsync(existing);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _memberRepo.DeleteAsync(id);
            return NoContent();
        }
    }
}
