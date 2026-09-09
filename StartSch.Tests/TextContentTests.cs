namespace StartSch.Tests;

[TestClass]
public partial class TextContentTests
{
    [TestMethod]
    public void RemovesJustifiedTextAlign()
    {
        var content = new TextContent(@"<p style=""text-align: justify"">Hello</p>", null);

        // Assert: only "text-align: justify" is removed, other styles remain
        // Note: 'MinifyMarkupFormatter' is used, which removes the closing '</p>' tag
        Assert.DoesNotContain("text-align: justify", content.HtmlContent);
        Assert.AreEqual("<p>Hello", content.HtmlContent);
    }

    [TestMethod]
    public void KeepsOtherTextAlignValues()
    {
        var content = new TextContent(@"<p style=""text-align: center"">Hello</p>", null);

        // Assert: only "text-align: justify" is removed, other text-align styles persist
        // Note: 'MinifyMarkupFormatter' is used, which removes the closing '</p>' tag
        Assert.Contains("text-align: center", content.HtmlContent);
        Assert.AreEqual(@"<p style=""text-align: center"">Hello", content.HtmlContent);
    }
}
