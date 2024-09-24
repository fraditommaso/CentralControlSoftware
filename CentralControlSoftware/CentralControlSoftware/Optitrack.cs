using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CentralControlSoftware
{
    public class Optitrack : ExternalDevice
    {
        // PROPERTIES
        // (Connection does not require definition of network properties, hence default constructor is used)
        //private string transmitterIPMaster = null;
        //private string receiverIPMaster = null;
        //private string receiverIPSlave = null;
        //private int receiverPortMaster = nan (0);
        //private int receiverPortSlave = nan (0);
        //private int transmitterPortMaster = nan (0);

        public NatNetClass nat;

        public Optitrack() : base()
        {
            externalDeviceName = "Optitrack";
            triggerStart = "1";
            triggerEnd = "0";

            nat = new NatNetClass();
        }

        protected override void StartConnection(Dictionary<String, String> ManageConnectionInputParameters)
        {
            nat.CreateClient();
            nat.Connect();
            nat.SetRecordingTakeButton_Click();

            DataFromExternalDeviceEventArgs args = new DataFromExternalDeviceEventArgs
            {
                name = externalDeviceName,
                isExternalDeviceSynchronized = true
            };
            OnDataFromExternalDevice(args);
        }

        protected override void StopConnection()
        {
            nat.Disconnect();
        }

        //protected override void SendDataToSlaveCustomized() 
        //{
        //    while (true)
        //    {
        //        if (triggerValue != null)
        //        {
        //            if (triggerValue == "1")
        //            {
        //                nat.RecordButton_Click();
        //            }
        //            else if (triggerValue == "0")
        //            {
        //                nat.StopRecordButton_Click();
        //            }
        //        }
        //        else
        //        {
        //            continue;
        //        }
        //    }
        //}

        protected override void SendDataToSlaveCustomized()
        {
            //WriteLogMessage("[TransmitterThread (" + externalDeviceName + ")]" +
            //    " Sending message = '" + message + "'");
            if (commandMessage == "1") //1) messageToSlave; (then) 2) message
            {
                nat.RecordButton_Click();
            }
            else if (commandMessage == "0")
            {
                nat.StopRecordButton_Click();
            }
        }

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
            if (externalDeviceName == "Optitrack" & (commandMessage != "1" & commandMessage != "0"))
            {
                WriteLogMessage("--- WARNING --- In " + externalDeviceName + " the variable commandMessage is not correct (" + commandMessage + "); attempting to retransmit data...");
                if (commandMessage != "1" & isTriggerSent == true)
                {
                    commandMessage = getStartTrigger();
                }
                else if (commandMessage != "0" & isTriggerSent == false)
                {
                    commandMessage = getStopTrigger();
                }
                SendDataToSlaveCustomized();
                WriteLogMessage("--- WARNING --- Retransmitted " + commandMessage + " to " + externalDeviceName);
                isMessageRetransmitted = true;
            }
            else
            {
                isMessageRetransmitted = false;
            }
        }
    }
}
