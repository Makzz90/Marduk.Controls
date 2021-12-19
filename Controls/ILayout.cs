using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Marduk.Controls
{
    public interface ILayout
    {
        double Width { get; }
        Size LayoutSize { get; }
        int UnitCount { get; }
        Size HeaderSize { get; }
        Size FooterSize { get; }

        void AddItem(int index, object item, Size size);

        void ChangeItem(int index, object item, Size size);

        void RemoveItem(int index);

        void RemoveAll();


        IReadOnlyList<object> GetVisableItems(VisualWindow window, ref int firstIndex, ref int lastIndex);

        Rect GetItemLayoutRect(int index);

        bool FillWindow(VisualWindow window);

        void ChangePanelSize(double width);

        Size GetItemSize(int index);


        Size GetHeaderAvailableSize();

        Size GetFooterAvailableSize();

        bool SetHeaderSize(Size size);

        bool SetFooterSize(Size size);

        Rect GetHeaderLayoutRect();

        Rect GetFooterLayoutRect();
    }
}
