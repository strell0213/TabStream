using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TabStream2.Model
{
    public class Playhead
    {
        public double CurrentPos { get; set; }

        public Brush PlayheadColor { get; set; }


        Playhead() { }
    }
}
