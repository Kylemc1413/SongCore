using System;
using IPA.Config;
using Zenject;

namespace SongCore.Hooks
{
    /// <summary>
    /// This temporarily disables BSIPA's monitoring of config files while garbage collection is disabled to avoid excessive memory allocation.
    /// </summary>
    internal class ConfigWatchersToggle : IInitializable, IDisposable
    {
        public void Initialize()
        {
            ConfigWatchersHelper.ToggleWatchers();
        }

        public void Dispose()
        {
            ConfigWatchersHelper.ToggleWatchers();
        }
    }
}
