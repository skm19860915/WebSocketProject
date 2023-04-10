using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISCast.UI
{
    public static class TrackAssignment
    {
        public static IDictionary<string, int> trackList;

        static TrackAssignment()
        {
            trackList = new Dictionary<string, int>();
        }
    }
}
