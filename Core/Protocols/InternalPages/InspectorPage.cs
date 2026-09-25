namespace Swifter.Core.Protocols.InternalPages;

public static class InspectorPage
{
    public static string GetHtml()
    {
        return UI.WebInspectorDebugger.Instance.GenerateInspectorPageHtml();
    }
}