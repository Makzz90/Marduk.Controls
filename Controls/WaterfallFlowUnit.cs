using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Marduk.Controls
{
    public class WaterfallFlowUnit
    {
        public object Item
        {
            get { return _item; }
            set { _item = value; }
        }
        public int StackIndex
        {
            get { return _stackIndex; }
            set { _stackIndex = value; }
        }
        public double Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        public Size DesiredSize
        {
            get { return _desiredSize; }
            set { _desiredSize = value; }
        }

        private object _item;
        private int _stackIndex = -1;
        private double _offset = -1;
        private Size _desiredSize;

        public WaterfallFlowUnit(object item, Size desiredSize)
        {
            _item = item;
            _desiredSize = desiredSize;
        }
    }
}
