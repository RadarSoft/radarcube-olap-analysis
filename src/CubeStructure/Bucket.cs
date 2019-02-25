using System;
using System.Diagnostics;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.CubeStructure
{
    [DebuggerDisplay("Bucket Min={Min} Max={Max} Index={Index}")]
    internal class Bucket
    {
        internal const string __FIRST = "First";
        internal const string __LAST = "Last";
        internal const string __PREVIOUS_FIRST = "Previous first";
        internal const string __PREVIOUS_LAST = "Previous last";
        internal const string __NEXT_FIRST = "Next first";
        internal const string __NEXT_LAST = "Next last";
        internal const string __MIN = "Min";
        internal const string __MAX = "Max";
        internal const string __PREVIOUS_MIN = "Previous Min";
        internal const string __PREVIOUS_MAX = "Previous Max";
        internal const string __NEXT_MIN = "Next Min";
        internal const string __NEXT_MAX = "Next Max";
        internal const string __INDEX = "Index";
        internal BucketCollection fBucketCollection;
        internal string fDisplayName = null;
        internal int fIndex;
        internal double fMaxValue;
        internal double fMinValue;

        internal Bucket(BucketCollection BucketCollection)
        {
            fBucketCollection = BucketCollection;
        }

        public int Index => fIndex;

        public double Min => fMinValue;

        public double Max => fMaxValue;

        internal string GetBucketName(string FormatString)
        {
            int Index = 0, Index1 = 0, Index2 = 0;
            double Value = 0;
            Bucket NextBucket;
            var ValueString = FormatString;
            //begin
            var Result = FormatString;
            var OperatorText = ExtractOperator(Result, Index1, Index2);
            while (OperatorText.IsFill())
            {
                if (WideSameText(OperatorText, __FIRST) || WideSameText(OperatorText, __MIN))
                {
                    Value = fMinValue;
                }
                else if (WideSameText(OperatorText, __LAST) || WideSameText(OperatorText, __MAX))
                {
                    Value = fMaxValue;
                }
                else if (WideSameText(OperatorText, __INDEX))
                {
                    Value = Index;
                }
                else if (WideSameText(OperatorText, __PREVIOUS_LAST) || WideSameText(OperatorText, __PREVIOUS_MAX))
                {
                    if (Index == 0)
                        throw new Exception(string.Format(RadarUtils.GetResStr("rsecCantApplyOperatorToFirst"),
                            OperatorText));

                    NextBucket = fBucketCollection[Index - 1];
                    Value = NextBucket.fMaxValue;
                }
                else if (WideSameText(OperatorText, __NEXT_FIRST) || WideSameText(OperatorText, __NEXT_MIN))
                {
                    if (Index == fBucketCollection.Count - 1)
                        throw new Exception(string.Format(RadarUtils.GetResStr("rsecCantApplyOperatorToLast"),
                            OperatorText));

                    NextBucket = fBucketCollection[Index + 1];
                    Value = NextBucket.fMinValue;
                }
                else if (WideSameText(OperatorText, __PREVIOUS_MIN))
                {
                    if (Index == 0)
                        throw new Exception(string.Format(RadarUtils.GetResStr("rsecCantApplyOperatorToFirst"),
                            OperatorText));

                    NextBucket = fBucketCollection[Index - 1];
                    Value = NextBucket.fMinValue;
                }
                else if (WideSameText(OperatorText, __NEXT_MAX))
                {
                    if (Index == fBucketCollection.Count - 1)
                        throw new Exception(string.Format(RadarUtils.GetResStr("rsecCantApplyOperatorToLast"),
                            OperatorText));
                    NextBucket = fBucketCollection[Index + 1];
                    Value = NextBucket.fMaxValue;
                }
                else
                {
                    throw new Exception(string.Format(RadarUtils.GetResStr("rsecUnknownBucketOperator"), OperatorText));
                }

                ValueString = ValueString.Replace(RadarUtils.GetResStr("rsOperatorSign") + "{" + OperatorText + "}",
                    Value.ToString());
                Result = Result.Substring(0, Index1) + ValueString;

                OperatorText = ExtractOperator(Result, Index1, Index2);
            }
            return Result;
        }

        private bool WideSameText(string o1, string o2)
        {
            return o1 == o2;
        }

        internal static string ExtractOperator(string S, int Index1, int Index2)
        {
            var rsOperatorSign = RadarUtils.GetResStr("rsOperatorSign");
            var StartPos = S.IndexOf(rsOperatorSign);

            if (StartPos == -1)
                return null;

            if (S[StartPos + 1] != '{')
                throw new Exception(RadarUtils.GetResStr("rsecWrongBucketOperatorFormat") + S);

            var Index = StartPos + 2;
            while (Index <= S.Length)
                if (S[Index] == '}')
                    break;
                else
                    Index++;
            if (Index > S.Length)
                throw new Exception(RadarUtils.GetResStr("rsecWrongBucketOperatorFormat") + S);

            //var Index1 = StartPos;
            //var Index2 = Index;
            return S.Substring(StartPos + 2, Index - StartPos - 2);
        }

        internal string GetBucketName()
        {
            if (fIndex == 1)
                return GetBucketName(fBucketCollection.fString1);
            if (fIndex < fBucketCollection.Count)
                return GetBucketName(fBucketCollection.fString2);

            return GetBucketName(fBucketCollection.fString3);
        }

        public string GeBucketName(string sourceHierarchyDiscretizationBucketFormat)
        {
            throw new NotImplementedException();
        }
    }
}