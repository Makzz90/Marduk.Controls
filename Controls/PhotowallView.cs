using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Marduk.Controls
{
    public class PhotowallView : OrientedVirtualizingPanel
    {
        private PhotowallLayout _photowallLayout;
        private PhotowallLayout Photowall
        {
            get
            {
                if (_photowallLayout == null)
                    _photowallLayout = (PhotowallLayout)base.Layout;
                return _photowallLayout;
            }
        }

        public static DependencyProperty SpacingProperty = DependencyProperty.Register("Spacing", typeof(double), typeof(PhotowallView), new PropertyMetadata(5.0, new PropertyChangedCallback(PhotowallView.OnSpacingChangedStatic)));
        public static DependencyProperty UnitSizeProperty = DependencyProperty.Register("UnitSize", typeof(int), typeof(PhotowallView), new PropertyMetadata(200.0, new PropertyChangedCallback(PhotowallView.OnUnitSizeChangedStatic)));

        public double Spacing
        {
            get { return (double)this.GetValue(SpacingProperty); }
            set { this.SetValue(SpacingProperty, value); }
        }

        public double UnitSize
        {
            get { return (double)this.GetValue(UnitSizeProperty); }
            set { this.SetValue(UnitSizeProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size result = base.MeasureOverride(availableSize);

            foreach (var item in VisableItems)
            {
                var container = GetContainerFormItem(item);
                container.Measure(Photowall.GetItemSize(item));
            }

            return result;
        }

        protected override Size GetItemAvailableSize(Size availableSize)
        {
            availableSize.Width = double.PositiveInfinity;
            availableSize.Height = (float)UnitSize;
            return availableSize;
        }

        protected override bool NeedRelayout(Size availableSize)
        {
            return base.NeedRelayout(availableSize) || Photowall.Spacing != Spacing || Photowall.UnitSize != UnitSize;
        }

        protected override void Relayout(Size availableSize)
        {
            Photowall.ChangeSpacing(Spacing);
            Photowall.ChangeUnitSize(UnitSize);
            base.Relayout(availableSize);
        }
        
        protected override ILayout GetLayout(Size availableSize)
        {
            return new PhotowallLayout(Spacing, availableSize.Width, UnitSize);
        }
        
        private static void OnSpacingChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (PhotowallView)sender;

            if (panel == null || panel.Photowall == null)
            {
                return;
            }

            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }
        
        private static void OnUnitSizeChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (PhotowallView) (sender);

            if (panel == null || panel.Photowall == null)
            {
                return;
            }

            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }
    }
}
