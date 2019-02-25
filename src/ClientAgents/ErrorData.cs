using System;
using System.Reflection;
using RadarSoft.RadarCube.Controls;

namespace RadarSoft.RadarCube.ClientAgents
{
    /// <exclude />
    public class ErrorData
    {
        public ErrorData()
        {
        }

        public ErrorData(Exception E, OlapControl grid)
        {
            try
            {
                Message = E.Message;

                GridVersion = grid.GetType().Name + ": " + grid.GetType().GetTypeInfo().Assembly.GetName().Version;
                if (grid.Cube != null)
                    CubeVersion = grid.Cube.GetType().Name + ": " +
                                  grid.Cube.GetType().GetTypeInfo().Assembly.GetName().Version;
                else
                    CubeVersion = "Cube is not connected.";
                if (grid.callbackExceptionData != null)
                    foreach (var item in grid.callbackExceptionData)
                        RequestInfo = item.Key + ": " + item.Value;
                StackTrace = E.StackTrace;
                Support = grid.SupportEMail;
            }
            catch (Exception eee)
            {
                Message = eee.Message;
                StackTrace = eee.StackTrace;
            }
        }

        public string Message { get; set; }
        public string GridVersion { get; set; }
        public string CubeVersion { get; set; }
        public string StackTrace { get; set; }
        public string RequestInfo { get; set; }
        public string Support { get; set; }
    }
}