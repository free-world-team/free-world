using System;
using Game.Core;

namespace Game.Presentation
{
    /// <summary>
    /// Presentation-only compatibility for legacy authored placeholder identities.
    /// Simulation and serialized content retain their stable IDs; only visual lookup is normalized.
    /// </summary>
    public static class FormalPresentationIdResolver
    {
        private const string PlaceholderQinglanPrefix = "placeholder.presentation.qinglan.";
        private const string PlaceholderSkillPrefix = "placeholder.presentation.qinglan.skill.";
        private const string FormalSkillPrefix = "qinglan.presentation.skill.";
        private const string RuntimeSkillPrefix = "qinglan.skill.";

        public static ContentId NormalizeProfileId(ContentId source)
        {
            if (!source.IsValid) return source;
            var value = source.Value;
            string normalized;
            if (value.StartsWith(PlaceholderSkillPrefix, StringComparison.Ordinal))
                normalized = FormalSkillPrefix + value.Substring(PlaceholderSkillPrefix.Length);
            else if (value.StartsWith(PlaceholderQinglanPrefix, StringComparison.Ordinal))
                normalized = "qinglan." + value.Substring(PlaceholderQinglanPrefix.Length);
            else
                return source;

            var result = ContentId.Create(normalized);
            return result.IsSuccess ? result.Value : source;
        }

        public static bool TryGetSkillVfxKey(ContentId source, out string stableKey)
        {
            stableKey = string.Empty;
            if (!source.IsValid) return false;
            var value = source.Value;
            string suffix;
            if (value.StartsWith(PlaceholderSkillPrefix, StringComparison.Ordinal))
                suffix = value.Substring(PlaceholderSkillPrefix.Length);
            else if (value.StartsWith(FormalSkillPrefix, StringComparison.Ordinal))
                suffix = value.Substring(FormalSkillPrefix.Length);
            else if (value.StartsWith(RuntimeSkillPrefix, StringComparison.Ordinal))
                suffix = value.Substring(RuntimeSkillPrefix.Length);
            else
                return false;

            var family = "base";
            const string evolvedPrefix = "evolved.";
            if (suffix.StartsWith(evolvedPrefix, StringComparison.Ordinal))
            {
                family = "evolved";
                suffix = suffix.Substring(evolvedPrefix.Length);
            }
            if (suffix.Length == 0 || suffix.IndexOf('.') >= 0) return false;
            stableKey = "qinglan/skill/" + family + "/" + suffix.Replace('_', '-') + "/vfx";
            return true;
        }
    }
}
