using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.CellSet
{
    /// <summary>
    ///     The class realizing the Undo and Redo methods for the Grid and Graph.
    /// </summary>
    public class HistoryManager
    {
        private bool _Enabled;

        private MemoryStream current = new MemoryStream();
        private OlapControl fGrid;

        private bool IsHistoryAction;
        private readonly Stack<MemoryStream> redoList = new Stack<MemoryStream>();

        private readonly Stack<MemoryStream> undoList = new Stack<MemoryStream>();

        internal HistoryManager(OlapControl grid)
        {
            fGrid = grid;

            Updater = new Updater(this);
            Updater.UpdateEnd += Updater_UpdateEnd;
        }

        /// <summary>
        ///     Indicates if the Undo operation is avaliable.
        /// </summary>
        public bool IsUndoAllowed => undoList.Count > 0;

        /// <summary>
        ///     Indicates if the Redo operation is avaliable.
        /// </summary>
        public bool IsRedoAllowed => redoList.Count > 0;

        [DefaultValue(true)]
        public bool Enabled
        {
            get => _Enabled;
            set
            {
                _Enabled = value;

                if (value == false)
                    ClearHistory();
            }
        }

        internal Updater Updater { get; private set; }

        private void Updater_UpdateEnd(object sender, EventArgs e)
        {
            if (IsActionEnabled())
                DoAction_Inner();
        }

        /// <summary>
        ///     Performs the Undo operation.
        /// </summary>
        public void Undo()
        {
            if (undoList.Count == 0) return;
            var m = undoList.Pop();
            IsHistoryAction = true;

            fGrid.Serializer.ReadXML(m);

            if (current.Length > 0)
                redoList.Push(current);

            current = m;
            fGrid.EndChange(GridEventType.geEndUpdate);
            IsHistoryAction = false;

            DebugLogging.WriteLine("HistoryManager.Undo(undoList={0})", undoList.Count);
        }

        /// <summary>
        ///     Performs the Redo operation.
        /// </summary>
        public void Redo()
        {
            if (redoList.Count < 1) return;
            var m = redoList.Pop();
            IsHistoryAction = true;
            fGrid.Serializer.ReadXML(m);
            if (current.Length > 0) undoList.Push(current);
            current = m;
            fGrid.EndChange(GridEventType.geEndUpdate);
            IsHistoryAction = false;

            DebugLogging.WriteLine("HistoryManager.Redo(undoList={0})", undoList.Count);
        }

        /// <summary>
        ///     Clears up the stored operations history.
        /// </summary>
        public void ClearHistory()
        {
            undoList.Clear();
            redoList.Clear();
            current = new MemoryStream();
            IsHistoryAction = true;
            fGrid.EndChange(GridEventType.geEndUpdate);
            IsHistoryAction = false;

            _Enabled = true;
        }

        internal void DoAction()
        {
            if (IsActionEnabled() == false)
                return;

            Updater.BeginUpdate();
            Updater.EndUpdate();
        }

        private bool IsActionEnabled()
        {
            if (fGrid == null) return false;
            if (IsHistoryAction) return false;
            if (Enabled == false) return false;

            if (!fGrid.Active)
            {
                ClearHistory();
                return false;
            }
            if (fGrid.IsUpdating)
                return false;

            return true;
        }

        private void DoAction_Inner()
        {
            var m = new MemoryStream();

            fGrid.Serializer.WriteXML(m);

            if (current.Length > 0)
            {
                if (RadarUtils.IsStreamEquals(current, m))
                {
                    DebugLogging.WriteLine("HistoryManager.DoAction_Inner(ALREADTY EXIST! not ADDED!!!)");
                    return;
                }

                undoList.Push(current);
                DebugLogging.WriteLine("HistoryManager.DoAction_Inner(undoList={0})", undoList.Count);
            }
            current = m;

            redoList.Clear();
        }

        internal void Close()
        {
            fGrid = null;
            Updater.UpdateEnd -= Updater_UpdateEnd;
            Updater = null;
        }
    }
}