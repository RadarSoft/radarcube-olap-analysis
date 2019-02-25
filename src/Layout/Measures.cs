using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using RadarSoft.RadarCube.Controls;
using RadarSoft.RadarCube.Enums;
using RadarSoft.RadarCube.Serialization;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.Layout
{
    /// <summary>A collection of measures that can be displayed in the Grid.</summary>
    /// <moduleiscollection />
    public class Measures : List<Measure>, IStreamedObject
    {
        internal OlapControl FGrid;
#if DEBUG
        internal Level FLevel { get; set; }

        public new void Add(Measure item)
        {
            base.Add(item);
        }
#else
        internal Level FLevel;
#endif


        private MemoryStream membersstream;

        [OnSerializing]
        private void SerializeMembers(StreamingContext context)
        {
            membersstream = new MemoryStream();
            var writer = new BinaryWriter(membersstream);
            WriteStream(writer);
        }

        [OnSerialized]
        private void SerializeMembersEnd(StreamingContext context)
        {
            membersstream = null;
        }

        internal void WriteStream(BinaryWriter stream)
        {
            if (FLevel == null)
            {
                StreamUtils.WriteInt32(stream, 0);
                return;
            }
            StreamUtils.WriteInt32(stream, FLevel.FUniqueNamesArray.Count);
            for (var j = 0; j < FLevel.FUniqueNamesArray.Count; j++)
            {
                var m = FLevel.FUniqueNamesArray.Values[j];
                m.WriteStream(stream);
            }
            DoMembersWriteStream(stream, FLevel.Members);
        }

        private void DoMembersWriteStream(BinaryWriter stream, Members members)
        {
            StreamUtils.WriteTag(stream, Tags.tgHierarchy_ChildrenCount);
            StreamUtils.WriteInt32(stream, members.Count);
            for (var i = 0; i < members.Count; i++)
            {
                var m = members[i];
                var index = FLevel.FUniqueNamesArray.IndexOfKey(m.UniqueName);
                StreamUtils.WriteInt32(stream, index);
                var b = true;
                if (m.Children.Count > 0)
                {
                    b = false;
                    DoMembersWriteStream(stream, m.Children);
                }
                if (b)
                    StreamUtils.WriteTag(stream, Tags.tgHierarchy_LeafMember);
            }
        }

        internal void DoMembersReadStream(BinaryReader stream, Members members, Member parent)
        {
            var memberscount = StreamUtils.ReadInt32(stream);
            members.Capacity = memberscount;
            for (var i = 0; i < memberscount; i++)
            {
                var memberindex = StreamUtils.ReadInt32(stream);
                var m = FLevel.FUniqueNamesArray.Values[memberindex];
                m.FParent = parent;
                m.FDepth = parent == null || parent.FLevel != m.FLevel ? 0 : parent.FDepth + 1;
                members.Add(m);
                var Tag = StreamUtils.ReadTag(stream);
                switch (Tag)
                {
                    case Tags.tgHierarchy_LeafMember:
                        break;
                    case Tags.tgHierarchy_ChildrenCount:
                        DoMembersReadStream(stream, m.Children, m);
                        break;
                }
            }
        }

        internal void ReadStream(BinaryReader stream)
        {
            var memberscount = StreamUtils.ReadInt32(stream);
            if (memberscount == 0)
                return;
            if (FLevel == null)
                FLevel = new Level(null, this);
            FLevel.FMembers = new Members();
            FLevel.FUniqueNamesArray = FLevel.CreateSortedList(memberscount);
            for (var j = 0; j < memberscount; j++)
            {
                var Tag = StreamUtils.ReadTag(stream);
                if (Tag != Tags.tgMember)
                    throw new Exception("Unknown tag: " + Tag);
                var m = new Member(FLevel);
                m.ReadStream(stream);
                FLevel.FUniqueNamesArray.Add(m.UniqueName, m);
            }

            StreamUtils.ReadTag(stream); // Maybe Int32 
            DoMembersReadStream(stream, FLevel.Members, null);
        }

        internal void RestoreAfterSerialization(OlapControl grid)
        {
            ClearCache();
            FGrid = grid;
            foreach (var m in this)
                m.RestoreAfterSerialization(grid);
            if (FLevel != null)
                FLevel.RestoreAfterSerialization(grid);
        }

        internal void InitMeasures()
        {
            _Finded = null;
            if (FLevel != null)
                return;

            FGrid.EventInitMeasures();

#warning TODO(PPTI) after olapcube.RereadOLAPCubeData() FGrid.fMeasures.Count == 0
            if (FGrid.fMeasures.Count == 0)
                FGrid.UpdateMeasures();

            FGrid.EventInitMeasures();
            FLevel = new Level(null, null, this);
            FLevel.fIndex = 0;
        }

        internal void GetVisibleMeasures(List<Measure> AList)
        {
            AList.Clear();
            AList.AddRange(this.Where(x => x.Visible));
        }

        internal void CheckConsistence()
        {
            var fCorrupted = false;
            for (var i = Count - 1; i >= 0; i--)
            {
                var M = this[i];
                if (M.FCubeMeasure == null && M.FFunction != OlapFunction.stCalculated) fCorrupted = true;
            }
            for (var i = 0; i < Grid.Cube.Measures.Count; i++)
            {
                var cm = Grid.Cube.Measures[i];
                var M = Find(cm.UniqueName);
                if (M == null)
                {
                    fCorrupted = true;
                    M = new Measure(FGrid);
                    Insert(i, M);
                    M.InitMeasure(cm);
                }
                else
                {
                    if (IndexOf(M) != i)
                    {
                        Remove(M);
                        Insert(i, M);
                    }
                }
            }
            if (fCorrupted)
            {
                FGrid.Engine.Clear();
                FLevel = null;
                InitMeasures();
            }
        }

        /// <summary>
        ///     Adds a calculated measure of the third type to the collection of Grid
        ///     measures.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         MSAS version supports two ways of calculating measures, added by this
        ///         method:
        ///     </para>
        ///     <list type="bullet">
        ///         <item></item>
        ///         <item>
        ///             With an MDX expression (if an MDX formula for calculating measure is
        ///             specified by the Measure.Expression property).
        ///         </item>
        ///         <item>
        ///             With the OlapGrid.OnCalcMember event handler (if the Expression property
        ///             for this measure is not specified)
        ///         </item>
        ///     </list>
        ///     <para>The Direct version supports only the latter.</para>
        /// </remarks>
        /// <example>
        ///     <para>
        ///         For example, add a calculated measure which strikes an average cost of solved
        ///         goods. The total sales is a "Warehouse Sales" measure value, and the quantity of
        ///         sales is a "Sales Count" measure value.
        ///     </para>
        ///     <para>Assign the OlapGrid.OnCalcMember event handler.</para>
        ///     <para></para>
        ///     <para>
        ///         After that we can create a calculated measure by using the
        ///         Measures.AddCalculatedMeasure method:
        ///     </para>
        ///     <para>GridDSN.Measures.AddCalculatedMeasure("Sales Average");</para>
        ///     <code lang="CS" title="[New Example]">
        /// private void CalculatingSalesAverage(object sender, CalcMemberArgs e)
        /// {
        ///     if (e.CurrentAddress.Measure.DisplayName == "Sales Average")
        ///     {
        ///         ICubeAddress a = e.CurrentAddress;
        ///         a.Measure = this.Grid.Measures.FindByDisplayName("Sales Count");
        ///         int v1 = Convert.ToInt32(e.Evaluator.GetValue(a));
        ///         if (v1 == 0)
        ///         {
        ///             e.ReturnData = null;
        ///             e.ReturnValue = "N/A";
        ///         }
        ///         else
        ///         {
        ///             a.Measure = this.Grid.Measures.FindByDisplayName("Warehouse Sales");
        ///             double v2 = Convert.ToDouble(e.Evaluator.GetValue(a));
        ///             if (v2 == 0)
        ///             {
        ///                 e.ReturnData = null;
        ///                 e.ReturnValue = "N/A";
        ///             }
        ///             else
        ///             {
        ///                 e.ReturnData = v2 / ((double) v1);
        ///                 e.ReturnValue = e.Evaluator.Format(e.ReturnData, "Currency");
        ///             }
        ///         }
        ///     }
        /// }
        /// </code>
        ///     <code lang="VB" title="[New Example]">
        /// Private Sub CalculatingSalesAverage(ByVal sender As Object, ByVal e As CalcMemberArgs)
        ///     If (e.CurrentAddress.Measure.DisplayName Is "Sales Average") Then
        ///         Dim a As ICubeAddress = e.CurrentAddress
        ///         a.Measure = Me.Grid.Measures.FindByDisplayName("Sales Count")
        ///         Dim v1 As Integer = Convert.ToInt32(e.Evaluator.GetValue(a))
        ///         If (v1 = 0) Then
        ///             e.ReturnData = Nothing
        ///             e.ReturnValue = "N/A"
        ///         Else
        ///             a.Measure = Me.Grid.Measures.FindByDisplayName("Warehouse Sales")
        ///             Dim v2 As Double = Convert.ToDouble(e.Evaluator.GetValue(a))
        ///             If (v2 = 0) Then
        ///                 e.ReturnData = Nothing
        ///                 e.ReturnValue = "N/A"
        ///             Else
        ///                 e.ReturnData = (v2 / CDbl(v1))
        ///                 e.ReturnValue = e.Evaluator.Format(e.ReturnData, "Currency")
        ///             End If
        ///         End If
        ///     End If
        /// End Sub
        /// </code>
        /// </example>
        /// <param name="DisplayName">measure caption</param>
        /// <param name="Description">measure description</param>
        /// <param name="DisplayFolder">subfolder in Cube Structure tree where the specified measure is to be placed</param>
        /// <param name="UniqueName">a unique string identifier</param>
        /// <param name="Visible">defines whether the measure will be displayed in the Grid right after its creation</param>
        public Measure AddCalculatedMeasure(string DisplayName, string Description,
            string DisplayFolder, string UniqueName, bool Visible)
        {
            if (FLevel == null && Grid.Active)
                throw new InvalidOperationException();
            var Result = FindByDisplayName(DisplayName);
            if (Result != null)
            {
                if (Result.FFunction != OlapFunction.stCalculated)
                    throw new Exception(string.Format(
                        "The Grid already has an ordinary measure with the same display name ({0}).", DisplayName));
                Result.Visible = Visible;
                return Result;
            }
            Result = new Measure(FGrid);
            Add(Result);
            Result.FDisplayName = DisplayName;
            Result.FDisplayFolder = DisplayFolder;
            Result.FDescription = Description;
            Result.UniqueName = UniqueName != "" ? UniqueName : "[Measures].[" + DisplayName + "]";
            Result.FVisible = Visible;
            Result.FFunction = OlapFunction.stCalculated;
            if (FLevel == null) // return Result;
                return Result;
            Member M = new MemberWrapper(FLevel, null, null, Result);
            M.FDescription = Description;
            M.FMemberType = MemberType.mtMeasure;
            M.FVisible = Visible;
            M.fVirtualID = IndexOf(Result);
            FLevel.Members.Add(M);
            FLevel.FUniqueNamesArray.Add(M.UniqueName, M);
            for (var j = 0; j < Result.ShowModes.Count; j++)
            {
                var M1 = new Member(FLevel, null, null);
                var sm = Result.ShowModes[j];
                M1.DisplayName = sm.Caption;
                M1.SetUniqueName(Guid.NewGuid().ToString());
                M1.FDescription = "";
                M1.FMemberType = MemberType.mtMeasureMode;
                FLevel.FUniqueNamesArray.Add(M1.UniqueName, M1);
                M1.FVisible = sm.Visible;
                M1.fVirtualID = j;
                M.Children.Add(M1);
                M1.FParent = M;
                M1.FDepth = 1;
            }
            Grid.EndChange(GridEventType.geChangeCubeStructure);
            if (Visible && Grid.Active)
                Grid.CellSet.Rebuild();
            return Result;
        }

        /// <summary>
        ///     Adds a calculated measure of the third type to the collection of Grid
        ///     measures
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         MSAS version supports two ways of calculating measures, added by this
        ///         method:
        ///     </para>
        ///     <list type="bullet">
        ///         <item></item>
        ///         <item>
        ///             With an MDX expression (if an MDX formula for calculating measure is
        ///             specified by the Measure.Expression property).
        ///         </item>
        ///         <item>
        ///             With the OlapGrid.OnCalcMember event handler (if the Expression property
        ///             for this measure is not specified)
        ///         </item>
        ///     </list>
        ///     <para>The Direct version supports only the latter.</para>
        /// </remarks>
        public Measure AddCalculatedMeasure(string DisplayName)
        {
            return AddCalculatedMeasure(DisplayName, "", "", "", false);
        }

        /// <summary>
        ///     Deletes the specified third-type measure from the list of available Grid
        ///     measures.
        /// </summary>
        public void DeleteCalculatedMeasure(Measure Measure)
        {
            ClearCache();
            if (FLevel == null && Grid.Active)
                throw new InvalidOperationException();
            if (Measure.FFunction != OlapFunction.stCalculated)
                throw new Exception(string.Format(RadarUtils.GetResStr("rsTryToDeleteWrongMeasure"),
                    Measure.FDisplayName));
            Measure.Visible = false;
            var Idx = IndexOf(Measure);
            if (FLevel == null) // return;
            {
                // if (Grid.Active) throw exception
                //
                //FGrid.Engine.ClearMeasureData(Measure);
                //Remove(Measure);
                //Grid.EndChange(GridEventType.geChangeCubeStructure, new object[0]);
                //if (Grid.Active) Grid.FCellSet.Rebuild();
                Remove(Measure);
                return;
            }

            FGrid.Engine.ClearMeasureData(Measure);

            var P = new List<Member>();
            Member M = null;
            for (var i = 0; i < FLevel.Members.Count; i++)
            {
                if (FLevel.Members[i].fVirtualID == Idx) M = FLevel.Members[i];
                if (FLevel.Members[i].fVirtualID > Idx) P.Add(FLevel.Members[i]);
            }
            FLevel.FUniqueNamesArray.Remove(M.UniqueName);
            FLevel.Members.Remove(M);

            foreach (var m in P)
                m.fVirtualID--;
            Remove(Measure);
            Grid.EndChange(GridEventType.geChangeCubeStructure);
            if (Grid.Active && M.Visible) Grid.FCellSet.Rebuild();
        }

        /// <summary>
        ///     A level containing Member objects that represent measures and measure show
        ///     modes.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Through the Members collection of this object, you can manipulate the visual
        ///         order of measures in the Grid.
        ///     </para>
        ///     <para>
        ///         Each of the elements in this collection has a unique name, the same as the
        ///         name of the appropriate measure. There are as many members in this collection as
        ///         there are measures in the Grid.
        ///     </para>
        ///     <para>
        ///         To relocate an element within the collection, use the MoveMember
        ///         method.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     For example, to make the Sales Amount measure the first to be displayed, you need
        ///     to fulfill the following code:
        ///     <code lang="CS">
        /// 		<![CDATA[
        /// OlapAnalysis1.Measures.Level.Members.MoveMember(OlapAnalysis1.Measures.Level.FindMember("[Measures].[Sales Amount]"), 0);
        /// OlapAnalysis1.CellSet.Rebuild();]]>
        /// 	</code>
        /// </example>
        public Level Level => FLevel;

        internal Measures(OlapControl AGrid)
        {
            FGrid = AGrid;
        }

        /// <summary>
        ///     References to the instance of the OlapGrid class containing the specified
        ///     measure.
        /// </summary>
        public OlapControl Grid => FGrid;

        private Dictionary<string, Measure> _Finded;

        /// <summary>
        ///     Returns from the collection the object of the Measure type with a unique name
        ///     passed as the parameter or null, if there's no such object in the collection.
        /// </summary>
        public Measure Find(string UniqueName)
        {
            Measure res = null;
            if (_Finded == null)
                _Finded = new Dictionary<string, Measure>();
            if (_Finded.TryGetValue(UniqueName, out res) == false)
            {
                foreach (var m in this)
                    if (m.UniqueName == UniqueName)
                    {
                        res = m;
                        break;
                    }
#if DEBUG
                if (res == null)
                {
                }
#endif
                if (res != null)
                    _Finded.Add(UniqueName, res);
            }
            return res;
        }

        /// <summary>
        ///     Searches for a measure with the specified DisplayName in the collection of the
        ///     Grid measures.
        /// </summary>
        /// <remarks>If that measure is not found, returns null.</remarks>
        public Measure FindByDisplayName(string DisplayName)
        {
            foreach (var m in this)
                if (string.Compare(m.FDisplayName, DisplayName, StringComparison.CurrentCultureIgnoreCase) ==
                    0) return m;
            return null;
        }

        public Measure this[string UniqueName] => Find(UniqueName);

        #region IStreamedObject Members

        void IStreamedObject.WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteList(writer, this);

            StreamUtils.WriteTag(writer, Tags.tgMeasures_MembersStream);
            WriteStream(writer);

            StreamUtils.WriteTag(writer, Tags.tgMeasures_EOT);
        }

        void IStreamedObject.ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgList);
            Clear();
            ClearCache();
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgMeasures_MembersStream:
                        ReadStream(reader);
                        break;
                    case Tags.tgList_Count:
                        var c = StreamUtils.ReadInt32(reader);
                        break;
                    case Tags.tgList_Item:
                        var m = new Measure(FGrid);
                        m.ShowModes.Clear();
                        // BeforeRead
                        StreamUtils.ReadStreamedObject(reader, m);
                        // AfterRead
                        Add(m);
                        break;
                    case Tags.tgList_EOT:
                        break;
                    case Tags.tgMeasures_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        #endregion

        internal void ClearCache()
        {
            _Finded = null;
        }
    }
}