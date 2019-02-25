using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadarSoft.RadarCube.Controls.Analysis;
using RadarSoft.RadarCube.Enums;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using RadarSoft.RadarCube.Interfaces;
using UnitTest.OlapAnalysis.Mock;

namespace UnitTest.OlapAnalysis
{
    [TestClass]
    public class OlapAnalysisTest
    {
        private MOlapAnalysis _OlapAnalysis;
        public MOlapAnalysis OlapAnalysis
        {
            get
            {
                if (_OlapAnalysis != null)
                    return _OlapAnalysis;

                _OlapAnalysis = new MOlapAnalysis("OlapAnalysis1", MockCreator.CreateContext(), MockCreator.CreateHosting(), null);
                _OlapAnalysis.AddResourceStrings("Resources.ru.resx", new CultureInfo("ru-RU"));
                _OlapAnalysis.AddResourceStrings("Resources.en.resx", new CultureInfo("en"));

                return _OlapAnalysis;
            }
        }

        void ClearOlapAnalysis()
        {
            if (_OlapAnalysis == null)
                return;
            _OlapAnalysis.Active = false;
            _OlapAnalysis = null;
        }

        public void Initialize()
        {
            OlapAnalysis.MaxTextLength = 17;
            OlapAnalysis.Height = "500px";
            OlapAnalysis.UseFixedHeaders = false;

            ActivateOlapAnalysis();

            OlapAnalysis.PivotingFirst(OlapAnalysis.Dimensions.FindHierarchy("[Product].[Category]"), LayoutArea.laRow);
            OlapAnalysis.PivotingFirst(OlapAnalysis.Dimensions.FindHierarchy("[Date].[Fiscal Year]"), LayoutArea.laColumn);

            OlapAnalysis.Pivoting(OlapAnalysis.Measures.Find("[Measures].[Internet Sales-Unit Price]"), LayoutArea.laRow, null);
        }

        public void ActivateOlapAnalysis()
        {
            SqlConnectionStringBuilder csBuilder = new SqlConnectionStringBuilder();
            csBuilder.DataSource = "http://localhost/OLAP/msmdpump.dll";
            csBuilder.InitialCatalog = @"Analysis Services Tutorial";
            csBuilder.ConnectTimeout = 30;
            //csBuilder.UserID = "sa";
            //csBuilder.Password = "admin@123";

            //Data Source=http://localhost/OLAP/msmdpump.dll;Initial Catalog="Analysis Services Tutorial";Connect Timeout=30
            string cs = csBuilder.ConnectionString;

            OlapAnalysis.ConnectionString = cs;
            OlapAnalysis.CubeName = "Analysis Services Tutorial";

            if (OlapAnalysis.Active)
            {
                OlapAnalysis.Active = false;
            }

            OlapAnalysis.Active = true;
        }

        [TestMethod]
        public void TestRender()
        {
            ClearOlapAnalysis();
            OlapAnalysis.InitOlap += delegate { Initialize(); };
            var request = OlapAnalysis.Render();
            Debug.WriteLine(request.ToString());
            
            Assert.IsFalse(string.IsNullOrEmpty(request.ToString()));
        }

        [TestMethod]
        public void TestPivoting()
        {
            ClearOlapAnalysis();
            ActivateOlapAnalysis();

            OlapAnalysis.BeginUpdate();
            OlapAnalysis.PivotingFirst(OlapAnalysis.Dimensions.FindHierarchy("[Customer].[Customer Geography]"), LayoutArea.laRow);
            //var m = OlapAnalysis.Measures.Find("[Measures].[Internet Sales Count]");
            //OlapAnalysis.Pivoting(m);
            OlapAnalysis.EndUpdate();
        }

        [TestMethod]
        public void TestDrilling()
        {
            ClearOlapAnalysis();
            ActivateOlapAnalysis();

            OlapAnalysis.BeginUpdate();
            OlapAnalysis.PivotingFirst(OlapAnalysis.Dimensions.FindHierarchy("[Customer].[Customer Geography]"), LayoutArea.laRow);
            var m = OlapAnalysis.Measures.Find("[Measures].[Internet Sales Count]");
            OlapAnalysis.Pivoting(m);
            OlapAnalysis.EndUpdate();


            var drillCells = new List<Tuple<int, int>>
            {
                Tuple.Create(0, 2),
                Tuple.Create(1, 2),
                Tuple.Create(2, 2),
                Tuple.Create(3, 2)
            };


            foreach (var cellCoord in drillCells)
            {
                int icol = cellCoord.Item1;
                int irow = cellCoord.Item2;

                ICell c = OlapAnalysis.CellSet.Cells(icol, irow);
                if (c is IMemberCell)
                {
                    IMemberCell mc = (IMemberCell)c;
                    mc.DrillAction(PossibleDrillActions.esNextLevel);
                }
                
            }

            //var request = OlapAnalysis.Render();
            //Debug.WriteLine(request.ToString());
            //Assert.IsFalse(string.IsNullOrEmpty(request.ToString()));
        }





    }
}
