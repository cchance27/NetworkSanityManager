using System;
using System.IO;
using System.Linq;

namespace NetworkSanityManager
{
    public static class Tools
    {
        /// <summary>
        /// Compares 2 memory streams to see if they match for comparing files.
        /// </summary>
        /// <param name="ms1"></param>
        /// <param name="ms2"></param>
        /// <returns></returns>
        public static bool CompareMemoryStreams(MemoryStream ms1, MemoryStream ms2)
        {
            if (ms1.Length != ms2.Length)
                return false;

            var msArray1 = ms1.ToArray();
            var msArray2 = ms2.ToArray();

            return msArray1.SequenceEqual(msArray2);
        }
    }
}
