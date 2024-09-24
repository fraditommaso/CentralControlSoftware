using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CentralControlSoftware
{
    public class XsensMTw : ExternalDevice
    {
        // PROPERTIES
        // (analog connection does not require definition of network properties, hence default constructor is used)
        //private string transmitterIPMaster = null;
        //private string receiverIPMaster = null;
        //private string receiverIPSlave = null;
        //private int receiverPortMaster = nan (0);
        //private int receiverPortSlave = nan (0);
        //private int transmitterPortMaster = nan (0);

        public int baudRate;
        public Serial serialPort;

        public XsensMTw() : base()
        {
            externalDeviceName = "XsensMTw";
            triggerStart = "T";
            triggerEnd = "S";

            serialPort = new Serial();
            baudRate = 115200;
        }


        protected override void StartConnection(Dictionary<String, String> ManageConnectionInputParameters)
        {
            string comPort = ManageConnectionInputParameters["comPort"];

            try
            {
                serialPort.Setup(comPort, baudRate);

                DataFromExternalDeviceEventArgs args = new DataFromExternalDeviceEventArgs
                {
                    name = externalDeviceName,
                    isExternalDeviceSynchronized = true
                };
                OnDataFromExternalDevice(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        protected override void StopConnection()
        {
            serialPort.CloseSerialCom();
        }

        protected override void SendDataToSlaveCustomized()
        {
            char triggerMessage = 'T';

            if (commandMessage == "T") //1) messageToSlave; (then) 2) message
            {
                triggerMessage = char.Parse(triggerStart);
            }
            else if (commandMessage == "S")
            {
                triggerMessage = char.Parse(triggerEnd);
            }
            serialPort.SendTrigger(triggerMessage, 1);
            //triggerValue = null;
            //Console.WriteLine("Sent " + triggerMessage + " to analog device.");
        }

        protected override void ReceiveMessagesFromSlave(string receivedMessage) { }

        protected override Dictionary<String, Object> UnpackArrayData(string[] lineSubstrings)
        {
            Dictionary<String, Object> unpackedDataDictionary = new Dictionary<string, object>();

            return unpackedDataDictionary;
        }

        protected override void CheckTransmissionMessage()
        {
            if (externalDeviceName == "XsensMTw" & (commandMessage != "T" & commandMessage != "S"))
            {
                WriteLogMessage("--- WARNING --- In " + externalDeviceName + " the variable commandMessage is not correct (" + commandMessage + "); attempting to retransmit data...");
                if (commandMessage != "T" & isTriggerSent == true)
                {
                    commandMessage = getStartTrigger();
                }
                else if (commandMessage != "S" & isTriggerSent == false)
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
