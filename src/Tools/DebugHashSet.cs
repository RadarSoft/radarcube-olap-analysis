using System.Collections.Generic;
using System.Linq;
using RadarSoft.RadarCube.CellSet;

namespace RadarSoft.RadarCube.Tools
{
#if DEBUG
    internal class DebugHashSet<T> : HashSet<T>
    {
        private readonly string p;

        public DebugHashSet()
        {
        }

        public DebugHashSet(string p)
            : this()
        {
            this.p = p;
        }

        public new bool Add(T item)
        {
            //DebugLogging.WriteLine("Drill.DebugHashSet{0}.Add(Item={1})", p, item);

            if (p != null)
            {
            }
            if (Contains(item))
            {
            }
            var it = item as DrillAction;
            if (it != null)
            {
                var M = this.Cast<DrillAction>().FirstOrDefault(x => x.ParentMember == it.ParentMember);
                if (M != null)
                {
                }
            }

            return base.Add(item);
        }

        public new bool Remove(T item)
        {
            //DebugLogging.WriteLine("Drill.DebugHashSet{0}.Remove(Item={1})", p, item);

            if (p != null)
            {
            }

            return base.Remove(item);
        }

        public new void Clear()
        {
            if (p != null)
            {
            }

            base.Clear();
        }
    }
#endif
}