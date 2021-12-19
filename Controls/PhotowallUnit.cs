using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Marduk.Controls
{
    public class PhotowallUnit
    {
        public object Item;
        public Size DesiredSize;
        public double Offset;
        public int RowIndex;
        public Size ActualSize;
        public double ActualOffset;

        public PhotowallUnit(object item, Size desiredSize)
        {
            Item = item;
            DesiredSize = desiredSize;
        }
    }
}
