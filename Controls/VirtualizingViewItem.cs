using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Marduk.Controls
{
    [TemplateVisualState(GroupName = "CommonStates", Name = "Normal")]
    [TemplateVisualState(GroupName = "CommonStates", Name = "Pressed")]
    [TemplateVisualState(GroupName = "CommonStates", Name = "Selected")]
    [TemplateVisualState(GroupName = "CommonStates", Name = "PressedSelected")]
    [TemplateVisualState(GroupName = "CommonStates", Name = "PointerOver")]
    [TemplateVisualState(GroupName = "CommonStates", Name = "PointerOverSelected")]

    [TemplateVisualState(GroupName = "MultiSelectStates", Name = "MultiSelectDisabled")]
    [TemplateVisualState(GroupName = "MultiSelectStates", Name = "MultiSelectEnabled")]
    public sealed class VirtualizingViewItem : ContentControl
    {
        VisualStateGroup MultiSelectStates;
        VisualStateGroup CommonStates;
        public VirtualizingViewItem()
        {
            this.DefaultStyleKey = typeof(VirtualizingViewItem);
            base.Background = new SolidColorBrush(Colors.Transparent);
            base.Loaded += VirtualizingViewItem_Loaded;
        }

        private void VirtualizingViewItem_Loaded(object sender, RoutedEventArgs e)
        {
            if(this.Selected)
                VisualStateManager.GoToState(this, "Selected", false);

            if(this.InSelectingMode)
                VisualStateManager.GoToState(this, "MultiSelectEnabled", false);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            MultiSelectStates = GetTemplateChild("MultiSelectStates") as VisualStateGroup;
            CommonStates = GetTemplateChild("CommonStates") as VisualStateGroup;

            //MultiSelectStates.CurrentStateChanged += MultiSelectStates_CurrentStateChanged;
            //CommonStates.CurrentStateChanged += CommonStates_CurrentStateChanged;
        }
        
        private void CommonStates_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine((this.Content == null ? "null" : this.Content.ToString()) + " " + e.NewState.Name);
        }

        private void MultiSelectStates_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine((this.Content == null ? "null" : this.Content.ToString()) + " " + e.NewState.Name);
        }

#region Selected
        private static DependencyProperty SelectedProperty = DependencyProperty.Register(nameof(Selected), typeof(bool), typeof(VirtualizingViewItem), new PropertyMetadata(false, new PropertyChangedCallback(OnSelectedChangedStatic)));
        
        public bool Selected
        {
            get { return (bool)this.GetValue(SelectedProperty); }
            set { this.SetValue(SelectedProperty, value); }
        }
    
        private static void OnSelectedChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs  e)
        {
            var item = (VirtualizingViewItem)sender;

            if (item != null)
            {
                item.OnSelectedChanged(e);
            }
        }
        
        private void OnSelectedChanged(DependencyPropertyChangedEventArgs  e)
        {
            VisualStateManager.GoToState(this, (bool)e.NewValue ? "Selected" : "Pressed", true);
        }
#endregion


        #region InSelectingMode
        //private static DependencyProperty InSelectingModeProperty = DependencyProperty.Register(nameof(InSelectingMode), typeof(bool), typeof(VirtualizingViewItem), new PropertyMetadata(false, new PropertyChangedCallback(OnInSelectingModeChangedStatic)));

        private bool _inSelectingMode;
        public bool InSelectingMode
        {
            get
            {
                return this._inSelectingMode;//(bool)this.GetValue(InSelectingModeProperty);
            }
            set
            { 
                //this.SetValue(InSelectingModeProperty, value);
                _inSelectingMode = value;
                //System.Diagnostics.Debug.WriteLine(this.Content == null ? "null" : this.Content.ToString() + " OnInSelectingChanged " + value.ToString());
                VisualStateManager.GoToState(this, value ? "MultiSelectEnabled" : "MultiSelectDisabled", false);
            }
        }

        private static void OnInSelectingModeChangedStatic(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var item = (VirtualizingViewItem)sender;

            if (item != null)
            {
                item.OnInSelectingChanged(e);
            }
        }

        private void OnInSelectingChanged(DependencyPropertyChangedEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine(this.Content==null ? "null" :  this.Content.ToString() + " OnInSelectingChanged " + e.NewValue.ToString());
            VisualStateManager.GoToState(this, (bool)e.NewValue ? "MultiSelectEnabled" : "MultiSelectDisabled", false);
        }
#endregion

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(e);
            VisualStateManager.GoToState(this, this.Selected ? "PressedSelected" : "Pressed", true);
        }
        
        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(e);
            VisualStateManager.GoToState(this, this.Selected ? "Selected" : "Normal", true);
        }
        
        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, this.Selected ? "Selected" : "Normal", true);
        }
        
        protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
        {
            base.OnPointerCaptureLost(e);
            VisualStateManager.GoToState(this, this.Selected ? "Selected" : "Normal", true);
        }
        
        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, this.Selected ? "PointerOverSelected" : "PointerOver", true);
        }
        
    }
}
