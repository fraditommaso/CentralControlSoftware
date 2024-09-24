using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace CentralControlSoftware
{
    public class Multimedia : ExternalDevice
    {
        // PROPERTIES
        //private string receiverIPSlave = "127.0.0.1";
        //private int receiverPortSlave = 50212;

        //private Process processRecord;
        //private Process processPlayback;
        private Process process;
        //private Thread multimediaThread; // should be unnecessary because multimedia runs on separate process and multitasking should be implicit.
        

        private string cmdCommandLine;
        protected Thread recordThread;
        protected Thread playblackThread;

        public Multimedia(string _transmitterIpMaster, int _transmitterPortMaster, string _receiverIpSlave, 
            int _receiverPortSlave) : base(_transmitterIpMaster, _transmitterPortMaster, _receiverIpSlave, _receiverPortSlave)
        {
            externalDeviceName = "Multimedia";
            triggerStart = "1";
            triggerEnd = "0";
        }

        // METHODS

        // Event Handlers: inherited from ExternalDevice

        // Transmitter: inherited from ExternalDevice

        // Receiver: inherited from ExternalDevice

        // Child class-specific methods
        protected override void StartConnection(Dictionary<String, String> ManageConnectionInputParameters)
        {
            string defaultDirectory = ManageConnectionInputParameters["defaultDirectory"];
            string experimentalCondition = ManageConnectionInputParameters["experimentalCondition"];
            string filename = ManageConnectionInputParameters["filename"];

            if (experimentalCondition == "record" | experimentalCondition == "haptic" | experimentalCondition == "bidirectional")
            {
                //if (null != recordThread)
                //    recordThread.Abort();

                //recordThread = new Thread(() => RecordMultimedia(defaultDirectory, filename));
                //recordThread.Start();
                //recordThread.Name = externalDeviceName + "_" + experimentalCondition;
                
                RecordMultimedia(defaultDirectory, filename);
            }
            else if (experimentalCondition == "playback")
            {
                //if (null != playblackThread)
                //    playblackThread.Abort();

                //playblackThread = new Thread(() => PlaybackMultimedia(defaultDirectory));
                //playblackThread.Start();
                //playblackThread.Name = externalDeviceName + "_" + experimentalCondition;
                
                // This works
                //PlaybackMultimedia(defaultDirectory);

                // Test to record data also in playback conditions
                //if (null != recordThread)
                //    recordThread.Abort();

                //recordThread = new Thread(() => RecordMultimedia(defaultDirectory, filename));
                //recordThread.Start();
                //recordThread.Name = externalDeviceName + "_" + experimentalCondition;
                //RecordMultimedia(defaultDirectory, filename);
            }

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
                    //processRecord.CloseMainWindow();
                    //processRecord.Dispose();
                    recordThread.Abort();
                }


                if (null != playblackThread)
                {
                    //processPlayback.CloseMainWindow();
                    //processPlayback.Dispose();
                    playblackThread.Abort();
                }

                process.CloseMainWindow();
                process.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        protected override void SendDataToSlaveCustomized() { }

        protected override void ReceiveMessagesFromSlave(string receivedMessage)
        {

        }

        protected override Dictionary<String, Object> UnpackArrayData(string[] lineSubstrings)
        {
            Dictionary<String, Object> unpackedDataDictionary = new Dictionary<string, object>();

            return unpackedDataDictionary;
        }

        protected override void CheckTransmissionMessage()
        {

        }

        public void RecordMultimedia(string defaultPath, string filename = "default")
        {
            process = new Process();

            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            string[] subfolders = { defaultPath, "AudioVideoRecorder_Python"};

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

        public void PlaybackMultimedia(string defaultPath)
        {
            process = new Process();

            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            string[] subfolders = { defaultPath, "AudioVideoRecorder_Python"};

            string workingDirectoryPath = Path.Combine(subfolders);

            process.StartInfo.WorkingDirectory = workingDirectoryPath;
            process.StartInfo.Arguments = "/k python AudioVideoPlayer.py"; ;
            process.Start();
        }
    }
}
