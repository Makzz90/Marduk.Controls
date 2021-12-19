using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Marduk.Controls
{
    public class OrientedVirtualizingPanel : VirtualizingPanel
    {
        public IItemResizer Resizer
        {
            get { return _resizer; }
            set { _resizer = value; }
        }

        public bool DelayMeasure
        {
            get { return _delayMeasure; }
            set { _delayMeasure = value; }
        }

        private ScrollViewer _parentScrollView;
        private int _viewIndex = -1;
        private int _firstRealizationItemIndex = -1;
        private int _lastRealizationItemIndex = -1;
        //private double _width = 0;
        private int _requestShowItemIndex = -1;
        private Size _itemAvailableSize = new Size();
        private Size _itemAvailableSizeCache = new Size();

        List<object> _visableItems = new List<object>();
        private VisualWindow _requestWindow = new VisualWindow();
        private VirtualizingViewItem _measureControl;
        private DispatcherTimer _timer;
        private bool _isSkip = false;
        private bool _requestRelayout = false;
        private bool _delayMeasure = true;
        private IItemResizer _resizer;
        private ILayout _layout;
        private Point _scrollViewOffset = new Point(-1, -1);
        private Point _scrollViewOffsetCache = new Point(-1, -1);

        private Size _headerSize = new Size(0, 0);
        private Size _footerSize = new Size(0, 0);

        protected VirtualizingViewItem MeasureControl
        {
            get
            {
                if (_measureControl == null)
                {
                    _measureControl = GetContainerForItemOverride();
                    base.Children.Add(_measureControl);
                }
                return _measureControl;
            }
        }

        protected ILayout Layout { get { return _layout; } }
        protected List<object> VisableItems { get { return _visableItems; } }//RegisterReadOnlyProperty(LONGLONG, (LONGLONG)_visableItems, VisableItems);

        public OrientedVirtualizingPanel()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += OnTick;

            var mc = MeasureControl;
        }

        public void ScrollIntoView(object item)
        {
            this.ScrollIntoView(item, false);
        }

        public void ScrollIntoView(uint index)
        {
            this.ScrollIntoView(index, false);
        }

        public void ScrollIntoView(object item, bool disableAnimation)
        {
            int index = Items.IndexOf(item);
            if (index == 0)
            {
                return;
            }

            this.ScrollIntoView(index, false);
        }

        public void ScrollIntoView(uint index, bool disableAnimation)
        {
            var rect = this.Layout.GetItemLayoutRect((int)index);
            var viewSize = new Size((float)this.ParentScrollView.ViewportWidth, (float)this.ParentScrollView.ViewportHeight);
            var viewOffset = new Point((float)this.ParentScrollView.HorizontalOffset, (float)this.ParentScrollView.VerticalOffset);

            var vtOffset = viewOffset.Y;
            var vbOffset = viewOffset.Y + viewSize.Height;
            var htOffset = viewOffset.X;
            var hbOffset = viewOffset.X + viewSize.Width;

            double vTarget = double.NaN;
            double hTarget = double.NaN;

            if (rect.Top < vtOffset)
            {
                vTarget = rect.Top;
            }
            else if (rect.Bottom > vtOffset)
            {
                vTarget = rect.Bottom - viewSize.Height;
            }

            if (rect.Left < htOffset)
            {
                hTarget = rect.Left;
            }
            else if (rect.Right > hbOffset)
            {
                vTarget = rect.Right - viewSize.Width;
            }

            if (!double.IsNaN(vTarget))
            {
                if (vTarget < 0)
                {
                    vTarget = 0;
                }
            }

            if (!double.IsNaN(hTarget))
            {
                if (hTarget < 0)
                {
                    hTarget = 0;
                }
            }

            double? h = double.IsNaN(hTarget) ? null : new double?(hTarget);
            double? v = double.IsNaN(vTarget) ? null : new double?(vTarget);

            this.ParentScrollView.ChangeView(h, v, null, disableAnimation);
        }

        protected ScrollViewer ParentScrollView
        {
            get { return _parentScrollView; }
        }

        private void OnViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            var i = e.FinalView;
            int viewIndex = (int)Math.Floor(e.NextView.VerticalOffset / (_parentScrollView.ViewportHeight / 2)) + 1;
            if (viewIndex != _viewIndex)
            {
                _viewIndex = viewIndex;
                _scrollViewOffset = new Point((float)e.NextView.HorizontalOffset, (float)e.NextView.VerticalOffset);
                InvalidateMeasure();
                InvalidateArrange();
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _parentScrollView.SizeChanged -= this.OnSizeChanged;
            InvalidateMeasure();
            InvalidateArrange();
        }

        private void OnTick(object sender, object e)
        {
            _timer.Stop();
            _isSkip = false;
            InvalidateMeasure();
            InvalidateArrange();
        }

        protected virtual Size GetItemAvailableSize(Size availableSize)
        {
            return availableSize;
        }

        protected virtual bool NeedRelayout(Size availableSize)
        {
            return _layout.Width != availableSize.Width;
        }

        protected virtual void Relayout(Size availableSize)
        {
            _layout.ChangePanelSize(availableSize.Width);

            for (int i = 0; i < _layout.UnitCount; i++)
            {
                var newSize = MeasureItem(Items[i], _layout.GetItemSize(i));
                _layout.ChangeItem(i, Items[i], newSize);
            }
        }

        protected virtual ILayout GetLayout(Size availableSize)
        {
            return null;
        }

        protected virtual Size MeasureItem(object item, Size oldSize)
        {
            if (Resizer != null && oldSize.Height > 0)
            {
                return Resizer.Resize(item, oldSize, _itemAvailableSize);
            }

            if (IsItemItsOwnContainerOverride(item))
            {
                var measureControl = (ContentControl)(item);
                if(!base.Children.Contains(measureControl))
                    base.Children.Add(measureControl);
                measureControl.Measure(_itemAvailableSize);
                var result = measureControl.DesiredSize;
                base.Children.Remove(measureControl);
                return result;
            }
            else
            {
                PrepareContainerForItemOverride(MeasureControl, item);
                MeasureControl.Measure(_itemAvailableSize);
                ClearContainerForItemOverride(MeasureControl, item);
                return MeasureControl.DesiredSize;
            }
        }

        protected virtual VisualWindow GetVisibleWindow(Point offset, Size viewportSize)
        {
            return new VisualWindow() { Offset = Math.Max(offset.Y - viewportSize.Height, 0), Length = viewportSize.Height * 3 };
        }

        /// <summary>
        /// Переопределяем размеры данного элемента
        /// </summary>
        /// <param name="availableSize"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_parentScrollView == null)
            {
                _parentScrollView = (ScrollViewer)this.Parent;

                if (_parentScrollView != null)
                {
                    _parentScrollView.ViewChanging += this.OnViewChanging;
                }
            }

            if (_parentScrollView == null)
            {
                return new Size(availableSize.Width, 0);
            }

            if (_parentScrollView.ViewportHeight == 0)
            {
                return new Size(availableSize.Width, 0);
            }

            if (_layout == null)
            {
                _layout = GetLayout(availableSize);
            }
            
            _itemAvailableSize = GetItemAvailableSize(availableSize);

            if (_requestRelayout || this.NeedRelayout(availableSize))
            {
                if (this.DelayMeasure && !_requestRelayout)
                {
                    _isSkip = true;
                }

                _requestRelayout = true;
            }
            /*
            double temp0 = Math.Abs(_parentScrollView.VerticalOffset - _scrollViewOffset.Y);
            System.Diagnostics.Debug.WriteLine("abs:" + temp0);
            if (_parentScrollView.VerticalOffset > 0 && temp0 > 200)
            {
                System.Diagnostics.Debug.WriteLine("skip:" + temp0);
                _scrollViewOffset = new Point((float)(_parentScrollView.HorizontalOffset), (float)(_parentScrollView.VerticalOffset));
                Size temp = _layout.LayoutSize;
                temp.Height += Layout.HeaderSize.Height;//
                temp.Height += Layout.FooterSize.Height;//

                return temp;
            }
            */

            if (_isSkip)
            {
                _isSkip = true;
                _timer.Stop();
                _timer.Start();

                Size temp = _layout.LayoutSize;
                temp.Height += Layout.HeaderSize.Height;//
                temp.Height += Layout.FooterSize.Height;//

                return temp;
            }

            if (_requestRelayout)
            {
                _requestRelayout = false;

                if (_requestShowItemIndex < 0 && (_parentScrollView.VerticalOffset >= Layout.HeaderSize.Height))
                {
                    int requestFirstVisableItemIndex = _firstRealizationItemIndex;
                    int requestLastVisableItemIndex = _lastRealizationItemIndex;
                    
                    var items = _layout.GetVisableItems(new VisualWindow() { Offset = _parentScrollView.VerticalOffset, Length = _parentScrollView.ViewportHeight }, ref requestFirstVisableItemIndex, ref requestLastVisableItemIndex);
                    items = null;
                    for (int i = requestFirstVisableItemIndex; i <= requestLastVisableItemIndex; i++)
                    {
                        if (i >= 0)
                        {
                            if (Layout.GetItemLayoutRect(i).Top >= _parentScrollView.VerticalOffset)
                            {
                                _requestShowItemIndex = i;
                                break;
                            }
                        }
                    }
                }

                Relayout(availableSize);
            }

            if (_requestShowItemIndex >= 0)
            {
                var requestScrollViewOffset = MakeItemVisable(_requestShowItemIndex);

                if (_scrollViewOffsetCache != _scrollViewOffset)
                {
                    _scrollViewOffsetCache = _scrollViewOffset;
                    if (requestScrollViewOffset.Y != _scrollViewOffset.Y)
                    {
                        _timer.Start();
                        return _layout.LayoutSize;
                    }
                }

                _requestShowItemIndex = -1;
            }

            if (_scrollViewOffset.X < 0 || _scrollViewOffset.Y < 0)
            {
                _scrollViewOffset = new Point((float)(_parentScrollView.HorizontalOffset), (float)(_parentScrollView.VerticalOffset));
            }
            
            _requestWindow = GetVisibleWindow(_scrollViewOffset, new Size((float)_parentScrollView.ViewportWidth, (float)_parentScrollView.ViewportHeight));

            for (int i = _layout.UnitCount; i < (long)Items.Count; i++)
            {
                if (_layout.FillWindow(_requestWindow))
                {
                    break;
                }

                Size itemSize = MeasureItem(Items[i], new Size());
                _layout.AddItem(i, Items[i], itemSize);
            }

            if (!_layout.FillWindow(_requestWindow))
            {
                LoadMoreItems();
            }

            int requestFirstRealizationItemIndex = _firstRealizationItemIndex;
            int requestLastRealizationItemIndex = _lastRealizationItemIndex;

            
            var visableItems = _layout.GetVisableItems(_requestWindow, ref requestFirstRealizationItemIndex, ref requestLastRealizationItemIndex);
            //std::sort(_visableItems.begin(), _visableItems.end(), new CompareObject());
            //std::sort(visableItems.begin(), visableItems.end(), new CompareObject());
            
            
            IEnumerable<object> needRecycleItems = new List<object>();
            needRecycleItems = Items.Except(visableItems);//std::set_difference(_visableItems.begin(), _visableItems.end(), visableItems.begin(), visableItems.end(), std::back_inserter(*needRecycleItems), new CompareObject());

            foreach (var item in visableItems)
            {
                var container = RealizeItem(item);
                container.Measure(_itemAvailableSize);
            }

            foreach (var item in needRecycleItems)
            {
                RecycleItem(item);
            }

            needRecycleItems = null;
            _visableItems = null;
            _visableItems = visableItems.ToList();

            _firstRealizationItemIndex = requestFirstRealizationItemIndex;
            _lastRealizationItemIndex = requestLastRealizationItemIndex;

            _itemAvailableSizeCache = _itemAvailableSize;

            if (HeaderContainer != null)
            {
                OnHeaderMeasureOverride(availableSize);
            }

            if (FooterContainer != null)
            {
                /*
                if (_lastRealizationItemIndex + 1 == Items.Count)
                {
                    FooterContainer.Visibility = Visibility.Visible;
                }
                else
                {
                    FooterContainer.Visibility = Visibility.Collapsed;
                }
                */
                OnFooterMeasureOverride(availableSize);
            }
            Size result = _layout.LayoutSize;
            
            result.Height += Layout.HeaderSize.Height;//
            result.Height += Layout.FooterSize.Height;//

            return result;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_isSkip)
            {
                //System.Diagnostics.Debug.WriteLine("ArrangeOverride skip");
                return finalSize;
            }
            else
            {
                //System.Diagnostics.Debug.WriteLine("ArrangeOverride not skip");
            }

            if (_layout == null)
            {
                return finalSize;
            }

            if (_firstRealizationItemIndex < 0 || _lastRealizationItemIndex < 0)
            {
                return finalSize;
            }

            for (int i = _firstRealizationItemIndex; i <= _lastRealizationItemIndex; i++)
            {
                var rect = _layout.GetItemLayoutRect(i);
                var container = GetContainerFormIndex(i);
                container.Arrange(rect);
            }

            if (HeaderContainer != null)
            {
                OnHeaderArrangeOverride(finalSize);
            }

            if (FooterContainer != null)
            {
                OnFooterArrangeOverride(finalSize);
            }

            return finalSize;
        }

        protected virtual Point MakeItemVisable(int index)
        {
            var rect = _layout.GetItemLayoutRect(index);
            _parentScrollView.ChangeView(rect.Left, rect.Top, 1.0f, true);
            return new Point(rect.Left, rect.Top);
        }


        protected override void OnHeaderMeasureOverride(Size availableSize)
        {
            if (HeaderContainer == null)
                return;

            availableSize = Layout.GetHeaderAvailableSize();
            HeaderContainer.Measure(availableSize);
            //Layout.SetHeaderSize(availableSize);

            HeaderContainer.Arrange(new Rect(0, 0, availableSize.Width, HeaderContainer.DesiredSize.Height));
            Layout.SetHeaderSize(new Size(availableSize.Width, HeaderContainer.DesiredSize.Height));
        }

        protected override void OnHeaderArrangeOverride(Size finalSize)
        {
            if (HeaderContainer == null)
                return;

            HeaderContainer.Arrange(Layout.GetHeaderLayoutRect());
        }

        protected override void OnFooterMeasureOverride(Size availableSize)
        {
            if (FooterContainer == null)
                return;

            availableSize = Layout.GetFooterAvailableSize();
            FooterContainer.Measure(availableSize);
            //Layout.SetFooterSize(availableSize);

            
            
            Layout.SetFooterSize(new Size(availableSize.Width, FooterContainer.DesiredSize.Height));
            FooterContainer.Arrange(Layout.GetFooterLayoutRect());
        }

        protected override void OnFooterArrangeOverride(Size finalSize)
        {
            if (FooterContainer == null)
                return;

            FooterContainer.Arrange(Layout.GetFooterLayoutRect());
        }

        protected override void OnItemContainerSizeChanged(object item, VirtualizingViewItem itemContainer, Size newSize)
        {
            int index = Items.IndexOf(item);
            if (index >= 0)
            {
                if (newSize != Layout.GetItemSize(index))
                {
                    Layout.ChangeItem(index, item, newSize);
                    InvalidateMeasure();
                    InvalidateArrange();
                }
            }
        }

        protected override void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_layout == null)
            {
                InvalidateMeasure();
                InvalidateArrange();
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    _layout.RemoveAll();
                    InvalidateMeasure();
                    InvalidateArrange();
                    break;
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex != Items.Count - 1)
                    {
                        if (e.NewStartingIndex <= _layout.UnitCount)
                        {
                            _layout.AddItem(e.NewStartingIndex, Items[e.NewStartingIndex], MeasureItem(Items[e.NewStartingIndex], new Size(0, 0)));
                        }
                    }
                    else
                    {
                        if (_layout.FillWindow(_requestWindow))
                        {
                            break;
                        }
                    }

                    InvalidateMeasure();
                    InvalidateArrange();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.NewStartingIndex < _layout.UnitCount)
                    {
                        _layout.RemoveItem(e.NewStartingIndex);

                        InvalidateMeasure();
                        InvalidateArrange();
                    }
                    break;
                //case CollectionChange.ItemChanged:
                //    if ((long)e.Index < _layout.UnitCount)
                //    {
                //        _layout.ChangeItem(e.Index, Items.GetAt(e.Index), MeasureItem(Items.GetAt(e.Index), Size(0, 0)));

                //        InvalidateMeasure();
                //        InvalidateArrange();
                //    }
                //    break;
                default:
                    throw new Exception("Unexpected collection operation.");
                    break;
            }
        }
    }
}
