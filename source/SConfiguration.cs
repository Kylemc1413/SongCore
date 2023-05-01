using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace SongCore
{
    internal class SConfiguration
    {
        public virtual bool CustomSongNoteColors { get; set; } = true;
        public virtual bool CustomSongObstacleColors { get; set; } = true;
        public virtual bool CustomSongEnvironmentColors { get; set; } = true;
        public virtual bool CustomSongPlatforms { get; set; } = true;
        public virtual bool DisplayDiffLabels { get; set; } = true;
        public virtual bool ForceLongPreviews { get; set; } = true;
    }
}