using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using StartSch.Data;
using StartSch.Wasm;

namespace StartSch.Controllers;

public class PersonalCalendarUrlController(
    IDataProtectionProvider dataProtectionProvider,
    Db db
) : ControllerBase
{
    [Authorize]
    [HttpPost("/calendars/personal/deserialize-export-url")]
    public PersonalCalendarExportUrl DeserializeExportUrl([FromBody] string exportUrl)
    {
        var userId = User.GetId();
        PersonalCalendarExportUrl personalCalendarExportUrl = PersonalCalendarExportUrl.Deserialize(exportUrl, dataProtectionProvider);
        // TODO: Check PersonalCalendarExport.Id and user
        return personalCalendarExportUrl;
    }
}
