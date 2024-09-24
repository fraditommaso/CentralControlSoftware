using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CentralControlSoftware
{
    public class XsensDOT : ExternalDevice
    {
        // PROPERTIES
        //private string transmitterIPMaster = "127.0.0.1";
        //private string receiverIPMaster = "127.0.0.1";
        //private string receiverIPSlave = "127.0.0.1";
        //private int receiverPortMaster = 50114;
        //private int receiverPortSlave = 50112;
        //private int transmitterPortMaster = 50111;

        private readonly string url = "http://localhost:8080/"; //http://127.0.0.1:8080/
        public Process process;
        private string xsensDotStreamingMode;


        // CONSTUCTOR
        // Inherited from ExternalDevice
        public XsensDOT(string _transmitterIpMaster, int _transmitterPortMaster, string _receiverIpMaster, int _receiverPortMaster, 
            string _receiverIpSlave, int _receiverPortSlave) : base(_transmitterIpMaster, _transmitterPortMaster, 
                _receiverIpMaster, _receiverPortMaster, _receiverIpSlave, _receiverPortSlave)
        {
            // Override these class-specific properties
            externalDeviceName = "XsensDOT";

            ackMessageSynchronization = "sync done";
            triggerStart = "Start Trigger";
            triggerEnd = "Stop Trigger";
        }


        // METHODS

        // Event Handlers: inherited from ExternalDevice

        // Transmitter: inherited from ExternalDevice

        // Receiver: inherited from ExternalDevice

        // Child class-specific methods
        protected override void StartConnection(Dictionary<String, String> ManageConnectionInputParameters)
        {
            string defaultDirectory = ManageConnectionInputParameters["defaultDirectory"];
            xsensDotStreamingMode = ManageConnectionInputParameters["streamingMode"];

            if (xsensDotStreamingMode == "Rate quantities")
            {
                columnHeadersExternalDevice = string.Join("\t", "Timestamp", "Address",
                    "Acc_x", "Acc_y", "Acc_z",
                    "Gyr_x", "Gyr_y", "Gyr_z",
                    "Mag_x", "Mag_y", "Mag_z", 
                    "Trigger", "\n");
            }
            else if (xsensDotStreamingMode == "Custom Mode 1")
            {
                columnHeadersExternalDevice = string.Join("\t", "Timestamp", "Address",
                    "Euler_x", "Euler_y", "Euler_z",
                    "FreeAcc_x", "FreeAcc_y", "FreeAcc_z",
                    "Gyr_x", "Gyr_y", "Gyr_z",
                    "Trigger", "\n");
            }

            bool isXsensDotDebug = false;
            Console.WriteLine("isXsensDotDebug = " + isXsensDotDebug);

            if (isXsensDotDebug == false)
            {
                string[] subfolders = { defaultDirectory, "XsensDOT", "xsens_dot_server-master" };
                string workingDirectoryPath = Path.Combine(subfolders);

                process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                process.StartInfo.WorkingDirectory = workingDirectoryPath;
                process.StartInfo.Arguments = "/c node xsensDotServer";
                //process.StartInfo.UseShellExecute = false;
                //process.StartInfo.RedirectStandardInput = true;
                process.Start();

                try
                {
                    Process.Start("explorer", url);
                }
                catch (System.ComponentModel.Win32Exception noBrowser)
                {
                    if (noBrowser.ErrorCode == -2147467259)
                        Console.WriteLine(noBrowser.Message);
                }
                catch (System.Exception otherException)
                {
                    Console.WriteLine(otherException.Message);
                }
            }
        }

        protected override void StopConnection()
        {
            try
            {
                if (process != null)
                {
                    process.CloseMainWindow();
                    process.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error in externalDevice (" + externalDeviceName + ") - StopConnection()");
                Console.WriteLine(ex);
            }
        }

        protected override void SendDataToSlaveCustomized() { }

        protected override void ReceiveMessagesFromSlave(string receivedMessage)
        {

        }

        protected override Dictionary<String, Object> UnpackArrayData(string[] lineSubstrings)
        {
            Dictionary<String, Object> unpackedDataDictionary = new Dictionary<string, object>();

            unpackedDataDictionary.Add("Timestamp", Convert.ToDouble(lineSubstrings[0]));
            unpackedDataDictionary.Add("Address", lineSubstrings[1]);
            if (xsensDotStreamingMode == "Rate quantities")
            {
                unpackedDataDictionary.Add("Acc_X", Convert.ToDouble(lineSubstrings[2]));
                unpackedDataDictionary.Add("Acc_Y", Convert.ToDouble(lineSubstrings[3]));
                unpackedDataDictionary.Add("Acc_Z", Convert.ToDouble(lineSubstrings[4]));
                unpackedDataDictionary.Add("Gyr_X", Convert.ToDouble(lineSubstrings[5]));
                unpackedDataDictionary.Add("Gyr_Y", Convert.ToDouble(lineSubstrings[6]));
                unpackedDataDictionary.Add("Gyr_Z", Convert.ToDouble(lineSubstrings[7]));
                unpackedDataDictionary.Add("Mag_X", Convert.ToDouble(lineSubstrings[8]));
                unpackedDataDictionary.Add("Mag_Y", Convert.ToDouble(lineSubstrings[9]));
                unpackedDataDictionary.Add("Mag_Z", Convert.ToDouble(lineSubstrings[10]));
                unpackedDataDictionary.Add("Trigger", Convert.ToDouble(lineSubstrings[11]));
            }
            else if (xsensDotStreamingMode == "Custom Mode 1")
            {
                unpackedDataDictionary.Add("Euler_X", Convert.ToDouble(lineSubstrings[2]));
                unpackedDataDictionary.Add("Euler_Y", Convert.ToDouble(lineSubstrings[3]));
                unpackedDataDictionary.Add("Euler_Z", Convert.ToDouble(lineSubstrings[4]));
                unpackedDataDictionary.Add("FreeAcc_X", Convert.ToDouble(lineSubstrings[5]));
                unpackedDataDictionary.Add("FreeAcc_Y", Convert.ToDouble(lineSubstrings[6]));
                unpackedDataDictionary.Add("FreeAcc_Z", Convert.ToDouble(lineSubstrings[7]));
                unpackedDataDictionary.Add("Gyr_X", Convert.ToDouble(lineSubstrings[8]));
                unpackedDataDictionary.Add("Gyr_Y", Convert.ToDouble(lineSubstrings[9]));
                unpackedDataDictionary.Add("Gyr_Z", Convert.ToDouble(lineSubstrings[10]));
                unpackedDataDictionary.Add("Trigger", Convert.ToDouble(lineSubstrings[11]));
            }

            return unpackedDataDictionary;
        }

        protected override void CheckTransmissionMessage()
        {

        }
    }
}
