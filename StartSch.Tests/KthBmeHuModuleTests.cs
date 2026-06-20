using StartSch.Modules.KthBmeHu;

namespace StartSch.Tests;

[TestClass]
public sealed class KthBmeHuModuleTests
{
    [TestMethod]
    public void GetEventUrlBuildsEventDetailsUrl()
    {
        Assert.AreEqual(
            "https://www.kth.bme.hu/hallgatoknak/idopontok/550",
            KthBmeHuModule.GetEventUrl(550)
        );
    }
}
