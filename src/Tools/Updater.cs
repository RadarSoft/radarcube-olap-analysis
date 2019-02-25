using System;

namespace RadarSoft.RadarCube.Tools
{
    internal class Updater
    {
        private readonly object _Parent;

        public Updater(object AParent)
        {
            _Parent = AParent;
            ResetUpdate();
        }

        public Updater()
            : this(null)
        {
        }

        public bool IsBusy => UpdateCount > 0;

        public int Depth => Math.Max(UpdateCount, 0);
        public int UpdateCount { get; private set; }

        internal static Updater CreateByEvent(EventHandler endupdate)
        {
            var res = new Updater();
            res.UpdateEnd += endupdate;
            return res;
        }

        public void ResetUpdate()
        {
            UpdateCount = 0;
        }

        public void BeginUpdate()
        {
            OnUpdate(this, EventArgs.Empty);
#if DEBUG
            //var list = _Parent as TAPSIMPLEList;
            var id = "null";
            //if (list != null)
            //{
            //    id = "ListType=" + list.ListType.ToString();
            //}

            DebugLogging.WriteLine("Updater" + new string(' ', Depth) +
                                   string.Format("BeginUpdate({0}) Depth={1}", id, UpdateCount));
#endif
            UpdateCount++;
        }

        public void EndUpdate()
        {
            if (--UpdateCount == 0)
                OnUpdateEnd(this, EventArgs.Empty);
            OnUpdate(this, EventArgs.Empty);
#if DEBUG
            //var list = _Parent as TAPSIMPLEList;
            var id = "null";
            //if (list != null)
            //{
            //    id = "ListType=" + list.ListType.ToString();
            //    if (list.ListType == TPivotPanelType.pptCol && _UpdateCount == 0)
            //    {
            //    }
            //}

            DebugLogging.WriteLine("Updater" + new string(' ', Depth) +
                                   string.Format("EndUpdate({0}) Depth={1}", id, UpdateCount));

            if (UpdateCount < 0)
                throw new ArgumentOutOfRangeException("_UpdateCount < 0!");
#endif
        }

        public event EventHandler UpdateEnd;
        public event EventHandler Update;

        protected virtual void OnUpdateEnd(object sender, EventArgs e)
        {
            if (UpdateEnd != null)
                UpdateEnd(_Parent, e);
        }

        protected virtual void OnUpdate(object sender, EventArgs e)
        {
            if (Update != null)
                Update(_Parent, e);
        }
    }
}