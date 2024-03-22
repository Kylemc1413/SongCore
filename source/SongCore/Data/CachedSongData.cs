using MessagePack;

namespace SongCore.Data
{
    [MessagePackObject]
    public class SongHashData
    {
        [Key(0)]
        public long directoryHash;

        [Key(1)]
        public string songHash;

        public SongHashData(long directoryHash, string songHash)
        {
            this.directoryHash = directoryHash;
            this.songHash = songHash;
        }
    }

    [MessagePackObject]
    public class AudioCacheData
    {
        [Key(0)]
        public string id;

        [Key(1)]
        public float duration;

        public AudioCacheData(string audioFileHash, float duration)
        {
            id = audioFileHash;
            this.duration = duration;
        }
    }
}