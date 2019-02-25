using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RadarSoft.RadarCube.Tools
{
    public class TObservableCollection<T> : ObservableCollection<T>
    {
#if DEBUG

        //public event NotifyCollectionChangedEventHandler CollectionChanged;

        public new void Add(T item)
        {
            base.Add(item);

            //if (CollectionChanged != null)
            //    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new[] { item }));
        }

        public new void Insert(int index, T item)
        {
            base.Insert(index, item);

            //if (CollectionChanged != null)
            //    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
        }

        public new bool Remove(T item)
        {
            var res = base.Remove(item);

            return res;
        }

        internal IList<T> AsReadOnly()
        {
            return this.Select(x => x).ToList();
        }
#endif
    }
}