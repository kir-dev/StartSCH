using Microsoft.Extensions.Caching.Memory;
using StartSch.Data;

namespace StartSch.Services;

public class UserInterestService(Db db, IMemoryCache cache)
{
    public async Task<InterestSubscription> GetInterests(int userId)
    {
        throw new NotImplementedException();
    }
}
