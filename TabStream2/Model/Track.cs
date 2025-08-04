using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabStream2.Model
{
    public class Track
    {
        public int IDTrack { get; set; }
        public string Name { get; set; }

        public Track(int id)
        {
            IDTrack = id;
            Name = $"Track {id}";
        }
    }
}
