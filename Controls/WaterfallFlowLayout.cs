using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Marduk.Controls
{
    public class WaterfallFlowLayout : ILayout
    {
        public double Spacing { get { return _spacing; } }
        public int StackCount { get { return _stacks.Count; } }




        List<WaterfallFlowUnit> _units;
        private double _spacing;
        private double _width;

        private List<double> _stacks;
        private int _requestRelayoutIndex = -1;
        private Size _headerSize = new Size(0, 0);
        private Size _footerSize = new Size(0, 0);

        public double Width { get { return _width; } }

        public Size LayoutSize { get { return new Size(this.Width, _stacks.Max()); } }

        public int UnitCount { get { return _units.Count; } }

        public Size HeaderSize { get { return _headerSize; } }

        public Size FooterSize { get { return _footerSize; } }

        public WaterfallFlowLayout(double spacing, double width, int stackCount)
        {
            _spacing = spacing;
            _width = width;
            _units = new List<WaterfallFlowUnit>();
            _stacks = new List<double>();

            for (int i = 0; i < stackCount; i++)
            {
                _stacks.Add(0);
            }
        }

        public void AddItem(int index, object item, Size size)
        {
            size.Width = (float)((Width - ((StackCount - 1) * Spacing)) / StackCount);
            var unit = new WaterfallFlowUnit(item, size);

            if (index != -1 && index < (long)_units.Count)
            {
                _units.Insert(index, unit);
                SetRelayoutIndex(index);
            }
            else
            {
                if (!(_requestRelayoutIndex < 0 || _requestRelayoutIndex >= (long)_units.Count))
                {
                    Relayout();
                }

                //int minStackIndex = (int)std::distance(_stacks.begin(), std::min_element(_stacks.begin(), _stacks.end()));
                double min_element = _stacks.Min(); int minStackIndex = _stacks.FindIndex((m) => m == min_element);

                unit.StackIndex = minStackIndex;

                if (_stacks[minStackIndex] == 0)
                {
                    unit.Offset = _stacks[minStackIndex];
                    _stacks[minStackIndex] += size.Height;
                }
                else
                {
                    unit.Offset = _stacks[minStackIndex] + Spacing;
                    _stacks[minStackIndex] += size.Height + Spacing;
                }

                _units.Add(unit);
            }
        }
        

        public void ChangeItem(int index, object item, Size size)
        {
            if (item != null)
            {
                _units[index].Item = item;
            }

            if (size != _units[index].DesiredSize)
            {
                _units[index].DesiredSize = size;
                SetRelayoutIndex(index);
            }
        }

        public void ChangePanelSize(double width)
        {
            if (width != _width)
            {
                _width = width;
                SetRelayoutIndex(0);
            }
        }

        public Size GetFooterAvailableSize()
        {
            //var width = (Width - ((StackCount - 1) * Spacing)) / StackCount;
            //return new Size((float)width, _stacks.Max() - _stacks.Min()/*(float)((*std::max_element(_stacks.begin(), _stacks.end())) - (*std::min_element(_stacks.begin(), _stacks.end())))*/);

            return new Size(Width, double.PositiveInfinity);
        }

        public Rect GetFooterLayoutRect()
        {
            //var width = (Width - ((StackCount - 1) * Spacing)) / StackCount;
            //double min_element = _stacks.Min(); int minStackIndex = _stacks.FindIndex((m) => m == min_element);// (int)std::distance(_stacks.begin(), std::min_element(_stacks.begin(), _stacks.end()));
            //return new Rect((float)(minStackIndex * (width + Spacing)), (float)(_stacks[minStackIndex] + Spacing + _headerSize.Height), _footerSize.Width, _footerSize.Height);


            return new Rect(0, _stacks.Max() + _headerSize.Height, _footerSize.Width, _footerSize.Height);
        }

        public Size GetHeaderAvailableSize()
        {
            return new Size(Width, double.PositiveInfinity);
        }

        public Rect GetHeaderLayoutRect()
        {
            return new Rect(0, 0, _headerSize.Width, _headerSize.Height);
        }

        public Rect GetItemLayoutRect(int index)
        {
            if (!(_requestRelayoutIndex < 0 || _requestRelayoutIndex >= (long)_units.Count))
            {
                Relayout();
            }

            Rect result = new Rect();
            double unitWidth = (Width - Spacing) / _stacks.Count;

            var unit = _units[index];

            result.Height = unit.DesiredSize.Height;
            result.Width = unit.DesiredSize.Width;
            result.X = (float)(unit.StackIndex * (unitWidth + Spacing));
            result.Y = (float)(unit.Offset + _headerSize.Height);

            return result;
        }
        
        public Size GetItemSize(int index)
        {
            return _units[index].DesiredSize;
        }

        public void RemoveAll()
        {
            _units.Clear();
            for (int i = 0; i < (long)_stacks.Count; i++)
            {
                _stacks[i] = 0;
            }
            SetRelayoutIndex(0);
        }

        public void RemoveItem(int index)
        {
            SetRelayoutIndex(index);
            _units.RemoveAt(index);
        }

        public bool SetFooterSize(Size size)
        {
            if (size.Width != _footerSize.Width || size.Height != _footerSize.Height)
            {
                _footerSize = size;
                return true;
            }
            return false;
        }

        public bool SetHeaderSize(Size size)
        {
            if (size.Width != _headerSize.Width || size.Height != _headerSize.Height)
            {
                _headerSize = size;
                return true;
            }
            return false;
        }

        public void ChangeStackCount(int stackCount)
        {
            if (stackCount != _stacks.Count)
            {
                _stacks.Clear();

                for (int i = 0; i < stackCount; i++)
                {
                    _stacks.Add(0);
                }

                SetRelayoutIndex(0);
            }
        }

        public void ChangeSpacing(double spacing)
        {
            if (spacing != _spacing)
            {
                _spacing = spacing;
                SetRelayoutIndex(0);
            }
        }

        public void Relayout()
        {
            List<bool> flags = new List<bool>();
            int stackCount = 0;

            for (int i = 0; i < (long)_stacks.Count; i++)
            {
                _stacks[i] = 0;
                flags.Add(false);
            }

            for (int i = _requestRelayoutIndex - 1; i >= 0; i--)
            {
                var unit = _units[i];

                if (!flags[unit.StackIndex])
                {
                    stackCount++;
                    flags[unit.StackIndex] = true;
                    _stacks[unit.StackIndex] = unit.Offset + unit.DesiredSize.Height;
                }

                if (stackCount == _stacks.Count)
                {
                    break;
                }
            }

            for (int i = _requestRelayoutIndex; i < (long)_units.Count; i++)
            {
                var unit = _units[i];
                
                Size size = new Size((float)((Width - Spacing) / _stacks.Count), unit.DesiredSize.Height);
                unit.DesiredSize = size;


                //int minStackIndex = (int)std::distance(_stacks.begin(), std::min_element(_stacks.begin(), _stacks.end()));
                double min_element = _stacks.Min(); int minStackIndex = _stacks.FindIndex((m) => m == min_element);

                unit.StackIndex = minStackIndex;

                if (_stacks[minStackIndex] == 0)
                {
                    unit.Offset = _stacks[minStackIndex];
                    _stacks[minStackIndex] += size.Height;
                }
                else
                {
                    unit.Offset = _stacks[minStackIndex] + Spacing;
                    _stacks[minStackIndex] += size.Height + Spacing;
                }
            }

            _requestRelayoutIndex = -1;
        }

        public void SetRelayoutIndex(int index)
        {
            if (index >= 0 && _requestRelayoutIndex >= 0)
            {
                _requestRelayoutIndex = Math.Min(_requestRelayoutIndex, index);
            }
            else
            {
                _requestRelayoutIndex = Math.Max(_requestRelayoutIndex, index);
            }
        }

        public IReadOnlyList<object> GetVisableItems(VisualWindow window, ref int firstIndex, ref int lastIndex)
        {
            if (!(_requestRelayoutIndex < 0 || _requestRelayoutIndex >= (long)_units.Count))
            {
                Relayout();
            }

            window.Offset -= _headerSize.Height;

            List < object > result = new List<object>();

            if (_units.Count == 0)
            {
                firstIndex = -1;
                lastIndex = -1;
                return result;
            }

            if (firstIndex < 0)
            {
                for (int i = 0; i < (long)_units.Count; i++)
                {
                    if (_units[i].Offset >= window.Offset)
                    {
                        firstIndex = i - 1;
                        break;
                    }
                }
            }
            else
            {
                if (_units[0].Offset + _units[0].DesiredSize.Height > window.Offset)
                {
                    firstIndex = 0;
                }
                else
                {
                    if (_units[firstIndex].Offset > window.Offset)
                    {
                        for (int i = firstIndex; i >= 0; i--)
                        {
                            if (_units[i].Offset + _units[i].DesiredSize.Height < window.Offset)
                            {
                                firstIndex = i + 1;
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int i = firstIndex; i < (long)_units.Count; i++)
                        {
                            if (_units[i].Offset + _units[i].DesiredSize.Height >= window.Offset)
                            {
                                firstIndex = i;
                                break;
                            }
                        }
                    }
                }
            }

            if (firstIndex < 0)
            {
                firstIndex = 0;
            }

            if (lastIndex < 0)
            {
                for (int i = firstIndex; i < (long)_units.Count; i++)
                {
                    if (_units[i].Offset >= VisualWindowExtension.GetEndOffset(window))
                    {
                        lastIndex = i - 1;
                        break;
                    }
                }
            }
            else
            {
                if (_units[_units.Count - 1].Offset < VisualWindowExtension.GetEndOffset(window))
                {
                    lastIndex = (int)_units.Count - 1;
                }
                else
                {
                    if (_units[lastIndex].Offset > VisualWindowExtension.GetEndOffset(window))
                    {
                        for (int i = lastIndex; i >= 0; i--)
                        {
                            if (_units[i].Offset < VisualWindowExtension.GetEndOffset(window))
                            {
                                lastIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int i = lastIndex; i < (long)_units.Count; i++)
                        {
                            if (_units[i].Offset >= VisualWindowExtension.GetEndOffset(window))
                            {
                                lastIndex = i - 1;
                                break;
                            }
                        }
                    }
                }
            }

            if (lastIndex < 0)
            {
                lastIndex = (int)_units.Count - 1;
            }

            for (int i = firstIndex; i <= lastIndex; i++)
            {
                result.Add(_units[i].Item);
            }

            return  result;
        }

        public bool FillWindow(VisualWindow window)
        {
            window.Offset -= _headerSize.Height;
            return _stacks.Min() >= VisualWindowExtension.GetEndOffset(window);
        }
        
    }
}
