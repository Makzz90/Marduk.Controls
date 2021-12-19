using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Marduk.Controls
{
    public enum AdaptiveMode
    {
        Disable,
        MaxBased,
        MinBased
    }

    public sealed class WaterfallFlowView : OrientedVirtualizingPanel
    {
        public WaterfallFlowView()
        {
            base.Loaded += this.WaterfallFlowView_Loaded;
        }

        private void WaterfallFlowView_Loaded(object sender, RoutedEventArgs e)
        {
            base.Loaded -= WaterfallFlowView_Loaded;
            this.ResetStackCount();
        }

        public static DependencyProperty SpacingProperty = DependencyProperty.Register("Spacing", typeof(double), typeof(WaterfallFlowView), new PropertyMetadata(5.0, new PropertyChangedCallback(WaterfallFlowView.OnSpacingChangedStatic)));
        public static DependencyProperty StackCountProperty = DependencyProperty.Register("StackCount", typeof(int), typeof(WaterfallFlowView), new PropertyMetadata(2, new PropertyChangedCallback(WaterfallFlowView.OnStackCountChangedStatic)));
        public static DependencyProperty AdaptiveModeProperty = DependencyProperty.Register("AdaptiveMode", typeof(AdaptiveMode), typeof(WaterfallFlowView), new PropertyMetadata(AdaptiveMode.Disable, new PropertyChangedCallback(WaterfallFlowView.OnAdaptiveModeChangedStatic)));
        public static DependencyProperty MaxItemWidthProperty = DependencyProperty.Register("MaxItemWidth", typeof(int), typeof(WaterfallFlowView), new PropertyMetadata(300, new PropertyChangedCallback(WaterfallFlowView.OnMaxItemWidthChangedStatic)));
        public static DependencyProperty MinItemWidthProperty = DependencyProperty.Register("MinItemWidth", typeof(int), typeof(WaterfallFlowView), new PropertyMetadata(200, new PropertyChangedCallback(WaterfallFlowView.OnMinItemWidthChangedStatic)));


        private WaterfallFlowLayout _waterfallFlowLayout;
        private WaterfallFlowLayout WaterfallFlow
        {
            get
            {
                if (_waterfallFlowLayout == null)
                    _waterfallFlowLayout = (WaterfallFlowLayout)Layout;
                return this._waterfallFlowLayout;
            }
        }


        public double Spacing
        {
            get { return (double)this.GetValue(SpacingProperty); }
            set { this.SetValue(SpacingProperty, value); }
        }

        public int StackCount
        {
            get { return (int)this.GetValue(StackCountProperty); }
            set { this.SetValue(StackCountProperty, value); }
        }

        public AdaptiveMode AdaptiveMode
        {
            get { return (AdaptiveMode)this.GetValue(AdaptiveModeProperty); }
            set { this.SetValue(AdaptiveModeProperty, value); }
        }

        public int MaxItemWidth
        {
            get { return (int)this.GetValue(MaxItemWidthProperty); }
            set { this.SetValue(MaxItemWidthProperty, value); }
        }

        public int MinItemWidth
        {
            get { return (int)this.GetValue(MinItemWidthProperty); }
            set { this.SetValue(MinItemWidthProperty, value); }
        }

        private static void OnSpacingChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (WaterfallFlowView)(sender);

            if (panel == null || panel.WaterfallFlow == null)
            {
                return;
            }

            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }
        
        private static void OnStackCountChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (WaterfallFlowView)(sender);

            if (panel == null || panel.WaterfallFlow == null)
            {
                return;
            }
            //
            panel.WaterfallFlow.ChangeStackCount((int)e.NewValue);
            //
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }
        
        private static void OnAdaptiveModeChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (WaterfallFlowView)(sender);

            if (panel == null || panel.WaterfallFlow == null)
            {
                return;
            }

            switch (panel.AdaptiveMode)
            {
                default:
                case Marduk.Controls.AdaptiveMode.Disable:
                    break;
                case Marduk.Controls.AdaptiveMode.MaxBased:
                case Marduk.Controls.AdaptiveMode.MinBased:
                    panel.ResetStackCount();
                    break;
            }
        }

        
        private static void OnMaxItemWidthChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (WaterfallFlowView)(sender);

            if (panel == null || panel.WaterfallFlow == null)
            {
                return;
            }

            switch (panel.AdaptiveMode)
            {
                default:
                case Marduk.Controls.AdaptiveMode.Disable:
                case Marduk.Controls.AdaptiveMode.MinBased:
                    break;
                case Marduk.Controls.AdaptiveMode.MaxBased:
                    panel.ResetStackCount();
                    break;
            }
        }

        
        private static void OnMinItemWidthChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (WaterfallFlowView)(sender);

            if (panel == null || panel.WaterfallFlow == null)
            {
                return;
            }

            switch (panel.AdaptiveMode)
            {
                default:
                case Marduk.Controls.AdaptiveMode.Disable:
                case Marduk.Controls.AdaptiveMode.MaxBased:
                    break;
                case Marduk.Controls.AdaptiveMode.MinBased:
                    panel.ResetStackCount();
                    break;
            }
        }

        public void ResetStackCount()
        {
            ResetStackCount(new Size((float)ActualWidth, (float)ActualWidth));
        }

        public void ResetStackCount(Size availableSize)
        {
            double width = availableSize.Width;
            switch (this.AdaptiveMode)
            {
                default:
                case Marduk.Controls.AdaptiveMode.Disable:
                    break;
                case Marduk.Controls.AdaptiveMode.MinBased:
                    {
                        int maxStackCount = (int)((width + Spacing) / (MinItemWidth + Spacing));
                        StackCount = Math.Max(maxStackCount, 1);
                    }
                    break;
                case Marduk.Controls.AdaptiveMode.MaxBased:
                    {
                        int minStackCount = (int)((width + Spacing) / (MaxItemWidth + Spacing));
                        StackCount = Math.Max(minStackCount, 1);
                    }
                    break;
            }
        }

        protected override Size GetItemAvailableSize(Size availableSize)
        {
            availableSize.Width = (float)((availableSize.Width - ((StackCount - 1) * Spacing)) / StackCount);
            return availableSize;
        }
        /*
        protected override bool NeedRelayout(Size availableSize)
        {
            ResetStackCount(availableSize);
            return base.NeedRelayout(availableSize) || WaterfallFlow.Spacing != Spacing || WaterfallFlow.StackCount != StackCount;
        }
        */
        protected override void Relayout(Size availableSize)
        {
            WaterfallFlow.ChangeSpacing(Spacing);
            //
            //
            ResetStackCount(availableSize);
            //
            //
            //WaterfallFlow.ChangeStackCount(StackCount);
            base.Relayout(availableSize);
        }

        protected override ILayout GetLayout(Size availableSize)
        {
            return new WaterfallFlowLayout(Spacing, availableSize.Width, StackCount);
        }

        

        
    }
}
