using System.Collections.Generic;
using RadarSoft.RadarCube.Tools;

namespace RadarSoft.RadarCube.CubeStructure
{
    internal class BucketCollection : List<Bucket>
    {
        internal string fFormatString;
        internal string fString1;
        internal string fString2;
        internal string fString3;

        internal Bucket AddAndReturn()
        {
            var b = new Bucket(this) {fIndex = Count};
            Add(b);
            return b;
        }

        //As the format string can be used template
        //"string1%;string2%; " string3" or "string". Each string can include the following statements,
        //which-naming replaced by real members:
        //%{First} - the First member of the current бакита
        //%{Last} - the Last member of the current бакита
        //%{Previous last} - the Last member of the previous бакита
        //%{Next first} - First слен next бакита
        //%{Min} - Minimum (lowest) member of the current бакита
        //%{Max} - the Maximum (highest) member of the current бакита
        //%{Previous Min} - Minimum (lowest) member of the previous бакита
        //%{Previous Max} - the Maximum (highest) member of the previous бакита
        //%{Next Min} - Minimum (lowest) member of the next бакита
        //%{Next Max} - the Maximum (highest) member of the next бакита
        //%{Index} - the Index of the current бакита

        internal void SetBucketFormat(string BucketNameFormat, string AFormatString)
        {
            fFormatString = AFormatString;

            var S = BucketNameFormat;
            var bmd = RadarUtils.GetResStr("rsBucketMemberDelimiter");
            var Index = S.IndexOf(bmd);
            if (Index == -1)
            {
                fString1 = S;
                fString2 = S;
                fString3 = S;
                return;
            }

            fString1 = S.Substring(0, Index);
            S = S.Substring(Index + bmd.Length, S.Length - 1);

            Index = S.IndexOf(bmd);
            if (Index == -1)
            {
                fString2 = S;
                fString3 = S;
                return;
            }
            fString2 = S.Substring(0, Index - 1);
            S = S.Substring(Index + bmd.Length, S.Length - (Index + bmd.Length));

            Index = S.IndexOf(bmd);
            if (Index == -1)
            {
                fString3 = S;
                return;
            }
            fString3 = S.Substring(0, Index - 1);
        }

        public void SeBucketFormat(string sourceHierarchyDiscretizationBucketFormat, string sourceHierarchyFormatString)
        {
            throw new System.NotImplementedException();
        }
    }
}