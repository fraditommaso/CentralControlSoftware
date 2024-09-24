using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CentralControlSoftware
{
    public class ARgames : ExternalDevice
    {
        // PROPERTIES
        //private string transmitterIPMaster = "";
        //private string receiverIPMaster = "";
        //private string receiverIPSlave = "";
        //private int transmitterPortMaster = ;
        //private int receiverPortMaster = ;
        //private int receiverPortSlave = ;


        // CONSTUCTOR
        // Inherited from ExternalDevice
        public ARgames(string _transmitterIpMaster, int _transmitterPortMaster, string _receiverIpMaster, int _receiverPortMaster,
            string _receiverIpSlave, int _receiverPortSlave) : base(_transmitterIpMaster, _transmitterPortMaster,
                _receiverIpMaster, _receiverPortMaster, _receiverIpSlave, _receiverPortSlave)
        {
            externalDeviceName = "ARgames";
            columnHeadersExternalDevice = string.Join("\t", "", "\n");
            receivedDataFormatType = "floatAR";
            ackMessageSynchronization = "sync done";
            triggerStart = "1"; //test, can be changed
            triggerEnd = "0";
        }

        // METHODS

        // Event Handlers: inherited from ExternalDevice

        // Transmitter: inherited from ExternalDevice

        // Receiver: inherited from ExternalDevice

        // Child class-specific methods
        protected override void StartConnection(Dictionary<String, String> ManageConnectionInputParameters)
        {
            DataFromExternalDeviceEventArgs args = new DataFromExternalDeviceEventArgs
            {
                name = externalDeviceName,
                isExternalDeviceSynchronized = true
            };
            OnDataFromExternalDevice(args);
        }

        protected override void StopConnection()
        {

        }

        protected override void SendDataToSlaveCustomized() { }

        protected override void ReceiveMessagesFromSlave(string receivedMessage) 
        {
            Console.WriteLine("Received from slave: " + receivedMessage);
        }

        protected override Dictionary<String, Object> UnpackArrayData(string[] lineSubstrings)
        {
            Dictionary<String, Object> unpackedDataDictionary = new Dictionary<string, object>();

            unpackedDataDictionary.Add("", Convert.ToInt32(lineSubstrings[0]));

            return unpackedDataDictionary;
        }

        protected override void CheckTransmissionMessage()
        {
            throw new NotImplementedException();
        }
    }
}
