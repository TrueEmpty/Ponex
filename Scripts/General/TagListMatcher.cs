using System;
using System.Collections.Generic;

/// <summary>Shared matching for inspector-configured tag lists.</summary>
public static class TagListMatcher
{
    public static bool Contains(IList<string> tags, string tag)
    {
        if (tags == null || string.IsNullOrEmpty(tag))
            return false;

        string candidate = tag.Trim();
        for (int i = 0; i < tags.Count; i++)
        {
            string configuredTag = tags[i];
            if (configuredTag != null
                && string.Equals(configuredTag.Trim(), candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
