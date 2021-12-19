using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Animation;

namespace Marduk.Controls
{
    public enum ItemTapMode : uint
    {
        Left,
        LeftDouble,
        Right
    };

    public class VirtualizingPanel : Panel
    {
        List<VirtualizingViewItem> _recycledContainers;
        ObservableCollection<object> _items;
        ObservableCollection<object> _selectedItems;
        
        bool _shiftSelecting = false;
        bool _rightTapSelecting = false;
        //bool _userSelecting = false;
        bool _isShiftSelectEnable = false;
        bool _isRightTapSelectEnable = false;

        private ContentControl _headerContainer;
        private ContentControl _footerContainer;

        private Dictionary<object, VirtualizingViewItem> _itemContainerMap;

        public ObservableCollection<object> SelectedItems { get { return _selectedItems; } }
        private bool Selecting
        {
            get
            {
                if (IsShiftSelectEnable && _shiftSelecting == false)
                    return false;
                if (IsRightTapSelectEnable && _rightTapSelecting == false)
                    return false;
                return true; }
        }

        public bool IsRightTapSelectEnable { get { return _isRightTapSelectEnable; } }
        public bool IsShiftSelectEnable { get { return _isShiftSelectEnable; } }

        public event ItemTappedEventHandler ItemTapped;
        public event SeletionChangedEventHandler SeletionChanged;

        public delegate void ItemTappedEventHandler(object sender, ItemTappedEventArgs e);
        public delegate void SeletionChangedEventHandler(object sender, SeletionChangedEventArgs e);

        public class SeletionChangedEventArgs
        {

        }

        public class ItemTappedEventArgs
        {
            private object _item;
            private VirtualizingViewItem _container;
            private ItemTapMode _tapMode;

            public ItemTappedEventArgs(VirtualizingViewItem container, object item, ItemTapMode mode)
            {
                _container = container;
                _item = item;
                _tapMode = mode;
            }
        }

        private ISupportIncrementalLoading _sil;
        private int _loadCount = 0;
        private bool _moreItemsLoading = false;
        /*
        public void BeginSelect()
        {
            _userSelecting = true;
        }

        public void EndSelect()
        {
            _userSelecting = false;
        }
        */
        

        StyleSelector ItemContainerStyleSelector
        {
            get { return (StyleSelector)this.GetValue(ItemContainerStyleSelectorProperty); }
            set { this.SetValue(ItemContainerStyleSelectorProperty, value); }
        }


        public ListViewSelectionMode SelectionMode
        {
            get { return (ListViewSelectionMode)this.GetValue(SelectionModeProperty); }
            set { this.SetValue(SelectionModeProperty, value); }
        }

        DependencyProperty SelectionModeProperty = DependencyProperty.Register(nameof(SelectionMode), typeof(ListViewSelectionMode), typeof(VirtualizingPanel), new PropertyMetadata(ListViewSelectionMode.None, new PropertyChangedCallback(OnSelectionModeChangedStatic)));

        private static void OnSelectionModeChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingPanel)sender;

            if (panel == null)
            {
                return;
            }

            panel.OnSelectionModeChanged((ListViewSelectionMode)e.NewValue);
        }

        private void OnSelectionModeChanged(ListViewSelectionMode newValue)
        {
            foreach(var key in _itemContainerMap)
            {
                var container = key.Value;
                container.InSelectingMode = newValue != ListViewSelectionMode.None;
            }
        }

        public VirtualizingPanel()
        {
            _items = new ObservableCollection<object>();
            _selectedItems = new ObservableCollection<object>();
            _recycledContainers = new List<VirtualizingViewItem>();
            _itemContainerMap = new Dictionary<object, VirtualizingViewItem>();

            _items.CollectionChanged += OnItemsChanged;
            _selectedItems.CollectionChanged += OnSeletionChanged;

            this.Tapped += OnItemTapped;
            this.DoubleTapped += OnItemDoubleTapped;
            this.RightTapped += OnItemRightTapped;
            this.KeyDown += OnKeyDown;
            this.KeyUp += OnKeyUp;

            this.ChildrenTransitions = new TransitionCollection();
            this.ChildrenTransitions.Add(new RepositionThemeTransition());
            this.ChildrenTransitions.Add(new AddDeleteThemeTransition());
            this.ChildrenTransitions.Add(new ReorderThemeTransition());
            this.ChildrenTransitions.Add(new PaneThemeTransition());
            this.ChildrenTransitions.Add(new EdgeUIThemeTransition());
        }

        protected ObservableCollection<object> Items
        {
            get { return this._items; }
        }

        private void OnKeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (IsShiftSelectEnable && (e.Key | Windows.System.VirtualKey.Shift) == Windows.System.VirtualKey.Shift)
            {
                _shiftSelecting = false;
            }
        }

        private void HandleTapped(object item, ItemTapMode tapMode)
        {
            int index = Items.IndexOf(item);
            if (index < 0)
            {
                return;
            }

            var container = GetContainerFormItem(item);

            switch (this.SelectionMode)
            {
                case ListViewSelectionMode.Single:
                    if (Selecting)
                    {
                        if (_selectedItems.Count == 1 && _selectedItems.IndexOf(item) >= 0)
                        {
                            if (container != null)
                            {
                                container.Selected = false;
                            }
                            _selectedItems.Clear();
                        }
                        else
                        {
                            foreach (var i in _selectedItems)
                            {
                                var c = GetContainerFormItem(i);
                                if (c != null)
                                {
                                    c.Selected = false;
                                }
                            }
                            _selectedItems.Clear();
                            _selectedItems.Add(item);

                            container.Selected = true;
                        }

                    }
                    break;
                case ListViewSelectionMode.Multiple:
                    if (Selecting)
                    {
                        if ((index = _selectedItems.IndexOf(item)) >= 0)
                        {
                            if (container != null)
                            {
                                container.Selected = false;
                            }
                            _selectedItems.RemoveAt(index);
                        }
                        else
                        {
                            if (container != null)
                            {
                                container.Selected = true;
                            }
                            _selectedItems.Add(item);
                        }

                    }
                    break;
                case ListViewSelectionMode.None:
                default:
                    ItemTapped?.Invoke(this, new ItemTappedEventArgs(container, item, tapMode));
                    break;
            }

        }

        protected VirtualizingViewItem GetContainerFormItem(object item)
        {
            if (_itemContainerMap.ContainsKey(item))
            {
                return _itemContainerMap[item];
            }
            else
            {
                return null;
            }
        }

        private void OnKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (IsShiftSelectEnable && (e.Key | Windows.System.VirtualKey.Shift) == Windows.System.VirtualKey.Shift)
            {
                _shiftSelecting = true;
            }
        }

        private void OnItemRightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (e.OriginalSource == this || e.OriginalSource == HeaderContainer || e.OriginalSource == FooterContainer)
            {
                return;
            }

            var item = (e.OriginalSource as FrameworkElement).DataContext;

            if (item == null)
            {
                return;
            }

            if (IsRightTapSelectEnable)
            {
                _rightTapSelecting = true;
            }
            HandleTapped(item, ItemTapMode.Right);
            if (IsRightTapSelectEnable)
            {
                _rightTapSelecting = false;
            }
        }

        private void OnItemDoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (e.OriginalSource == this || e.OriginalSource == HeaderContainer || e.OriginalSource == FooterContainer)
            {
                return;
            }

            var item = (e.OriginalSource as FrameworkElement).DataContext;

            if (item == null)
            {
                return;
            }

            HandleTapped(item, ItemTapMode.LeftDouble);
        }

        private void OnItemTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (e.OriginalSource == this || e.OriginalSource == HeaderContainer || e.OriginalSource == FooterContainer)
            {
                return;
            }
            
            var item = (e.OriginalSource as FrameworkElement).DataContext;

            if (item == null)
            {
                return;
            }

            this.HandleTapped(item, ItemTapMode.Left);
        }

        private void OnSeletionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.SeletionChanged?.Invoke(this, new SeletionChangedEventArgs());
        }

        protected virtual void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e) { }

        public object ItemsSource
        {
            get { return GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public Style ItemContainerStyle
        {
            get { return (Style)(this.GetValue(ItemContainerStyleProperty)); }
            set { this.SetValue(ItemContainerStyleProperty, value); }
        }

        public DataTemplateSelector ItemTemplateSelector
        {
            get { return (DataTemplateSelector)(this.GetValue(ItemTemplateSelectorProperty)); }
            set { this.SetValue(ItemTemplateSelectorProperty, value); }
        }

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public object Footer
        {
            get { return (object)GetValue(FooterProperty); }
            set { SetValue(FooterProperty, value); }
        }

        public object Header
        {
            get { return (object)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public Style FooterContainerStyle
        {
            get { return (Style)GetValue(FooterContainerStyleProperty); }
            set { SetValue(FooterContainerStyleProperty, value); }
        }

        public DataTemplate FooterTemplate
        {
            get { return (DataTemplate)GetValue(FooterTemplateProperty); }
            set { SetValue(FooterTemplateProperty, value); }
        }

        public Style HeaderContainerStyle
        {
            get { return (Style)GetValue(HeaderContainerStyleProperty); }
            set { SetValue(HeaderContainerStyleProperty, value); }
        }


        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }


        public ContentControl HeaderContainer { get { return _headerContainer; } }
        public ContentControl FooterContainer { get { return _footerContainer; } }

        public static DependencyProperty ItemContainerStyleProperty = DependencyProperty.Register(nameof(ItemContainerStyle), typeof(Style), typeof(VirtualizingPanel), new PropertyMetadata(null, new PropertyChangedCallback(VirtualizingPanel.OnItemContainerStyleChangedStatic)));
        public static DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(VirtualizingPanel), new PropertyMetadata(null, new PropertyChangedCallback(VirtualizingPanel.OnItemsSourceChangedStatic)));
        public static DependencyProperty ItemTemplateSelectorProperty = DependencyProperty.Register(nameof(ItemTemplateSelector), typeof(DataTemplateSelector), typeof(VirtualizingPanel), new PropertyMetadata(null, new PropertyChangedCallback(VirtualizingPanel.OnItemTemplateSelectorChangedStatic)));
        public static DependencyProperty ItemTemplateProperty = DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(VirtualizingPanel), new PropertyMetadata(null, new PropertyChangedCallback(VirtualizingPanel.OnItemTemplateChangedStatic)));
        public static DependencyProperty FooterProperty = DependencyProperty.Register("Footer", typeof(object), typeof(VirtualizingPanel), new PropertyMetadata(null, new PropertyChangedCallback(VirtualizingPanel.OnFooterChangedStatic)));
        public static DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(object), typeof(VirtualizingPanel), new PropertyMetadata(null, new PropertyChangedCallback(VirtualizingPanel.OnHeaderChangedStatic)));
        public static DependencyProperty FooterContainerStyleProperty = DependencyProperty.Register("FooterContainerStyle", typeof(Style), typeof(VirtualizingPanel), new PropertyMetadata(null, new PropertyChangedCallback(VirtualizingPanel.OnFooterContainerStyleChangedStatic)));
        public static DependencyProperty FooterTemplateProperty = DependencyProperty.Register("FooterTemplate", typeof(DataTemplate), typeof(VirtualizingPanel), new PropertyMetadata(null, new PropertyChangedCallback(VirtualizingPanel.OnFooterTemplateChangedStatic)));
        public static DependencyProperty HeaderContainerStyleProperty = DependencyProperty.Register("HeaderContainerStyle", typeof(Style), typeof(VirtualizingPanel), new PropertyMetadata(null, new PropertyChangedCallback(VirtualizingPanel.OnHeaderContainerStyleChangedStatic)));
        public static DependencyProperty HeaderTemplateProperty = DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(VirtualizingPanel), new PropertyMetadata(null, new PropertyChangedCallback(VirtualizingPanel.OnHeaderTemplateChangedStatic)));
        public static DependencyProperty ItemContainerStyleSelectorProperty = DependencyProperty.Register("ItemContainerStyleSelector", typeof(StyleSelector), typeof(VirtualizingPanel), new PropertyMetadata(null, new PropertyChangedCallback(VirtualizingPanel.OnItemContainerStyleChangedStatic)));

        protected List<VirtualizingViewItem> RecycledContainers { get { return _recycledContainers; } }

        private static void OnItemsSourceChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingPanel)sender;

            if (panel == null)
            {
                return;
            }

            panel.OnItemSourceChanged(e.NewValue, e.OldValue);
        }

        protected virtual void OnItemSourceChanged(object newItems, object oldItems)
        {
            this.RecycleAllItem();
            _itemContainerMap.Clear();
            _items.Clear();
            
            if (newItems is IList items)
            {
                foreach(var item in items)
                    _items.Add(item);
            }
            else
            {
                _items.Add(newItems);
            }
            
            var nc = (INotifyCollectionChanged)(oldItems);
            if (nc != null)
            {
                nc.CollectionChanged -= OnCollectionChanged;
            }

            nc = (INotifyCollectionChanged)(newItems);
            if (nc != null)
            {
                nc.CollectionChanged += OnCollectionChanged;
            }

            _sil = null;
            _loadCount = 0;

            if (newItems is ISupportIncrementalLoading)
            {
                _sil = (ISupportIncrementalLoading)newItems;
            }

        }

        protected void RecycleAllItem()
        {
            List<object> items = new List<object>();

            foreach (var item in _itemContainerMap)
            {
                items.Add(item.Key);
            }

            foreach (var item in items)

            {
                RecycleItem(item);
            }
        }

        protected virtual bool IsItemItsOwnContainerOverride(object obj)
        {
            return obj is VirtualizingViewItem;
        }

        protected void RecycleItem(object item)
        {
            VirtualizingViewItem container = GetContainerFormItem(item);

            if (container == null)
            {
                return;
            }
            
            if (base.Children.Contains(container))
            {
                base.Children.Remove(container);
                _itemContainerMap.Remove(item);
                ClearContainerForItemOverride(container, item);

                container.SizeChanged -= OnItemSizeChanged;

                if (!IsItemItsOwnContainerOverride(item))
                {
                    RecycledContainers.Add(container);
                }
            }
            else
            {
                throw new Exception( "Can't found container in panel.");
            }
        }

        protected virtual void PrepareContainerForItemOverride(VirtualizingViewItem container, object item)
        {
            if (IsItemItsOwnContainerOverride(item))
            {
                return;
            }

            container.Content = item;

            ApplyItemContainerStyle(container, item);
            ApplyItemTemplate(container, item);

            container.InSelectingMode = this.SelectionMode != ListViewSelectionMode.None;
            container.Selected = SelectedItems.IndexOf(item) >= 0;
        }

        private void ApplyItemContainerStyle(VirtualizingViewItem container, object item)
        {
            if (ItemContainerStyleSelector != null)
            {
                container.Style = ItemContainerStyleSelector.SelectStyle(item, container);
            }

            if (container.Style == null)
            {
                container.Style = ItemContainerStyle;
            }
        }

        private void ApplyItemTemplate(VirtualizingViewItem container, object item)
        {
            if (ItemTemplateSelector != null)
            {
                container.ContentTemplate = ItemTemplateSelector.SelectTemplate(item);
            }

            if (container.ContentTemplate == null)
            {
                container.ContentTemplate = ItemTemplate;
            }
        }



        private static void OnItemTemplateChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingPanel)(sender);

            if (panel == null)
            {
                return;
            }

            panel.OnItemTemplateChanged((DataTemplate)(e.NewValue), (DataTemplate)(e.OldValue));
        }

        private static void OnItemTemplateSelectorChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingPanel)(sender);

            if (panel == null)
            {
                return;
            }

            panel.OnItemTemplateSelectorChanged((DataTemplateSelector)(e.NewValue), (DataTemplateSelector)(e.OldValue));
        }

        private static void OnItemContainerStyleChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingPanel)(sender);

            if (panel == null)
            {
                return;
            }

            panel.OnItemContainerStyleChanged((Style)(e.NewValue), (Style)(e.OldValue));
        }

        private static void OnItemContainerStyleSelectorChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingPanel)(sender);

            if (panel == null)
            {
                return;
            }

            panel.OnItemContainerStyleSelectorChanged((StyleSelector)(e.NewValue), (StyleSelector)(e.OldValue));
        }

        private static void OnHeaderContainerStyleChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingPanel)(sender);

            if (panel == null)
            {
                return;
            }

            panel.OnHeaderContainerStyleChanged((Style)(e.NewValue), (Style)(e.OldValue));
        }

        private static void OnHeaderTemplateChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingPanel)(sender);

            if (panel == null)
            {
                return;
            }

            panel.OnHeaderTemplateChanged((DataTemplate)(e.NewValue), (DataTemplate)(e.OldValue));
        }

        private static void OnHeaderChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingPanel)(sender);

            if (panel == null)
            {
                return;
            }

            panel.OnHeaderChanged(e.NewValue, e.OldValue);
        }

        private static void OnFooterContainerStyleChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingPanel)(sender);

            if (panel == null)
            {
                return;
            }

            panel.OnFooterContainerStyleChanged((Style)(e.NewValue), (Style)(e.OldValue));
        }

        private static void OnFooterTemplateChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingPanel)(sender);

            if (panel == null)
            {
                return;
            }

            panel.OnFooterTemplateChanged((DataTemplate)(e.NewValue), (DataTemplate)(e.OldValue));
        }

        private static void OnFooterChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var panel = (VirtualizingPanel)(sender);

            if (panel == null)
            {
                return;
            }

            panel.OnFooterChanged(e.NewValue, e.OldValue);
        }





        private void CreateHeaderContainer()
        {
            if (_headerContainer == null && Header != null)
            {
                _headerContainer = new ContentControl();
                _headerContainer.Loaded += _container_Loaded;
                _headerContainer.Style = HeaderContainerStyle;
                _headerContainer.ContentTemplate = HeaderTemplate;
                _headerContainer.Content = Header;
                _headerContainer.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                base.Children.Insert(0, _headerContainer);
            }
        }
        
        private void CreateFooterContainer()
        {
            if (_footerContainer == null && Footer != null)
            {
                _footerContainer = new ContentControl();
                _footerContainer.Loaded += _container_Loaded;
                _footerContainer.Style = FooterContainerStyle;
                _footerContainer.ContentTemplate = FooterTemplate;
                _footerContainer.Content = Footer;
                _footerContainer.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                base.Children.Insert(0, _footerContainer);
            }
        }

        private void _container_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            element.Loaded -= _container_Loaded;
            element.Measure(new Size(this.ActualWidth, double.PositiveInfinity));
            element.Arrange(new Rect(0, 0, element.DesiredSize.Width, element.DesiredSize.Height));
        }


        protected virtual void OnHeaderContainerStyleChanged(Style newStyle, Style oldStyle)
        {
            if (newStyle == oldStyle)
            {
                return;
            }

            if (_headerContainer == null)
            {
                CreateHeaderContainer();
            }
            else
            {
                _headerContainer.Style = newStyle;
            }

            InvalidateMeasure();
            InvalidateArrange();
        }

        protected virtual void OnHeaderTemplateChanged(DataTemplate newTemplate, DataTemplate oldTemplate)
        {
            if (newTemplate == oldTemplate)
            {
                return;
            }

            if (_headerContainer == null)
            {
                CreateHeaderContainer();
            }
            else
            {
                _headerContainer.ContentTemplate = newTemplate;
            }

            InvalidateMeasure();
            InvalidateArrange();
        }

        protected virtual void OnHeaderChanged(object newHeader, object oldHeader)
        {
            if (newHeader == oldHeader)
            {
                return;
            }

            if (newHeader == null)
            {
                base.Children.Remove(_headerContainer);
                _headerContainer = null;
                return;
            }

            if (_headerContainer == null)
            {
                CreateHeaderContainer();
            }
            else
            {
                _headerContainer.Content = newHeader;
            }

            InvalidateMeasure();
            InvalidateArrange();
        }

        protected virtual void OnFooterContainerStyleChanged(Style newStyle, Style oldStyle)
        {
            if (newStyle == oldStyle)
            {
                return;
            }

            if (_footerContainer == null)
            {
                CreateFooterContainer();
            }
            else
            {
                _footerContainer.Style = newStyle;
            }

            InvalidateMeasure();
            InvalidateArrange();
        }

        protected virtual void OnFooterTemplateChanged(DataTemplate newTemplate, DataTemplate oldTemplate)
        {
            if (newTemplate == oldTemplate)
            {
                return;
            }

            if (_footerContainer == null)
            {
                CreateFooterContainer();
            }
            else
            {
                _footerContainer.ContentTemplate = newTemplate;
            }

            InvalidateMeasure();
            InvalidateArrange();
        }

        protected virtual void OnFooterChanged(object newFooter, object oldFooter)
        {
            if (newFooter == oldFooter)
            {
                return;
            }

            if (newFooter == null)
            {
                base.Children.Remove(_footerContainer);
                _footerContainer = null;
                return;
            }

            if (_footerContainer == null)
            {
                CreateFooterContainer();
            }
            else
            {
                _footerContainer.Content = newFooter;
            }

            InvalidateMeasure();
            InvalidateArrange();
        }

        protected virtual void OnHeaderMeasureOverride(Size availableSize)
        {

        }





        protected virtual void OnItemContainerStyleChanged(Style newStyle, Style oldStyle)
        {
            foreach (var item in _itemContainerMap)

            {
                ApplyItemContainerStyle(item.Value, item.Key);
            }
        }

        protected virtual void OnItemContainerStyleSelectorChanged(StyleSelector newStyleSelector, StyleSelector oldStyleSelector)
        {
            foreach (var item in _itemContainerMap)

            {
                ApplyItemContainerStyle(item.Value, item.Key);
            }
        }

        protected virtual void OnItemTemplateChanged(DataTemplate newTemplate, DataTemplate oldTemplate)
        {
            foreach (var item in _itemContainerMap)

            {
                ApplyItemTemplate(item.Value, item.Key);
            }
        }

        protected virtual void OnItemTemplateSelectorChanged(DataTemplateSelector newTemplateSelector, DataTemplateSelector oldTemplateSelector)
        {
            foreach (var item in _itemContainerMap)

            {
                ApplyItemTemplate(item.Value, item.Key);
            }
        }







        protected void ClearContainerForItemOverride(VirtualizingViewItem container, object item)
        {
            if (IsItemItsOwnContainerOverride(item))
            {
                return;
            }

            
            container.ContentTemplate = null;
            container.Style = null;
            //container.InSelectingMode = false;
            //container.Selected = false;

            container.Content = null;//было первым
        }


        /// <summary>
        /// Добавить элемент в визуальное дерево
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected VirtualizingViewItem RealizeItem(object item)
        {
            VirtualizingViewItem container = null;

            if (_itemContainerMap.ContainsKey(item))
            {
                return _itemContainerMap[item];
            }

            if (!IsItemItsOwnContainerOverride(item))
            {
                if (RecycledContainers.Count > 0)
                {
                    container = RecycledContainers[RecycledContainers.Count - 1];
                    RecycledContainers.RemoveAt(RecycledContainers.Count-1);
                }
                else
                {
                    container = GetContainerForItemOverride();
                }
            }
            else
            {
                container = (VirtualizingViewItem)item;
            }

            PrepareContainerForItemOverride(container, item);
            _itemContainerMap.Add(item, container);
            container.SizeChanged += this.OnItemSizeChanged;
            base.Children.Add(container);

            return container;
        }

        protected virtual VirtualizingViewItem GetContainerForItemOverride()
        {
            return new VirtualizingViewItem();
        }

        protected VirtualizingViewItem GetContainerFormIndex(int index)
        {
            if (index < 0 || index >= _items.Count)
            {
                throw new Exception("Index out of bounds.");
            }

            var item = _items[index];
            return GetContainerFormItem(item);
        }


        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IList newItertor = null;
            int newIndex = -1;

            if (e.NewItems != null)
            {
                newItertor = e.NewItems;
                newIndex = e.NewStartingIndex;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    //while (newItertor.Current!=null)
                    //{
                    //    _items.Insert(newIndex++, newItertor.Current);
                    //    newItertor.MoveNext();
                    //}

                    //foreach(var item in newItertor)
                    //{
                    //    _items.Insert(newIndex++, item);
                    //}
                    _items.Insert(newIndex,e.NewItems[0]);
                    break;
                case NotifyCollectionChangedAction.Move:
                    throw new Exception( "Unexpected collection operation.");
                    break;
                case NotifyCollectionChangedAction.Remove:
                    for (int i = 0; i < (long)e.OldItems.Count; i++)
                    {
                        RecycleItem(e.OldItems[i]);
                        _items.RemoveAt(e.OldStartingIndex + i);
                    }
                    //todo: remove from _selectedItems
                    break;
                case NotifyCollectionChangedAction.Replace:
                    //while (newItertor.Current!=null)
                    //{
                    //    _items[newIndex++] = newItertor.Current;
                    //    newItertor.MoveNext();
                    //}
                    foreach (var item in newItertor)
                    {
                        _items.Insert(newIndex++, item);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    this.RecycleAllItem();//line added
                    _itemContainerMap.Clear();//line added
                    _items.Clear();
                    _selectedItems.Clear();//line added
                    break;
                default:
                    throw new Exception("Unexpected collection operation.");
                    break;
            }
        }

        private void OnItemSizeChanged(object sender,SizeChangedEventArgs  e)
        {
            var container = ( VirtualizingViewItem ) (sender);
            OnItemContainerSizeChanged(GetItemFormContainer(container), container, e.NewSize);
        }

        protected object GetItemFormContainer(VirtualizingViewItem container)
        {
            if (container == null)
            {
                return null;
            }

            var item = container.Content;

            if (item!=null           && _itemContainerMap.ContainsKey(item))
            {
                return item;
            }
            else
            {
                return null;
            }
        }

        protected void LoadMoreItems(int count)
        {
            if (_sil != null)
            {
                if (_sil.HasMoreItems && !_moreItemsLoading)
                {
                    _moreItemsLoading = true;
                    //ORIGINAL LINE: concurrency::create_task(_sil->LoadMoreItemsAsync(count)).then([this](Windows::UI::Xaml::Data::LoadMoreItemsResult result)
                    
                    Task.Run(async () =>
                    {
                        await _sil.LoadMoreItemsAsync((uint)count);
                        _moreItemsLoading = false;
                    });
                    
                }
            }
        }

        protected void LoadMoreItems()
        {
            LoadMoreItems(_loadCount++);
        }

        protected virtual void OnItemContainerSizeChanged(object item, VirtualizingViewItem itemContainer, Size newSize) { }

        protected virtual void OnHeaderArrangeOverride(Size finalSize) { }

        protected virtual void OnFooterMeasureOverride(Size availableSize) { }

        protected virtual void OnFooterArrangeOverride(Size finalSize) { }
    }
}
