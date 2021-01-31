using System.Collections.Generic;

namespace ddbConsole
{
    internal class RecordLine
    {
        public RecordLine()
        {
        }

        public string Token { get; set; }
        public int Index { get; set; }
        public bool Finished { get; set; }
        public List<int> StuffIds { get; set; }
    }
}