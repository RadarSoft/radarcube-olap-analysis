using System.Collections.Generic;
using System.IO;
using System.Linq;
using RadarSoft.RadarCube.Controls.Grid;
using RadarSoft.RadarCube.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace RadarSoft.RadarCube.Controls.Toolbox
{
    public class OlapToolbox : OlapToolboxBase, IStreamedObject
    {
        public OlapToolbox(HttpContext context, IHostingEnvironment hosting):
            base(context, hosting)
        {
            InitButtons();
        }

        public override CustomToolboxButtonCollection CustomButtons { get; } = new CustomToolboxButtonCollection();

        //private void SaveSettings(string fileName)
        //{
        //    string tempFile = Grid.Cube.SessionState.WorkingDirectoryName + fileName + ".dat";
        //    using (FileStream fileStream = File.Create(tempFile))
        //    {
        //        Grid.SaveCompressed(fileStream, StreamContent.All);
        //    //MvcGrid.Response.WriteFile(tempFile);
        //    //MvcGrid.Response.Flush();
        //    //File.Delete(tempFile);

        //    byte[] buffer;
        //    int length = (int)fileStream.Length;
        //    buffer = new byte[length];
        //    int count;
        //    while ((count = fileStream.Read(buffer, 0, buffer.Length)) > 0)
        //    {
        //        MvcGrid.Response.OutputStream.Write(buffer, 0, count);
        //        MvcGrid.Response.Flush();
        //    }
        //    fileStream.Flush();
        //    fileStream.Close();
        //    File.Delete(tempFile);
        //    }
        //}

        //internal override string ImageUrl(string resName)
        //{
        //    if (fGrid == null)
        //    {
        //        var images = new MvcStoredImagesProvider(MvcGrid);
        //        return images.ImageUrl(resName, Page, typeof(OlapGrid), "Temp");
        //    }
        //    return fGrid.images.ImageUrl(resName, Page, typeof(OlapGrid), fGrid.TempPath);
        //}

        public void WriteStream(BinaryWriter writer, object options)
        {
            StreamUtils.WriteTag(writer, Tags.tgToolbox);
            StreamUtils.WriteStreamedObject(writer, CustomButtons, Tags.tgToolbox_CustomButtons);
            StreamUtils.WriteStreamedObject(writer, ConnectButton, Tags.tgToolbox_ConnectButton);
            StreamUtils.WriteStreamedObject(writer, SaveLayoutButton, Tags.tgToolbox_SaveLayoutButton);
            StreamUtils.WriteStreamedObject(writer, LoadLayoutButton, Tags.tgToolbox_LoadLayoutButton);
            StreamUtils.WriteStreamedObject(writer, MDXQueryButton, Tags.tgToolbox_MDXQueryButton);
            StreamUtils.WriteStreamedObject(writer, AddCalculatedMeasureButton,
                Tags.tgToolbox_AddCalculatedMeasureButton);
            StreamUtils.WriteStreamedObject(writer, AllAreasButton, Tags.tgToolbox_AllAreasButton);
            StreamUtils.WriteStreamedObject(writer, ClearLayoutButton, Tags.tgToolbox_ClearLayoutButton);
            StreamUtils.WriteStreamedObject(writer, DataAreaButton, Tags.tgToolbox_DataAreaButton);
            StreamUtils.WriteStreamedObject(writer, PivotAreaButton, Tags.tgToolbox_PivotAreaButton);
            StreamUtils.WriteStreamedObject(writer, ZoomOutButton, Tags.tgToolbox_ZoomOutButton);
            StreamUtils.WriteStreamedObject(writer, ZoomInButton, Tags.tgToolbox_ZoomInButton);
            StreamUtils.WriteStreamedObject(writer, ResetZoomButton, Tags.tgToolbox_ResetZoomButton);
            StreamUtils.WriteStreamedObject(writer, ModeButton, Tags.tgToolbox_ModeButton);
            StreamUtils.WriteStreamedObject(writer, DelayPivotingButton, Tags.tgToolbox_DelayPivotingButton);
            StreamUtils.WriteStreamedObject(writer, ResizingButton, Tags.tgToolbox_ResizingButton);
            StreamUtils.WriteStreamedObject(writer, MeasurePlaceButton, Tags.tgToolbox_MeasurePlaceButton);

            StreamUtils.WriteTag(writer, Tags.tgToolbox_ButtonsOrder);
            StreamUtils.WriteInt32(writer, fToolItems.Keys.ToList().Count);
            foreach (var bid in fToolItems.Keys.ToList())
                StreamUtils.WriteString(writer, bid);

            StreamUtils.WriteTag(writer, Tags.tgToolbox_EOT);
        }

        public void ReadStream(BinaryReader reader, object options)
        {
            StreamUtils.CheckTag(reader, Tags.tgToolbox);
            for (var exit = false; !exit;)
            {
                var tag = StreamUtils.ReadTag(reader);
                switch (tag)
                {
                    case Tags.tgToolbox_CustomButtons:
                        StreamUtils.ReadStreamedObject(reader, CustomButtons);
                        break;
                    case Tags.tgToolbox_ConnectButton:
                        StreamUtils.ReadStreamedObject(reader, ConnectButton);
                        break;
                    case Tags.tgToolbox_SaveLayoutButton:
                        StreamUtils.ReadStreamedObject(reader, SaveLayoutButton);
                        break;
                    case Tags.tgToolbox_LoadLayoutButton:
                        StreamUtils.ReadStreamedObject(reader, LoadLayoutButton);
                        break;
                    case Tags.tgToolbox_MDXQueryButton:
                        StreamUtils.ReadStreamedObject(reader, MDXQueryButton);
                        break;
                    case Tags.tgToolbox_AddCalculatedMeasureButton:
                        StreamUtils.ReadStreamedObject(reader, AddCalculatedMeasureButton);
                        break;
                    case Tags.tgToolbox_AllAreasButton:
                        StreamUtils.ReadStreamedObject(reader, AllAreasButton);
                        break;
                    case Tags.tgToolbox_ClearLayoutButton:
                        StreamUtils.ReadStreamedObject(reader, ClearLayoutButton);
                        break;
                    case Tags.tgToolbox_DataAreaButton:
                        StreamUtils.ReadStreamedObject(reader, DataAreaButton);
                        break;
                    case Tags.tgToolbox_PivotAreaButton:
                        StreamUtils.ReadStreamedObject(reader, PivotAreaButton);
                        break;
                    case Tags.tgToolbox_ZoomOutButton:
                        StreamUtils.ReadStreamedObject(reader, ZoomOutButton);
                        break;
                    case Tags.tgToolbox_ZoomInButton:
                        StreamUtils.ReadStreamedObject(reader, ZoomInButton);
                        break;
                    case Tags.tgToolbox_ResetZoomButton:
                        StreamUtils.ReadStreamedObject(reader, ResetZoomButton);
                        break;
                    case Tags.tgToolbox_ModeButton:
                        StreamUtils.ReadStreamedObject(reader, ModeButton);
                        break;
                    case Tags.tgToolbox_DelayPivotingButton:
                        StreamUtils.ReadStreamedObject(reader, DelayPivotingButton);
                        break;
                    case Tags.tgToolbox_ResizingButton:
                        StreamUtils.ReadStreamedObject(reader, ResizingButton);
                        break;
                    case Tags.tgToolbox_MeasurePlaceButton:
                        StreamUtils.ReadStreamedObject(reader, MeasurePlaceButton);
                        break;
                    case Tags.tgToolbox_ButtonsOrder:
                        var count = StreamUtils.ReadInt32(reader);
                        var buttonsOrder = new string[count];
                        for (var i = 0; i < buttonsOrder.Length; i++)
                            buttonsOrder[i] = StreamUtils.ReadString(reader);

                        SortToolItems(buttonsOrder);
                        break;
                    case Tags.tgToolbox_EOT:
                        exit = true;
                        break;
                    default:
                        StreamUtils.SkipValue(reader);
                        break;
                }
            }
        }

        internal override string ImageUrl(string resName)
        {
            return OlapControl.images.ImageUrl(resName, typeof(OlapGrid), OlapControl.TempPath);
        }

        internal override void InitButtons()
        {
            fConnectButton = new ConnectToolboxButton();
            //RegisterToolItem(fConnectButton);
            //fConnectButton.LoginWindowSettings.fOwner = this;

            fMDXQueryButton = new MDXQueryButton();
            //RegisterToolItem(fMDXQueryButton);

            fAddCalcMeasureButton = new AddCalculatedMeasureButton {NeedSeparator = true};
            RegisterToolItem(fAddCalcMeasureButton);

            fSaveLayoutButton = new SaveLayoutToolboxButton();
            //RegisterToolItem(fSaveLayoutButton);

            fLoadLayoutButton = new LoadLayoutButton {NeedSeparator = true};
            //RegisterToolItem(fLoadLayoutButton);
            //fLoadLayoutButton.fFile.ID = "olaptlw_loadfile_" + fLoadLayoutButton.GetGridID();
            //Controls.Add(fLoadLayoutButton.fFile);

            fClearLayoutButton = new ClearLayoutToolboxButton();
            RegisterToolItem(fClearLayoutButton);

            fAllAreasButton = new AllAreasToolboxButton();
            RegisterToolItem(fAllAreasButton);

            fPivotAreaButton = new PivotAreaToolboxButton();
            RegisterToolItem(fPivotAreaButton);

            fDataAreaButton = new DataAreaToolboxButton {NeedSeparator = true};
            RegisterToolItem(fDataAreaButton);

            fMeasurePlaceButton = new MeasurePlaceToolboxButton {NeedSeparator = true};
            RegisterToolItem(fMeasurePlaceButton);

            fZoomOutButton = new ScaleDecreaseButton();
            RegisterToolItem(fZoomOutButton);

            fZoomInButton = new ScaleIncreaseButton();
            RegisterToolItem(fZoomInButton);

            fResetZoomButton = new ScaleResetButton {NeedSeparator = true};
            RegisterToolItem(fResetZoomButton);

            FModeButton = new ModeButton {NeedSeparator = true};
            RegisterToolItem(FModeButton);

            fResizingButton = new ResizingButton {NeedSeparator = true};
            RegisterToolItem(fResizingButton);

            fDelayPivotingButton = new DelayPivotingButton();
            RegisterToolItem(fDelayPivotingButton);
        }

        internal void SortToolItems(IEnumerable<string> orderList)
        {
            var newToolItems = new Dictionary<string, CommonToolboxButton>();
            foreach (var bid in orderList)
            {
                if (fToolItems.TryGetValue(bid, out CommonToolboxButton button))
                {
                    fToolItems.Remove(bid);
                    newToolItems.Add(bid, button);
                }
            }

            foreach (var button in fToolItems.Values)
                newToolItems.Add(button.ButtonID, button);

            fToolItems.Clear();
            fToolItems = newToolItems;
            CorrectOrderOfToolItems();
        }
    }
}