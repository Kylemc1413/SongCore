using MessagePack;

namespace SongCore.Utilities
{
    internal static class MessagePackSettings
    {
        public static readonly MessagePackSerializerOptions StandardWithCompression = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
    }
}
