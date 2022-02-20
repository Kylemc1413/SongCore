using ProtoBuf;

namespace SongCore.Data
{
    [ProtoContract]
    public class SongHashData
    {
        [ProtoMember(1)]
        public long directoryHash;

        [ProtoMember(2)]
        public string songHash = null!;

        public SongHashData()
        {
        }

        public SongHashData(long directoryHash, string songHash)
        {
            this.directoryHash = directoryHash;
            this.songHash = songHash;
        }
    }

    [ProtoContract]
    public class AudioCacheData
    {
        [ProtoMember(1)]
        public string id = null!;

        [ProtoMember(2)]
        public float duration;

        public AudioCacheData()
        {
        }

        public AudioCacheData(string audioFileHash, float duration)
        {
            id = audioFileHash;
            this.duration = duration;
        }
    }
}