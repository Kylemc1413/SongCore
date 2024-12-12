using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;

namespace SongCore.UI
{
    internal class SettingsController : NotifiableBase
    {
        private readonly PluginConfig _config;

        private SettingsController(PluginConfig config)
        {
            _config = config;
        }

        [UIValue("noteColors")]
        public bool NoteColors
        {
            get => _config.CustomSongNoteColors;
            set
            {
                _config.CustomSongNoteColors = value;
                NotifyPropertyChanged();
            }
        }

        [UIValue("obstacleColors")]
        public bool ObstacleColors
        {
            get => _config.CustomSongObstacleColors;
            set
            {
                _config.CustomSongObstacleColors = value;
                NotifyPropertyChanged();
            }
        }

        [UIValue("environmentColors")]
        public bool EnvironmentColors
        {
            get => _config.CustomSongEnvironmentColors;
            set
            {
                _config.CustomSongEnvironmentColors = value;
                NotifyPropertyChanged();
            }
        }

        [UIValue("platforms")]
        public bool Platforms
        {
            get => _config.CustomSongPlatforms;
            set => _config.CustomSongPlatforms = value;
        }

        [UIValue("diffLabels")]
        public bool DiffLabels
        {
            get => _config.DisplayDiffLabels;
            set => _config.DisplayDiffLabels = value;
        }

        [UIValue("customChara")]
        public bool CustomChara
        {
            get => _config.DisplayCustomCharacteristics;
            set => _config.DisplayCustomCharacteristics = value;
        }

        [UIValue("longPreviews")]
        public bool LongPreviews
        {
            get => _config.ForceLongPreviews;
            set => _config.ForceLongPreviews = value;
        }

        [UIValue("mappercolor")]
        public bool MapperColor
        {
            get => _config.GreenMapperColor;
            set => _config.GreenMapperColor = value;
        }

        [UIValue("spawnlines")]
        public bool SpawnLines
        {
            get => _config.DisableRotationSpawnLinesOverride;
            set => _config.DisableRotationSpawnLinesOverride = value;
        }
    }
}
