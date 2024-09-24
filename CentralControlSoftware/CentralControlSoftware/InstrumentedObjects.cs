using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace CentralControlSoftware
{
    public class InstrumentedObjects : ExternalDevice
    {

        // PROPERTIES
        //private string transmitterIPMaster = "192.168.0.66";
        //private string receiverIPMaster = "192.168.0.66";
        //private string receiverIPSlave = "192.168.0.4";
        //private int transmitterPortMaster = 50411;
        //private int receiverPortMaster = 50414;
        //private int receiverPortSlave = 50412;

        string deviceConfiguration;

        // CONSTUCTOR
        // Inherited from ExternalDevice
        public InstrumentedObjects(string _transmitterIpMaster, int _transmitterPortMaster, string _receiverIpMaster, int _receiverPortMaster,
            string _receiverIpSlave, int _receiverPortSlave) : base(_transmitterIpMaster, _transmitterPortMaster,
                _receiverIpMaster, _receiverPortMaster, _receiverIpSlave, _receiverPortSlave)
        {
            externalDeviceName = "InstrumentedObjects";
            receivedDataFormatType = "float";
            ackMessageSynchronization = "50412";
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
        }

        protected override void StopConnection()
        {

        }

        protected override void SendDataToSlaveCustomized() { }


        protected override void ReceiveMessagesFromSlave(string receivedMessage)
        {
            char[] separator = { '\t' };
            string[] lineSubstrings = receivedString.Split(separator);

            deviceConfiguration = lineSubstrings[1];

            switch (deviceConfiguration)
            {
                case "1":
                    columnHeadersExternalDevice = string.Join("\t", "Preamble", 
                        "ViolinConfiguration", "MIMU_violin", "Timestamp",
                        "Acc_xv", "Acc_yv", "Acc_zv", "Gyr_xv", "Gyr_yv", "Gyr_zv", "Mag_xv", "Mag_yv", "Mag_zv",
                        "BowConfiguration", "MIMU_wrist", "Timestamp",
                        "Acc_xw", "Acc_yw", "Acc_zw", "Gyr_xw", "Gyr_yw", "Gyr_zw", "Mag_xw", "Mag_yw", "Mag_zw",
                        "\n");
                    break;
                case "3":
                    columnHeadersExternalDevice = string.Join("\t", "Preamble", 
                        "ViolinConfiguration", "MIMU_violin", "Timestamp",
                        "Acc_xv", "Acc_yv", "Acc_zv", "Gyr_xv", "Gyr_yv", "Gyr_zv", "Mag_xv", "Mag_yv", "Mag_zv",
                        "BowConfiguration", "MIMU_wrist", "Timestamp",
                        "Acc_xw", "Acc_yw", "Acc_zw", "Gyr_xw", "Gyr_yw", "Gyr_zw", "Mag_xw", "Mag_yw", "Mag_zw",
                        "MIMU_frog",
                        "Acc_xf", "Acc_yf", "Acc_zf", "Gyr_xf", "Gyr_yf", "Gyr_zf", "Mag_xf", "Mag_yf", "Mag_zf",
                        "\n");
                    break;
                case "7":
                    columnHeadersExternalDevice = string.Join("\t", "Preamble", 
                        "ViolinConfiguration", "MIMU_violin", "Timestamp",
                        "Acc_xv", "Acc_yv", "Acc_zv", "Gyr_xv", "Gyr_yv", "Gyr_zv", "Mag_xv", "Mag_yv", "Mag_zv",
                        "BowConfiguration", "MIMU_wrist", "Timestamp",
                        "Acc_xw", "Acc_yw", "Acc_zw", "Gyr_xw", "Gyr_yw", "Gyr_zw", "Mag_xw", "Mag_yw", "Mag_zw",
                        "MIMU_frog",
                        "Acc_xf", "Acc_yf", "Acc_zf", "Gyr_xf", "Gyr_yf", "Gyr_zf", "Mag_xf", "Mag_yf", "Mag_zf",
                        "AddressOpticalSensors", "SectionID",
                        "\n");
                    break;
                default:
                    break;
            }
        }

        protected override Dictionary<String, Object> UnpackArrayData(string[] lineSubstrings)
        {
            Dictionary<String, Object> unpackedDataDictionary = new Dictionary<string, object>();

            unpackedDataDictionary.Add("Preamble", Convert.ToInt16(lineSubstrings[0]));
            // Violin
            unpackedDataDictionary.Add("ViolinConfiguration", Convert.ToInt16(lineSubstrings[1]));
            unpackedDataDictionary.Add("MIMU_violin", Convert.ToInt16(lineSubstrings[2]));
            unpackedDataDictionary.Add("Timestamp_v", Convert.ToInt32(lineSubstrings[3]));
            unpackedDataDictionary.Add("Acc_xv", Convert.ToDouble(lineSubstrings[4]));
            unpackedDataDictionary.Add("Acc_yv", Convert.ToDouble(lineSubstrings[5]));
            unpackedDataDictionary.Add("Acc_zv", Convert.ToDouble(lineSubstrings[6]));
            unpackedDataDictionary.Add("Gyr_xv", Convert.ToDouble(lineSubstrings[7]));
            unpackedDataDictionary.Add("Gyr_yv", Convert.ToDouble(lineSubstrings[8]));
            unpackedDataDictionary.Add("Gyr_zv", Convert.ToDouble(lineSubstrings[9]));
            unpackedDataDictionary.Add("Mag_xv", Convert.ToDouble(lineSubstrings[10]));
            unpackedDataDictionary.Add("Mag_yv", Convert.ToDouble(lineSubstrings[11]));
            unpackedDataDictionary.Add("Mag_zv", Convert.ToDouble(lineSubstrings[12]));
            // Bow
            unpackedDataDictionary.Add("BowConfiguration", Convert.ToInt16(lineSubstrings[13]));
            unpackedDataDictionary.Add("MIMU_wrist", Convert.ToInt16(lineSubstrings[14]));
            unpackedDataDictionary.Add("Timestamp_b", Convert.ToInt32(lineSubstrings[15]));
            unpackedDataDictionary.Add("Acc_xw", Convert.ToDouble(lineSubstrings[16]));
            unpackedDataDictionary.Add("Acc_yw", Convert.ToDouble(lineSubstrings[17]));
            unpackedDataDictionary.Add("Acc_zw", Convert.ToDouble(lineSubstrings[18]));
            unpackedDataDictionary.Add("Gyr_xw", Convert.ToDouble(lineSubstrings[19]));
            unpackedDataDictionary.Add("Gyr_yw", Convert.ToDouble(lineSubstrings[20]));
            unpackedDataDictionary.Add("Gyr_zw", Convert.ToDouble(lineSubstrings[21]));
            unpackedDataDictionary.Add("Mag_xw", Convert.ToDouble(lineSubstrings[22]));
            unpackedDataDictionary.Add("Mag_yw", Convert.ToDouble(lineSubstrings[23]));
            unpackedDataDictionary.Add("Mag_zw", Convert.ToDouble(lineSubstrings[24]));

            switch (deviceConfiguration)
            {
                case "1":
                    break;
                case "3":
                    // Frog
                    unpackedDataDictionary.Add("MIMU_frog", Convert.ToInt16(lineSubstrings[25]));
                    unpackedDataDictionary.Add("Acc_xf", Convert.ToDouble(lineSubstrings[26]));
                    unpackedDataDictionary.Add("Acc_yf", Convert.ToDouble(lineSubstrings[27]));
                    unpackedDataDictionary.Add("Acc_zf", Convert.ToDouble(lineSubstrings[28]));
                    unpackedDataDictionary.Add("Gyr_xf", Convert.ToDouble(lineSubstrings[29]));
                    unpackedDataDictionary.Add("Gyr_yf", Convert.ToDouble(lineSubstrings[30]));
                    unpackedDataDictionary.Add("Gyr_zf", Convert.ToDouble(lineSubstrings[31]));
                    unpackedDataDictionary.Add("Mag_xf", Convert.ToDouble(lineSubstrings[32]));
                    unpackedDataDictionary.Add("Mag_yf", Convert.ToDouble(lineSubstrings[33]));
                    unpackedDataDictionary.Add("Mag_zf", Convert.ToDouble(lineSubstrings[34]));
                    break;
                case "7":
                    // Frog
                    unpackedDataDictionary.Add("MIMU_frog", Convert.ToInt16(lineSubstrings[25]));
                    unpackedDataDictionary.Add("Acc_xf", Convert.ToDouble(lineSubstrings[26]));
                    unpackedDataDictionary.Add("Acc_yf", Convert.ToDouble(lineSubstrings[27]));
                    unpackedDataDictionary.Add("Acc_zf", Convert.ToDouble(lineSubstrings[28]));
                    unpackedDataDictionary.Add("Gyr_xf", Convert.ToDouble(lineSubstrings[29]));
                    unpackedDataDictionary.Add("Gyr_yf", Convert.ToDouble(lineSubstrings[30]));
                    unpackedDataDictionary.Add("Gyr_zf", Convert.ToDouble(lineSubstrings[31]));
                    unpackedDataDictionary.Add("Mag_xf", Convert.ToDouble(lineSubstrings[32]));
                    unpackedDataDictionary.Add("Mag_yf", Convert.ToDouble(lineSubstrings[33]));
                    unpackedDataDictionary.Add("Mag_zf", Convert.ToDouble(lineSubstrings[34]));

                    unpackedDataDictionary.Add("AddressOpticalSensors", Convert.ToInt16(lineSubstrings[35]));
                    unpackedDataDictionary.Add("SectionID", Convert.ToInt16(lineSubstrings[36]));
                    break;
                default:
                    break;
            }

            //Console.WriteLine("Acc_xv = " + unpackedDataDictionary["Acc_xv"]);
            //Console.WriteLine("Acc_yv = " + unpackedDataDictionary["Acc_yv"]);
            //Console.WriteLine("Acc_zv = " + unpackedDataDictionary["Acc_zv"]);

            return unpackedDataDictionary;
        }

        protected override void CheckTransmissionMessage()
        {
            throw new NotImplementedException();
        }
    }
}
