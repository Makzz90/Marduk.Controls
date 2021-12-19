using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Marduk.Controls
{
    public interface IItemResizer
    {
        Size Resize(object item, Size oldSize, Size availableSize);
    }
}
