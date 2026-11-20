using System.Linq;

namespace Content.Client.Examine;

public sealed partial class ExamineSystem
{
    private Type[] _markupTags = ExamineMarkupTags;

    /// <summary>
    /// Allows client modules to extend the permitted examine markup.
    /// </summary>
    public void SetAdditionalMarkupTags(params Type[] tags)
    {
        _markupTags = ExamineMarkupTags.Concat(tags).Distinct().ToArray();
    }
}
