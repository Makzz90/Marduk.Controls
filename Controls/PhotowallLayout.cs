using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Marduk.Controls
{
    public class PhotowallLayout : ILayout
    {
        private double _spacing;
        private double _width;
        private double _offset;
        private int _rowIndex;
        private double _unitSize;
        private bool _lastRowLocked = false;
        
        List<PhotowallUnit> _units;
        private Dictionary<object, PhotowallUnit> _itemUnitMap;
        private int _requestRelayoutIndex = -1;

        private Size _headerSize = new Size(0, 0);
        private Size _footerSize = new Size(0, 0);

        public double Spacing { get { return _spacing; } }
        public double UnitSize { get { return _unitSize; } }

        public double Width { get { return _width; } }

        public Size LayoutSize { get { return new Size(this.Width, (float)(UnitCount == 0 ? 0 : RowCount * (_unitSize + _spacing) - _spacing)); } }

        public int UnitCount { get { return _units.Count; } }

        public Size HeaderSize { get { return _headerSize; } }

        public Size FooterSize { get { return _footerSize; } }

        public int RowCount
        {
            get
            {
                return _rowIndex + 1 - (_lastRowLocked ? 1 : 0);
            }
        }

        public PhotowallLayout(double spacing, double width, double unitSize)
        {
            _spacing = spacing;
            _width = width;
            _unitSize = unitSize;
            _units = new List<PhotowallUnit > ();
            _itemUnitMap = new Dictionary<object, PhotowallUnit> ();
        }
        
        public void Dispose()
        {
            _units=null;
            _itemUnitMap = null;
        }
        
        public void AddItem(int index, object item, Size size)
        {
            size.Height = (float)_unitSize;
            var unit = new PhotowallUnit(item, size);

            _itemUnitMap[item] = unit;

            if (index >= 0 && index < (long)_units.Count)
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

                _units.Add(unit);

                bool isNeedNext = Math.Abs(_width - _offset) > Math.Abs(_width - (_offset + size.Width + (_offset == 0 ? 0 : _spacing)));

                _lastRowLocked = false;
                if (isNeedNext || _offset * 2 < _width)
                {
                    unit.Offset = _offset + (_offset == 0 ? 0 : _spacing);
                    unit.RowIndex = _rowIndex;
                    _offset += size.Width + (_offset == 0 ? 0 : _spacing);
                }
                else
                {
                    _rowIndex++;
                    _offset = 0;

                    unit.Offset = 0;
                    unit.RowIndex = _rowIndex;
                    _offset += size.Width;

                    RelayoutRow(_units.Count - 2);
                }


                if (_offset >= _width)
                {
                    RelayoutRow(_units.Count - 1);
                    _rowIndex++;
                    _offset = 0;
                    _lastRowLocked = true;
                }
            }
        }

        
        public void RelayoutRow(int itemIndex)
        {
            int rowFirstItemIndex = -1;
            int rowLastItemIndex = -1;

            for (int i = itemIndex; i >= 0; i--)
            {
                if (_units[i].RowIndex == _units[itemIndex].RowIndex - 1)
                {
                    rowFirstItemIndex = i + 1;
                    break;
                }
            }

            if (rowFirstItemIndex < 0)
            {
                rowFirstItemIndex = 0;
            }

            for (int i = itemIndex; i < _units.Count; i++)
            {
                if (_units[i].RowIndex == _units[itemIndex].RowIndex + 1)
                {
                    rowLastItemIndex = i - 1;
                    break;
                }
            }

            if (rowLastItemIndex < 0)
            {
                rowLastItemIndex = _units.Count - 1;
            }

            double newOffset = 0;
            double rowLength = _units[rowLastItemIndex].Offset + _units[rowLastItemIndex].DesiredSize.Width;

            for (int i = rowFirstItemIndex; i <= rowLastItemIndex; i++)
            {
                var unit = _units[i];

                double overloadLength = rowLength - (Spacing * (rowLastItemIndex - rowFirstItemIndex));
                double itemLength = _width - (Spacing * (rowLastItemIndex - rowFirstItemIndex));
                double actualWidth = unit.DesiredSize.Width / overloadLength * itemLength;
                actualWidth = (int)(actualWidth + 0.5);

                unit.ActualSize = new Size(actualWidth, unit.DesiredSize.Height);
                unit.ActualOffset = newOffset;
                newOffset += actualWidth + _spacing;
            }

            newOffset -= _spacing;
            if (newOffset != _width)
            {
                _units[rowLastItemIndex].ActualSize = new Size((float)(_width - newOffset + _units[rowLastItemIndex].ActualSize.Width), (float)(_units[rowLastItemIndex].ActualSize.Height));
            }
        }

        
        public IReadOnlyList<object> GetVisableItems(VisualWindow window, ref int firstIndex, ref int lastIndex)
        {
            if (!(_requestRelayoutIndex < 0 || _requestRelayoutIndex >= _units.Count))
            {
                Relayout();
            }

            window.Offset -= _headerSize.Height;
            List < object > result = new List<object>();

            if (_units.Count == 0)
            {
                firstIndex = -1;
                lastIndex = -1;
                return  result;
            }

            int firstRowIndex;
            int lastRowIndex;
            int visableRowCount;
            int newFirstIndex = -1;
            int newLastIndex = -1;

            firstRowIndex = (int)Math.Floor((window.Offset + _spacing) / (_unitSize + _spacing));
            visableRowCount = (int)Math.Floor((window.Length + _spacing) / (_unitSize + _spacing));
            lastRowIndex = (int)Math.Floor((VisualWindowExtension.GetEndOffset(window) + _spacing) / (_unitSize + _spacing));

            int firstRow = 0;
            int lastRow = _units[_units.Count - 1].RowIndex;

            if (lastRow - firstRow + 1 < visableRowCount)
            {
                firstRowIndex = 0;
                lastRowIndex = lastRow;
            }
            else
            {
                if (firstRowIndex > lastRow - visableRowCount + 1)
                {
                    firstRowIndex = lastRow - visableRowCount + 1;
                }

                if (firstRowIndex < 0)
                {
                    firstRowIndex = 0;
                }
            }

            lastRowIndex = firstRowIndex + visableRowCount - 1;

            if (firstRowIndex != 0)
            {

                if (firstIndex < 0)
                {
                    for (int i = 0; i < _units.Count; i++)
                    {
                        if (_units[i].RowIndex == firstRowIndex)
                        {
                            newFirstIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    if (_units[firstIndex].RowIndex < firstRowIndex)
                    {
                        for (int i = firstIndex; i < _units.Count; i++)
                        {
                            if (_units[i].RowIndex == firstRowIndex)
                            {
                                newFirstIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (int i = firstIndex; i >= 0; i--)
                        {
                            if (_units[i].RowIndex == firstRowIndex - 1)
                            {
                                newFirstIndex = i + 1;
                                break;
                            }
                        }
                    }
                }
            }

            if (newFirstIndex < 0)
            {
                newFirstIndex = 0;
            }

            if (lastIndex < 0)
            {
                for (int i = 0; i < _units.Count; i++)
                {
                    if (_units[i].RowIndex == lastRowIndex + 1)
                    {
                        newLastIndex = i - 1;
                        break;
                    }
                }

            }
            else
            {
                if (lastIndex >= _units.Count)
                {
                    lastIndex = _units.Count - 1;
                }

                if (_units[lastIndex].RowIndex < lastRowIndex)
                {
                    for (int i = lastIndex; i < _units.Count; i++)
                    {
                        if (_units[i].RowIndex == lastRowIndex + 1)
                        {
                            newLastIndex = i - 1;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = lastIndex; i >= 0; i--)
                    {
                        if (_units[i].RowIndex == lastRowIndex)
                        {
                            newLastIndex = i;
                            break;
                        }
                    }
                }
            }

            if (newLastIndex < 0)
            {
                newLastIndex = (int)_units.Count - 1;
            }

            if (newLastIndex - newFirstIndex > 200)
            {

            }

            firstIndex = newFirstIndex;
            lastIndex = newLastIndex;

            for (int i = firstIndex; i <= lastIndex; i++)
            {
                result.Add(_units[i].Item);
            }

            return result;
        }

        
        public Rect GetItemLayoutRect(int index)
        {
            if (!(_requestRelayoutIndex < 0 || _requestRelayoutIndex >= (long)_units.Count))
            {
                Relayout();
            }

            Rect result = new Rect();

            var unit = _units[index];

            if (unit.RowIndex == _rowIndex && !_lastRowLocked)
            {
                result.Height = (float)unit.DesiredSize.Height;
                result.Width = (float)unit.DesiredSize.Width;
                result.X = (float)unit.Offset;
                result.Y = (float)(unit.RowIndex * (_unitSize + _spacing) + _headerSize.Height);
            }
            else
            {
                result.Height = double.IsInfinity(unit.ActualSize.Height) ? unit.DesiredSize.Height : unit.ActualSize.Height;
                result.Width = double.IsInfinity(unit.ActualSize.Width) ? unit.DesiredSize.Width : unit.ActualSize.Width;
                result.X = (float)((unit.ActualOffset < 0) ? unit.Offset : unit.ActualOffset);
                result.Y = (float)(unit.RowIndex * (_unitSize + _spacing) + _headerSize.Height);
            }

            return result;
        }

        
        public bool FillWindow(VisualWindow window)
        {
            var lastRowIndex = Math.Floor((VisualWindowExtension.GetEndOffset(window) + _spacing) / (_unitSize + _spacing));

            return _rowIndex > lastRowIndex;
        }

        
        public bool IsItemInWindow(VisualWindow window, int index)
        {
            Rect rect = GetItemLayoutRect(index);
            return VisualWindowExtension.Contain(window, new VisualWindow(){ Offset= rect.Top, Length = rect.Height } );
        }

        
        public void ChangeItem(int index, object item, Size size)
        {
            if (item != null)
            {
                _itemUnitMap.Remove(_units[index].Item);//_itemUnitMap.erase(_units[index].Item);
                _itemUnitMap[item] = _units[index];
                _units[index].Item = item;
            }

            if (size.Width != _units[index].DesiredSize.Width)
            {
                if (_units[index].RowIndex < _rowIndex)
                {
                    int thisRowStartIndex = -1;
                    int nextRowStartIndex = -1;
                    int newNextRowStartIndex = -1;

                    for (int i = index; i >= 0; i--)
                    {
                        if (_units[i].RowIndex == _units[index].RowIndex - 1)
                        {
                            thisRowStartIndex = i + 1;
                            break;
                        }
                    }

                    if (thisRowStartIndex < 0)
                    {
                        thisRowStartIndex = 0;
                    }

                    for (int i = index; i < (long)_units.Count; i++)
                    {
                        if (_units[i].RowIndex == _units[index].RowIndex + 1)
                        {
                            nextRowStartIndex = i;
                            break;
                        }
                    }

                    if (nextRowStartIndex < 0)
                    {
                        throw new Exception( "A catastrophic error occurred.");
                    }

                    _units[index].DesiredSize = new Size(size.Width, _units[index].DesiredSize.Height);

                    double offset = 0;
                    for (int i = thisRowStartIndex; i <= nextRowStartIndex; i++)
                    {
                        bool isNeedNext = Math.Abs(_width - offset) > Math.Abs(_width - (offset + size.Width + (offset == 0 ? 0 : _spacing)));

                        if (isNeedNext || offset * 2 < _width)
                        {
                            offset += _units[i].DesiredSize.Width + (_offset == 0 ? 0 : _spacing);
                        }
                        else
                        {
                            newNextRowStartIndex = i;
                            break;
                        }
                    }

                    if (newNextRowStartIndex != nextRowStartIndex)
                    {
                        SetRelayoutIndex(thisRowStartIndex);
                    }
                    else
                    {
                        RelayoutRow(thisRowStartIndex);
                    }
                }
                else
                {
                    _units[index].DesiredSize = new Size(size.Width, _units[index].DesiredSize.Height);
                    SetRelayoutIndex(index);
                }
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

        
        public void ChangeSpacing(double spacing)
        {
            if (spacing != _spacing)
            {
                _spacing = spacing;
                SetRelayoutIndex(0);
            }
        }

        
        public void RemoveItem(int index)
        {
            SetRelayoutIndex(Math.Max(0, index - 1));
            _itemUnitMap.Remove(_units[index].Item);
            _units.RemoveAt( index);
        }

        
        public void RemoveAll()
        {
            _itemUnitMap.Clear();
            _units.Clear();
            _lastRowLocked = false;
            _rowIndex = 0;
            _offset = 0;
            SetRelayoutIndex(0);
        }

        
        public Size GetItemSize(int index)
        {
            Size result = new Size();
            if (_units[index].RowIndex == _rowIndex && !_lastRowLocked)
            {
                result.Height = _units[index].DesiredSize.Height;
                result.Width = _units[index].DesiredSize.Width;
            }
            else
            {
                result.Height = double.IsInfinity(_units[index].ActualSize.Height) ? _units[index].DesiredSize.Height : _units[index].ActualSize.Height;
                result.Width = double.IsInfinity(_units[index].ActualSize.Width) ? _units[index].DesiredSize.Width : _units[index].ActualSize.Width;
            }

            return result;
        }

        
        public Size GetItemSize(object item)
        {
            Size result = new Size();
            if (_itemUnitMap[item].RowIndex == _rowIndex && !_lastRowLocked)
            {
                result.Height = _itemUnitMap[item].DesiredSize.Height;
                result.Width = _itemUnitMap[item].DesiredSize.Width;
            }
            else
            {
                result.Height = double.IsInfinity(_itemUnitMap[item].ActualSize.Height) ? _itemUnitMap[item].DesiredSize.Height : _itemUnitMap[item].ActualSize.Height;
                result.Width = double.IsInfinity(_itemUnitMap[item].ActualSize.Width) ? _itemUnitMap[item].DesiredSize.Width : _itemUnitMap[item].ActualSize.Width;
            }
            return result;
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

        
        public void ChangeUnitSize(double unitSize)
        {
            if (unitSize != _unitSize)
            {
                _unitSize = unitSize;
                SetRelayoutIndex(0);
            }
        }

        
        public void Relayout()
        {
            int thisRowStartIndex = -1;

            for (int i = _requestRelayoutIndex; i >= 0; i--)
            {
                if (_units[i].RowIndex == _units[_requestRelayoutIndex].RowIndex - 1)
                {
                    thisRowStartIndex = i + 1;
                    break;
                }
            }

            if (thisRowStartIndex < 0)
            {
                thisRowStartIndex = 0;
            }

            _rowIndex = _units[_requestRelayoutIndex].RowIndex;
            _offset = 0;
            List<int> relayoutRows = new List<int>();

            for (int i = thisRowStartIndex; i < (long)_units.Count; i++)
            {
                var unit = _units[i];
                double length = unit.DesiredSize.Width;

                bool isNeedNext = Math.Abs(_width - _offset) > Math.Abs(_width - (_offset + length + (_offset == 0 ? 0 : _spacing)));

                _lastRowLocked = false;
                if (isNeedNext || _offset * 2 < _width)
                {
                    unit.Offset = _offset + (_offset == 0 ? 0 : _spacing);
                    unit.RowIndex = _rowIndex;
                    _offset += length + (_offset == 0 ? 0 : _spacing);
                }
                else
                {
                    _rowIndex++;
                    _offset = 0;

                    unit.Offset = 0;
                    unit.RowIndex = _rowIndex;
                    _offset += length;

                    relayoutRows.Add(i - 1);
                }

                if (_offset >= _width)
                {
                    relayoutRows.Add(i);
                    _rowIndex++;
                    _offset = 0;
                    _lastRowLocked = true;
                }
            }

            foreach (var row in relayoutRows)
            {
                RelayoutRow(row);
            }

            relayoutRows = null;
            _requestRelayoutIndex = -1;
        }

        
        public Size GetHeaderAvailableSize()
        {
            return new Size((float)Width, double.PositiveInfinity);
        }

        
        public Size GetFooterAvailableSize()
        {
            return new Size((float)(Width - _offset + Spacing), (float)UnitSize);
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

        
        public bool SetFooterSize(Size size)
        {
            if (size.Width != _footerSize.Width || size.Height != _footerSize.Height)
            {
                _footerSize = size;
                return true;
            }
            return false;
        }

        
        public Rect GetHeaderLayoutRect()
        {
            return new Rect(0, 0, _headerSize.Width, _headerSize.Height);
        }

        
        public Rect GetFooterLayoutRect()
        {
            return new Rect(0, 0, 0, 0);
        }
    }
}
