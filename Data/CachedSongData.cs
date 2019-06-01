using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
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
}


