using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace SongCore
{
    internal class PluginConfig
    {
        public virtual bool CustomSongNoteColors { get; set; } = true;
        public virtual bool CustomSongObstacleColors { get; set; } = true;
        public virtual bool CustomSongEnvironmentColors { get; set; } = true;
        public virtual bool CustomSongPlatforms { get; set; } = true;
        public virtual bool DisplayDiffLabels { get; set; } = true;
        public virtual bool DisplayCustomCharacteristics { get; set; } = true;
        public virtual bool ForceLongPreviews { get; set; } = true;

        public virtual bool DisableRotationSpawnLinesOverride { get; set; } = false;
        public virtual bool DisableOneSaberOverride { get; set; } = false;
        public virtual bool GreenMapperColor { get; set; } = false;
    }
}