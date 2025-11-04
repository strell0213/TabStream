using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabStream2
{
    public class Calculate
    {
        static public double XToSS(double posX)
        {
            double ss = posX / 20;
            return ss;
        }

        static public double SSToX(double ss)
        {
            double X = ss * 20;
            return X;
        }

        static public double MillisecondsToSeconds(double mm)
        {
            return mm / 1000;
        }
    }
}
