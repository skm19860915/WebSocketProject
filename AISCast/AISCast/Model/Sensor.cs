using System;
using System.Collections.Generic;

namespace AISCast.Model
{
    public class Sensor
    {
        public string Name { get; set; }
        public int Tracks { get; set; }
        public string Status { get; set; }
        public Dictionary<string, List<DateTime>> Statistics { get; private set; } = new Dictionary<string, List<DateTime>>();
    }
}
