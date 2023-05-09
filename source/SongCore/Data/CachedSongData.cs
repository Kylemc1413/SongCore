using System;
using Newtonsoft.Json;

namespace SongCore.Data
{
    [Serializable]
    public class SongHashData
    {
        public long directoryHash;
        public string songHash;

        [JsonConstructor]
        public SongHashData(long directoryHash, string songHash)
        {
            this.directoryHash = directoryHash;
            this.songHash = songHash;
        }
    }

    [Serializable]
    public class AudioCacheData
    {
        public string id;
        public float duration;

        [JsonConstructor]
        public AudioCacheData(string audioFileHash, float duration)
        {
            id = audioFileHash;
            this.duration = duration;
        }
    }
}