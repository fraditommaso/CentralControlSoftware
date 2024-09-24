using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CentralControlSoftware
{
    public class Shimmer : ExternalDevice
    {
        // PROPERTIES
        //private string transmitterIPMaster = "127.0.0.1";
        //private string receiverIPMaster = "127.0.0.1";
        //private string receiverIPSlave = "127.0.0.1";
        //private int transmitterPortMaster = 50121;
        //private int receiverPortMaster = 50124;
        //private int receiverPortSlave = 50122;

        public Process process;


        // CONSTUCTOR
        // Inherited from ExternalDevice
        public Shimmer(string _transmitterIpMaster, int _transmitterPortMaster, string _receiverIpMaster, int _receiverPortMaster,
            string _receiverIpSlave, int _receiverPortSlave) : base(_transmitterIpMaster, _transmitterPortMaster,
                _receiverIpMaster, _receiverPortMaster, _receiverIpSlave, _receiverPortSlave)
        {
            externalDeviceName = "Shimmer";
            columnHeadersExternalDevice = string.Join("\t", "nSensor", "Trigger", "PacketType", "Timestamp", 
                "GSR", "PPG", "\n");
            ackMessageSynchronization = "sync done";
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
            string comPort = ManageConnectionInputParameters["comPort"];

            process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            // This line opens a cmd window and pops it up when launched
            //process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            // These lines redirect input to Visual Studio Output Window instead of showing to cmd window.
            //process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            //process.StartInfo.UseShellExecute = false;
            //process.StartInfo.RedirectStandardInput = true;

            // This line does not show cmd window
            //process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            // This line opens a cmd window but minimizes it so it does not pop up to the screen when launched
            process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;

            string[] subfolders = { defaultDirectory, "Shimmer" };
            string workingDirectoryPath = Path.Combine(subfolders);
            process.StartInfo.WorkingDirectory = workingDirectoryPath;
            process.StartInfo.Arguments = "/k python shimmer_API_python.py " + comPort;
            process.Start();
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

            unpackedDataDictionary.Add("nSensor", Convert.ToInt32(lineSubstrings[0]));
            unpackedDataDictionary.Add("Trigger", Convert.ToInt32(lineSubstrings[1]));
            unpackedDataDictionary.Add("PacketType", Convert.ToInt32(lineSubstrings[2]));
            unpackedDataDictionary.Add("Timestamp", Convert.ToInt32(lineSubstrings[3]));
            unpackedDataDictionary.Add("GSR", Convert.ToDouble(lineSubstrings[4]));
            unpackedDataDictionary.Add("PPG", Convert.ToDouble(lineSubstrings[5]));

            return unpackedDataDictionary;
        }

        protected override void CheckTransmissionMessage()
        {

        }
    }
}
