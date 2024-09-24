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
    public class MachineLearning : ExternalDevice
    {
        // PROPERTIES
        //private string transmitterIPMaster = "127.0.0.1";
        //private string receiverIPMaster = "127.0.0.1";
        //private string receiverIPSlave = "127.0.0.1";
        //private int receiverPortMaster = ;
        //private int receiverPortSlave = ;
        //private int transmitterPortMaster = ;

        //private Thread generateRandomBinaryThread;


        public Process process;

        public MachineLearning(string _transmitterIpMaster, int _transmitterPortMaster, string _receiverIpMaster, int _receiverPortMaster,
            string _receiverIpSlave, int _receiverPortSlave) : base(_transmitterIpMaster, _transmitterPortMaster,
                _receiverIpMaster, _receiverPortMaster, _receiverIpSlave, _receiverPortSlave)
        {
            externalDeviceName = "MachineLearning";
            columnHeadersExternalDevice = String.Join("\t", "Timestamp", "Engagement", "\n");
            ackMessageSynchronization = "sync";
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
            //DataFromExternalDeviceEventArgs args = new DataFromExternalDeviceEventArgs
            //{
            //    name = externalDeviceName,
            //    isExternalDeviceSynchronized = true
            //};
            //OnDataFromExternalDevice(args);

            string defaultDirectory = ManageConnectionInputParameters["defaultDirectory"];
            string emlModel = ManageConnectionInputParameters["emlModel"];

            process = new Process();
            process.StartInfo.FileName = "cmd.exe";

            // This line opens a cmd window but minimizes it so it does not pop up to the screen when launched
            process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;

            string[] subfolders = { defaultDirectory, "MachineLearning" };
            string workingDirectoryPath = Path.Combine(subfolders);
            process.StartInfo.WorkingDirectory = workingDirectoryPath;
            //process.StartInfo.Arguments = "/k python eml_main_m.py " + emlModel;
            process.StartInfo.Arguments = "/k py -3.8 eml_main_m.py " + emlModel;
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

        protected override void SendDataToSlaveCustomized()
        {

        }

        protected override void ReceiveMessagesFromSlave(string receivedMessage)
        {

        }

        // Test Debug
        protected override Dictionary<String, Object> UnpackArrayData(string[] lineSubstrings)
        {
            Dictionary<String, Object> unpackedDataDictionary = new Dictionary<string, object>();
            unpackedDataDictionary.Add("Timestamp", lineSubstrings[0]);
            unpackedDataDictionary.Add("Engagement", Convert.ToDouble(lineSubstrings[1]));

            return unpackedDataDictionary;
        }

        protected override void CheckTransmissionMessage()
        {
        }
    }
}
