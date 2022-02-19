using System;
using Newtonsoft.Json;
using ProtoBuf;

namespace SongCore.Data
{
    [ProtoContract]
    public class SongHashData
    {
        [ProtoMember(1)]
        public long directoryHash;

        [ProtoMember(2)]
        public string songHash;

        [JsonConstructor]
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
        public string id;

        [ProtoMember(2)]
        public float duration;

        [JsonConstructor]
        public AudioCacheData(string audioFileHash, float duration)
        {
            id = audioFileHash;
            this.duration = duration;
        }
    }
}