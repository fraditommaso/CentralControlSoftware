using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CentralControlSoftware
{
    public class Myro : ExternalDevice
    {

        // PROPERTIES
        //private string transmitterIPMaster = "";
        //private string receiverIPMaster = "";
        //private string receiverIPSlave = "";
        //private int transmitterPortMaster = ;
        //private int receiverPortMaster = ;
        //private int receiverPortSlave = ;

        //private string triggerFromMyro;

        // CONSTUCTOR
        // Inherited from ExternalDevice
        public Myro(string _transmitterIpMaster, int _transmitterPortMaster, string _receiverIpMaster, int _receiverPortMaster,
            string _receiverIpSlave, int _receiverPortSlave) : base(_transmitterIpMaster, _transmitterPortMaster,
                _receiverIpMaster, _receiverPortMaster, _receiverIpSlave, _receiverPortSlave)
        {
            externalDeviceName = "Myro";
            columnHeadersExternalDevice = string.Join("\t", "RemoteTrigger", "\n");
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
            char[] separator = { '\t' };
            string[] lineSubstrings = receivedString.Split(separator);

            dictionaryData = UnpackArrayData(lineSubstrings);

            WriteLogMessage("(Myro) receveid: " + dictionaryData["RemoteTrigger"]);

            DataFromExternalDeviceArgs.name = externalDeviceName;
            DataFromExternalDeviceArgs.unpackedDataDictionary = dictionaryData;
            OnDataFromExternalDevice(DataFromExternalDeviceArgs);
        }

        protected override Dictionary<String, Object> UnpackArrayData(string[] lineSubstrings)
        {
            Dictionary<String, Object> unpackedDataDictionary = new Dictionary<string, object>();

            unpackedDataDictionary.Add("RemoteTrigger", Convert.ToString(lineSubstrings[0]));

            return unpackedDataDictionary;
        }

        protected override void CheckTransmissionMessage()
        {
            throw new NotImplementedException();
        }

    }
}
