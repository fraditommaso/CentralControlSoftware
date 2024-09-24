using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CentralControlSoftware
{
    public class Bioharness : ExternalDevice
    {
        // PROPERTIES
        //private string transmitterIPMaster = "127.0.0.1";
        //private string receiverIPMaster = "127.0.0.1";
        //private string receiverIPSlave = "127.0.0.1";
        //private int transmitterPortMaster = 50131;
        //private int receiverPortMaster = 50134;
        //private int receiverPortSlave = 50132;

        public Process process;


        // CONSTUCTOR
        // Inherited from ExternalDevice
        public Bioharness(string _transmitterIpMaster, int _transmitterPortMaster, string _receiverIpMaster, int _receiverPortMaster,
            string _receiverIpSlave, int _receiverPortSlave) : base(_transmitterIpMaster, _transmitterPortMaster,
                _receiverIpMaster, _receiverPortMaster, _receiverIpSlave, _receiverPortSlave)
        {
            externalDeviceName = "Bioharness";
            columnHeadersExternalDevice = string.Join("\t", "Trigger", "Timestamp", 
                "Heart Rate", "Respiratory Rate", "ECG", "Breathing", "\n");
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
            string minicondaPath = ManageConnectionInputParameters["accessoryPath"];
            string macAddress = ManageConnectionInputParameters["macAddress"];

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

            process.StartInfo.WorkingDirectory = minicondaPath;
            process.StartInfo.Arguments = "/k activate pyzephyr";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;

            process.Start();

            string[] subfoldersToMain = { defaultDirectory, "BioHarness", "App-Zephyr-main" };
            string zephyrMainPath = Path.Combine(subfoldersToMain);
            byte[] bytes = Encoding.Default.GetBytes(zephyrMainPath);
            zephyrMainPath = Encoding.UTF8.GetString(bytes);

            process.StandardInput.WriteLine("cd " + zephyrMainPath);
            process.StandardInput.WriteLine("run --address=" + macAddress + " --stream=summary,ecg,respiration");
            process.StandardInput.WriteLine("01");
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

            unpackedDataDictionary.Add("Trigger", Convert.ToDouble(lineSubstrings[0]));
            unpackedDataDictionary.Add("Timestamp", lineSubstrings[1]);
            unpackedDataDictionary.Add("Heart Rate", Convert.ToDouble(lineSubstrings[3]));
            if (lineSubstrings[4] != "nan")
                unpackedDataDictionary.Add("Respiratory Rate", Convert.ToDouble(lineSubstrings[4]));
            else
                unpackedDataDictionary.Add("Respiratory Rate", Convert.ToDouble("0"));

            unpackedDataDictionary.Add("ECG", ConvertStringArray2DoubleArray(GetDataFromStringArray(lineSubstrings, 6)));
            unpackedDataDictionary.Add("Breathing", ConvertStringArray2DoubleArray(GetDataFromStringArray(lineSubstrings, 8)));
            return unpackedDataDictionary;
        }

        protected override void CheckTransmissionMessage() { }

        public double[] ConvertStringArray2DoubleArray(string[] dataSubstring)
        {
            double[] dataDoubleArray = null;
            double[] tempDataDoubleArray = new double[dataSubstring.Length];
            int counter = 0;        // to keep count of the real "numeric" values stored in the string array

            try
            {
                foreach (string dataStringValue in dataSubstring)
                {
                    if (Double.TryParse(dataStringValue, out double tempDouble))
                    {
                        tempDataDoubleArray[counter] = tempDouble;
                        counter++;
                    }
                }

                dataDoubleArray = new double[counter - 1];
                Array.Copy(tempDataDoubleArray, dataDoubleArray, counter - 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return dataDoubleArray;
        }

        public string[] GetDataFromStringArray(string[] dataStringArray, int dataIndex)
        {
            string dataString = dataStringArray[dataIndex].Replace("[", string.Empty);
            dataString = dataString.Replace("]", string.Empty);

            string[] dataSubstringArray = dataString.Split(new char[] { ',', ' ' });
            return dataSubstringArray;
        }
    }
}
