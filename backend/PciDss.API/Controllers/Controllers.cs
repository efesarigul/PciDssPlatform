using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PciDss.Business.Dtos;
using PciDss.Business.Services;

namespace PciDss.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class OrgControllerBase : ControllerBase
{
    protected int OrgId => int.Parse(User.FindFirst("org")!.Value);
}

public class AuthController(IAuthService auth) : OrgControllerBase
{
    [HttpPost("register")] public async Task<ActionResult<AuthResultDto>> Register(RegisterDto dto) => Ok(await auth.RegisterAsync(dto));
    [HttpPost("login")]    public async Task<ActionResult<AuthResultDto>> Login(LoginDto dto)       => Ok(await auth.LoginAsync(dto));
}

[Authorize]
public class ScopeController(IScopeService scope) : OrgControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() => await scope.GetAsync(OrgId) is { } r ? Ok(r) : NoContent();
    [HttpPut] public async Task<ActionResult<ScopeResultDto>> Save(ScopeDto dto) => Ok(await scope.SaveAsync(OrgId, dto));
}

[Authorize]
public class SurveyController(ISurveyService survey) : OrgControllerBase
{
    [HttpGet("questions")] public async Task<ActionResult<SurveyDto>> Questions() => Ok(await survey.GetQuestionsAsync(OrgId));
    [HttpPost("answers")]
    public async Task<IActionResult> Answers(SubmitAnswersDto dto) { await survey.SaveAnswersAsync(OrgId, dto); return NoContent(); }
    [HttpGet("progress")] public async Task<ActionResult<SurveyProgressDto>> Progress() => Ok(await survey.GetProgressAsync(OrgId));
}

[Authorize]
public class FilesController(IFileService files) : OrgControllerBase
{
    [HttpPost, RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<PolicyDto>> Upload(IFormFile file, [FromForm] string? requirementCode)
        => Ok(await files.UploadAsync(OrgId, file, requirementCode));

    [HttpGet] public async Task<ActionResult<List<PolicyDto>>> List() => Ok(await files.ListAsync(OrgId));
}
