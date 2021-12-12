using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace SongCore
{
    internal class SConfiguration
    {
        public virtual bool CustomSongColors { get; set; } = true;
        public virtual bool CustomSongPlatforms { get; set; } = true;
        public virtual bool DisplayDiffLabels { get; set; } = true;
        public virtual bool ForceLongPreviews { get; set; } = true;
    }
}