namespace Marduk.Controls
{
    public class VisualWindowExtension
    {
        public static double GetEndOffset(VisualWindow window)
        {
            return window.Offset + window.Length;
        }
        
        public static bool Contain(VisualWindow window, VisualWindow otherWindow)
        {
            return window.Offset <= otherWindow.Offset && GetEndOffset(window) >= GetEndOffset(otherWindow);
        }

        /*
        public bool GetIntersection(VisualWindow window, VisualWindow otherWindow, VisualWindow intersectionWindow)
        {
            bool flagEndOffset = GetEndOffset(otherWindow) <= GetEndOffset(window) && GetEndOffset(otherWindow) >= window.Offset;
            bool flagOffset = otherWindow.Offset <= GetEndOffset(window) && otherWindow.Offset >= window.Offset;

            if (!flagOffset && !flagEndOffset)
            {
                if (GetEndOffset(otherWindow) >= GetEndOffset(window) && otherWindow.Offset <= window.Offset)
                {
                    intersectionWindow.Offset = window.Offset;
                    intersectionWindow.Length = window.Length;
                    return true;
                }
                return false;
            }

            if (flagOffset && flagEndOffset)
            {
                intersectionWindow.Offset = otherWindow.Offset;
                intersectionWindow.Length = window.Length;
                return true;
            }

            if (flagOffset)
            {
                intersectionWindow.Offset = otherWindow.Offset;
                intersectionWindow.Length = GetEndOffset(window) - otherWindow.Offset;
                return true;
            }

            if (flagEndOffset)
            {
                intersectionWindow.Offset = window.Offset;
                intersectionWindow.Length = GetEndOffset(otherWindow) - window.Offset;
                return true;
            }
            return false;
        }

        
        public bool GetUnion(VisualWindow window, VisualWindow otherWindow, VisualWindow unionWindow)
        {
            bool flagEndOffset = GetEndOffset(otherWindow) <= GetEndOffset(window) && GetEndOffset(otherWindow) >= window.Offset;
            bool flagOffset = otherWindow.Offset <= GetEndOffset(window) && otherWindow.Offset >= window.Offset;

            if (!flagOffset && !flagEndOffset)
            {
                if (GetEndOffset(otherWindow) >= GetEndOffset(window) && otherWindow.Offset <= window.Offset)
                {
                    unionWindow.Offset = otherWindow.Offset;
                    unionWindow.Length = window.Length;
                    return true;
                }
                return false;
            }

            if (flagOffset && flagEndOffset)
            {
                unionWindow.Offset = window.Offset;
                unionWindow.Length = window.Length;
                return true;
            }

            if (flagOffset)
            {
                unionWindow.Offset = window.Offset;
                unionWindow.Length = GetEndOffset(otherWindow) - window.Offset;
                return true;
            }

            if (flagEndOffset)
            {
                unionWindow.Offset = otherWindow.Offset;
                unionWindow.Length = GetEndOffset(window) - otherWindow.Offset;
                return true;
            }
            return false;
        }
       

        public bool IsEmpty(VisualWindow window)
        {
            return window.Offset == 0 && window.Length == 0;
        }
         */
    }

}