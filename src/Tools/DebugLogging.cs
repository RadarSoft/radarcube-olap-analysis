using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RadarSoft.RadarCube.Tools
{
    public class DebugLogging
    {
        private const int VK_CAPITAL = 0x14;

        private static readonly string[] _StartedMessage =
        {
            //"HistoryManager",
            //"DockingManager",
            //"DockAnalysis",
            //"MOlapCube",
            //"TMDEngine",
            //"Line",
            //"TMDLine",
            //"MetaLine",
            //"TMDMetaLine",
            //"Drill",

            //"TOLAPChartObsolete",
            //"OlapAnalysis",

            //"TIndexPage",
            //"TIndexTree",
            //"TDataIndex",
            //"TPageItem",
            //"OlapAxisLayoutSerializer",
            //"TPlotSeriesesPie",
            //"Page.Cache",
            //"TChartsInternalGrid",
            //"TPlotSeriesesBar",
            //"VM_ConnectionEditor",
            //"MDConnectionEditor",
            //"Updater",
            //"TDataTable",

            //"TButton",

            //"TChartSettingLayer",
            //"TChartSettingElementT",
            //"TChartSettingSeries",

            //"LayoutAnchorable",
            //"DirectConverter",
            //"TPTDrillButtonsT",
            //"TPTCell",

            //"CubeEngine",

            //"Drill",
            //"TScrollablePivotList",
            //"TExportOptions",
            //"TAPSIMPLEList",
            //"DragHandlerBase",
            //"DockPane",
            //"DockPanel",

            //"TToolbox",
            //"WaitWindowManager",

            //"OlapControl", 
            //"OlapControl.ReadURCF",   
            //"OlapControl",
            //"TOLAPControlGeneral",
            //"OlapGrid",
            //"OlapChart",

            //"OLAPSlice",

            //"SliceBase",
            //"SliceChart",
            //"SliceGrid",
            //"SliceAnalysis",
            //"TInternalGrid",

            //"MOlapCube",
            //"TMDLine",
            //"TMDMetaLine",

            //"RadarCube",
            //"Cell",
            //"CellSet",
            //"ChartCellSet",

            //"Members"
            //"TEvalutor"            
            //"TInternalGridGeneral",
            //"TInternalGrid.ExcludePageCell",
            //"TPivotManagerT",

            //"TDrawRouting",
            //"TDrawDataBase",

            //"AxisGrid",
            //"ChartDocking",
            //"TChartsInternalGrid",
            //"TInternalGrid",               
            //"TInternalGrid.",
            //"ContentPanelArea",
            //"TMeasureLegend",
            //"TComboBox",
            //"ImageButton",
            //"TBrushSelector"
            //"CellsetLevel",
            //"TFormulaEditor",
            //"TOLAPCube",
            //"TList",
            //"PageCell",
            //"TTextBox",
            //"CellsetMember",

            //"TDashDockPanel",
            //"TAPSIMPLEList",
            //"OlapAxisLayoutSerializer",

            "OlapControl",
            "Engine",
            "TDataTable",
            "MOlapCube",

            "_DummyItem_" // its fake element but now "," after every item now ))
        };

        [Conditional("DEBUG")]
        internal static void Write(string format, params object[] args)
        {
            if (Verify(format))
                return;

            Debug.Write(string.Format(format, args));
        }

        internal static bool Verify(string format)
        {
            if (VerifyName(format) == false)
                return true;

            if (VerifyKey())
                return true;

            return false;
        }

        private static bool VerifyName(string format)
        {
            if (_StartedMessage.Length == 0)
                return false;

            return _StartedMessage
                .Where(startstring => !string.IsNullOrEmpty(format))
                .Any(startstring => format.Contains(startstring + "."));
        }

        [Conditional("DEBUG")]
        internal static void WriteLine(string format, params object[] args)
        {
            if (Verify(format))
                return;

            var s = string.Format(format, args);
            Debug.WriteLine(s);
        }

        [DllImport("User32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private static bool VerifyKey()
        {
            var isOn = (GetKeyState(VK_CAPITAL) & 1) > 0;
            return isOn;
        }

        [Conditional("DEBUG")]
        internal static void WriteLineIndent(string format, int FUpdateCounter)
        {
            WriteLine(string.Format(new string(' ', FUpdateCounter) + format, FUpdateCounter));
        }

        [Conditional("DEBUG")]
        internal static void WriteLineException(string p, Exception ex)
        {
            WriteLine(p);
            WriteLine(GetExceptionText(ex));
        }

        internal static string GetExceptionText(Exception e)
        {
            var sb = new StringBuilder();
            AddExceptionData(sb, e);
            return sb.ToString();
        }

        private static void AddExceptionData(StringBuilder sb, Exception e)
        {
            sb.AppendFormat("\nException happen\n Message: {0}", e.Message);
            sb.AppendFormat("\nStackTrace:\n{0}", e.StackTrace);

            if (e.InnerException != null)
                AddExceptionData(sb, e.InnerException);
        }

        internal static void DockingWriteLine(string p, params object[] args)
        {
            //Debug.WriteLine(p, args);
        }

        internal static void DockingWrite(string p, string args)
        {
            //Debug.Write(p, args);
        }

        internal static void DockingWrite(string p)
        {
            DockingWrite(p, null);
        }

        internal static object ToString(object arg)
        {
            return arg == null ? "{x:null}" : arg;
        }
    }
}