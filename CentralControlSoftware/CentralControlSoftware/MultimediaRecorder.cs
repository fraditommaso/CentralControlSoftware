using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CentralControlSoftware
{
    public class MultimediaRecorder : ExternalDevice
    {
        private Process process;

        private string cmdCommandLine;
        protected Thread recordThread;

        public MultimediaRecorder(string _transmitterIpMaster, int _transmitterPortMaster, string _receiverIpSlave,
            int _receiverPortSlave) : base(_transmitterIpMaster, _transmitterPortMaster, _receiverIpSlave, _receiverPortSlave)
        {
            externalDeviceName = "MultimediaRecorder";
            triggerStart = "1";
            triggerEnd = "0";
        }

        protected override void StartConnection(Dictionary<string, string> ManageConnectionInputParameters)
        {
            string defaultDirectory = ManageConnectionInputParameters["defaultDirectory"];
            string filename = ManageConnectionInputParameters["filename"];
            RecordMultimedia(defaultDirectory, filename);

            DataFromExternalDeviceEventArgs args = new DataFromExternalDeviceEventArgs
            {
                name = externalDeviceName,
                isExternalDeviceSynchronized = true
            };
            OnDataFromExternalDevice(args);
        }

        protected override void StopConnection()
        {
            try
            {
                if (null != recordThread)
                {
                    recordThread.Abort();
                }

                if (process != null)
                {
                    process.CloseMainWindow();
                    process.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        protected override void SendDataToSlaveCustomized() { }

        protected override void ReceiveMessagesFromSlave(string receivedMessage) { }

        protected override Dictionary<String, Object> UnpackArrayData(string[] lineSubstrings)
        {
            Dictionary<String, Object> unpackedDataDictionary = new Dictionary<string, object>();

            return unpackedDataDictionary;
        }

        protected override void CheckTransmissionMessage() { }

        public void RecordMultimedia(string defaultPath, string filename = "default")
        {
            process = new Process();

            process.StartInfo.FileName = "cmd.exe";

            // This line opens a cmd window but minimizes it so it does not pop up to the screen when launched
            process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;

            string[] subfolders = { defaultPath, "AudioVideoRecorder_Python" };

            string workingDirectoryPath = Path.Combine(subfolders);

            process.StartInfo.WorkingDirectory = workingDirectoryPath;

            if (filename != null)
            {
                cmdCommandLine = "/k python AudioVideoRecorder.py " + filename;
            }
            else
            {
                cmdCommandLine = "/k python AudioVideoRecorder.py";
            }
            process.StartInfo.Arguments = cmdCommandLine;
            process.Start();
        }

        public void CheckSaveMultimediaFiles()
        {
            bool? isMultimediaSaved = null;
                  
            //string multimediaFolder = Path.Combine(defaultDirectory, filename);
            string multimediaFolder = workingDirectory;
            string multimediaFilenameMixed = Path.Combine(multimediaFolder, filename + "_mixed.mp4");

            if (Directory.Exists(multimediaFolder))
            {
                if (File.Exists(multimediaFilenameMixed))
                {
                    isMultimediaSaved = true;
                }
                else
                {
                    isMultimediaSaved = false;
                }
            }
            
            //DataFromExternalDeviceArgs.isDataSaved = isMultimediaSaved;
            //OnDataFromExternalDevice(DataFromExternalDeviceArgs);

            DataFromExternalDeviceEventArgs args = new DataFromExternalDeviceEventArgs
            {
                name = externalDeviceName,
                isDataSaved = isMultimediaSaved
            };
            OnDataFromExternalDevice(args);
        }
    }
}
