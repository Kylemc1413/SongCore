using System;
namespace SongCore.Data
{
    [Serializable]
    public class SongHashData
    {
        public long directoryHash;
        public string songHash;

        [Newtonsoft.Json.JsonConstructor]
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

        [Newtonsoft.Json.JsonConstructor]
        public AudioCacheData(string audioFileHash, float duration)
        {
            this.id = audioFileHash;
            this.duration = duration;
        }
    }
}


