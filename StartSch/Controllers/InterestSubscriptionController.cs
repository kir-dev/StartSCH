using Microsoft.AspNetCore.Mvc;
using StartSch.Data;

namespace StartSch.Controllers;

[ApiController]
[ValidateAntiForgeryToken]
public class InterestSubscriptionController(Db db) : ControllerBase
{
    [HttpPost("/api/interests/{interestId:int}/subscriptions")]
    public async Task<IActionResult> Subscribe(int interestId)
    {
        return NoContent();
    }

    [HttpDelete("/api/interests/{interestId:int}/subscriptions")]
    public async Task<IActionResult> Unsubscribe(int interestId)
    {
        return NoContent();
    }
}
