using System.Security.Claims;
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
    protected int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}

// Kurum (müşteri) uç noktaları: denetçi hesapları kendi firmasının adına beyan veremez, belge yükleyemez
[Authorize(Roles = "Admin,OrgUser")]
public abstract class CustomerControllerBase : OrgControllerBase;

public class AuthController(IAuthService auth, IDemoService demo) : OrgControllerBase
{
    [HttpPost("register")] public async Task<ActionResult<AuthResultDto>> Register(RegisterDto dto) => Ok(await auth.RegisterAsync(dto));
    [HttpPost("login")]    public async Task<ActionResult<AuthResultDto>> Login(LoginDto dto)       => Ok(await auth.LoginAsync(dto));
    // Demo kurumu sıfırlar ve giriş yapar (Demo:Enabled kapalıysa 400)
    [HttpPost("demo")]     public async Task<ActionResult<AuthResultDto>> Demo()                    => Ok(await demo.ResetAndLoginAsync());
    // Demo denetçiyle giriş yapar; demo kurumu sıfırlamaz
    [HttpPost("demo-auditor")] public async Task<ActionResult<AuthResultDto>> DemoAuditor()         => Ok(await demo.AuditorLoginAsync());
}

public class GapController(IGapService gap) : CustomerControllerBase
{
    [HttpGet] public async Task<ActionResult<GapAnalysisDto>> Get() => Ok(await gap.GetAsync(OrgId));
}

// Kurumun denetçilere verdiği erişimler
public class AuditorsController(IAuditorAccessService access) : CustomerControllerBase
{
    [HttpGet] public async Task<ActionResult<List<AuditorAccessDto>>> List() => Ok(await access.ListAsync(OrgId));
    [HttpPost] public async Task<ActionResult<AuditorAccessDto>> Grant(GrantAuditorDto dto) => Ok(await access.GrantAsync(OrgId, dto));
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Revoke(int id) { await access.RevokeAsync(OrgId, id); return NoContent(); }
}

// Denetçi paneli: kurum kimliği token'dan değil rotadan gelir; servis her istekte erişim kaydını doğrular
[Authorize(Roles = "Auditor")]
public class AuditController(IAuditService audit) : OrgControllerBase
{
    [HttpGet("orgs")] public async Task<ActionResult<List<AuditOrgDto>>> Orgs() => Ok(await audit.OrganizationsAsync(UserId));
    [HttpGet("orgs/{orgId:int}")]
    public async Task<ActionResult<AuditOrgDetailDto>> Org(int orgId) => Ok(await audit.OrganizationAsync(UserId, orgId));
    [HttpPut("orgs/{orgId:int}/reviews/{questionId:int}")]
    public async Task<ActionResult<AuditorReviewDto>> Review(int orgId, int questionId, ReviewDto dto)
        => Ok(await audit.ReviewAsync(UserId, orgId, questionId, dto));
    [HttpGet("orgs/{orgId:int}/files/{id:int}")]
    public async Task<IActionResult> File(int orgId, int id)
    {
        var (path, type, name) = await audit.FileAsync(UserId, orgId, id);
        Response.Headers.XContentTypeOptions = "nosniff";
        return PhysicalFile(path, type, name);
    }
}

public class ScopeController(IScopeService scope) : CustomerControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() => await scope.GetAsync(OrgId) is { } r ? Ok(r) : NoContent();
    [HttpPut] public async Task<ActionResult<ScopeResultDto>> Save(ScopeDto dto) => Ok(await scope.SaveAsync(OrgId, dto));
}

public class SurveyController(ISurveyService survey) : CustomerControllerBase
{
    [HttpGet("questions")] public async Task<ActionResult<SurveyDto>> Questions() => Ok(await survey.GetQuestionsAsync(OrgId));
    [HttpPost("answers")]
    public async Task<IActionResult> Answers(SubmitAnswersDto dto) { await survey.SaveAnswersAsync(OrgId, dto); return NoContent(); }
    [HttpGet("progress")] public async Task<ActionResult<SurveyProgressDto>> Progress() => Ok(await survey.GetProgressAsync(OrgId));
}

public class FilesController(IFileService files) : CustomerControllerBase
{
    [HttpPost, RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<PolicyDto>> Upload(IFormFile file, [FromForm] string? requirementCode)
        => Ok(await files.UploadAsync(OrgId, file, requirementCode));

    [HttpGet] public async Task<ActionResult<List<PolicyDto>>> List() => Ok(await files.ListAsync(OrgId));

    [HttpPost("{id:int}/analyze")]
    public async Task<IActionResult> Reanalyze(int id) { await files.ReanalyzeAsync(OrgId, id); return NoContent(); }
}
