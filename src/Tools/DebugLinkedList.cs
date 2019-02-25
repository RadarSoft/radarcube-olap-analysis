using System.Collections.Generic;

namespace RadarSoft.RadarCube.Tools
{
#if DEBUG
    public class DebugLinkedList<T> : LinkedList<T>
    {
        public new void AddLast(T AMember)
        {
            //if (AMember == null)
            //{

            //}
            //else
            base.AddLast(AMember);
        }
    }
#endif
}