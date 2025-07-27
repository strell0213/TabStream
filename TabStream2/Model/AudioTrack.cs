using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabStream2.Model
{
    public class AudioTrack
    {
        public int IDAudioTrack { get; set; }
        public string FileName { get; set; }
        public Uri AudioPath { get; set; }
        public double StartMs { get; set; }
        public double EndMs { get; set; }
        public int IDTrack { get; set; }

        public AudioTrack(int iDAudioTrack, string fileName, Uri audioPath, double startMs, double endMs, int iDTrack)
        {
            IDAudioTrack = iDAudioTrack;
            FileName = fileName;
            AudioPath = audioPath;
            StartMs = startMs;
            EndMs = endMs;
            IDTrack = iDTrack;
        }
    }
}
