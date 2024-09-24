using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace CentralControlSoftware
{
    public class Exoskeleton : ExternalDevice
    {
        // PROPERTIES
        //private string transmitterIPMaster = "192.168.0.66"; //"192.168.0.7"; "192.168.0.32"; "127.0.0.1"
        //private string receiverIPMaster = "192.168.0.66"; //"192.168.0.7"; "192.168.0.32"; "127.0.0.1"
        //private string receiverIPSlave = "192.168.0.22"; //"192.168.0.32", "192.168.0.7"
        //private int receiverPortMaster = 50314;
        //private int receiverPortSlave = 50312;
        //private int transmitterPortMaster = 50311;

        //public string[] completeFilename;

        protected string experimentalCondition;
        protected bool? isFirstPacket;

        // CONSTUCTOR
        // Inherited from ExternalDevice
        public Exoskeleton(string _transmitterIpMaster, int _transmitterPortMaster, string _receiverIpMaster, int _receiverPortMaster,
            string _receiverIpSlave, int _receiverPortSlave) : base(_transmitterIpMaster, _transmitterPortMaster,
                _receiverIpMaster, _receiverPortMaster, _receiverIpSlave, _receiverPortSlave)
        {
            // Override these class-specific properties
            externalDeviceName = "Exoskeleton";
            ackMessageSynchronization = "synch";
        }

        // METHODS

        // Event Handlers: inherited from ExternalDevice

        // Transmitter: inherited from ExternalDevice

        // Receiver: inherited from ExternalDevice

        // Child class-specific methods
        protected override void StartConnection(Dictionary<String, String> ManageConnectionInputParameters)
        {
            //string defaultDirectory = ManageConnectionInputParameters["defaultDirectory"];
            //string experimentalCondition = ManageConnectionInputParameters["experimentalCondition"];
            experimentalCondition = ManageConnectionInputParameters["experimentalCondition"];
            //string filename = ManageConnectionInputParameters["filename"];

            SetTriggerValues(experimentalCondition);
            DataFromExternalDeviceEventArgs args = new DataFromExternalDeviceEventArgs
            {
                name = externalDeviceName,
                isExternalDeviceSynchronized = true
            };
            OnDataFromExternalDevice(args);
        }

        protected override void StopConnection() { }

        protected override void SendDataToSlaveCustomized() { }

        protected override void ReceiveMessagesFromSlave(string receivedMessage) { }

        protected override Dictionary<String, Object> UnpackArrayData(string[] lineSubstrings)
        {
            Dictionary<String, Object> unpackedDataDictionary = new Dictionary<string, object>();
            double udpCommand = 0;

            unpackedDataDictionary.Add("Timestamp", lineSubstrings[0]);
            unpackedDataDictionary.Add("ExoConfig", lineSubstrings[1]);
            
            if (lineSubstrings[1] == "0.000000") // single exo "0.000000"
            {
                unpackedDataDictionary.Add("ShoulderMeasuredAngleActiveTeacher", Convert.ToDouble(lineSubstrings[2])); // desired trajectory shoulder
                unpackedDataDictionary.Add("ElbowMeasuredAngleActiveTeacher", Convert.ToDouble(lineSubstrings[3]));

                unpackedDataDictionary.Add("ShoulderMeasuredAngleActiveLearner", Convert.ToDouble(lineSubstrings[4]));
                unpackedDataDictionary.Add("ElbowMeasuredAngleActiveLearner", Convert.ToDouble(lineSubstrings[5]));

                unpackedDataDictionary.Add("ShoulderMeasuredTorqueLearner", Convert.ToDouble(lineSubstrings[6]));
                unpackedDataDictionary.Add("ShoulderDesiredTorqueLearner", Convert.ToDouble(lineSubstrings[7]));

                unpackedDataDictionary.Add("ShoulderMeasuredAnglePassiveLearner", Convert.ToDouble(lineSubstrings[8]));

                unpackedDataDictionary.Add("ElbowDesiredTorqueLearner", Convert.ToDouble(lineSubstrings[9]));
                unpackedDataDictionary.Add("ElbowMeasuredTorqueLearner", Convert.ToDouble(lineSubstrings[10]));

                unpackedDataDictionary.Add("LoopIteration", Convert.ToDouble(lineSubstrings[11]));

                if (experimentalCondition == "record")
                {
                    udpCommand = 3.0;
                }
                else if (experimentalCondition == "playback")
                {
                    udpCommand = 1.0;
                }
                unpackedDataDictionary.Add("UdpCommand", udpCommand);

                unpackedDataDictionary.Add("ShoulderMeasuredAnglePassiveTeacher", 0.0);
                unpackedDataDictionary.Add("ShoulderMeasuredTorqueTeacher", 0.0);
                unpackedDataDictionary.Add("ShoulderDesiredTorqueTeacher", 0.0);
                unpackedDataDictionary.Add("ElbowMeasuredTorqueTeacher", 0.0);
                unpackedDataDictionary.Add("ElbowDesiredTorqueTeacher", 0.0);
            }
            else if (lineSubstrings[1] == "1.000000") // two exos
            {
                unpackedDataDictionary.Add("ShoulderMeasuredAngleActiveLearner", Convert.ToDouble(lineSubstrings[2]));
                unpackedDataDictionary.Add("ShoulderMeasuredAnglePassiveLearner", Convert.ToDouble(lineSubstrings[3]));

                unpackedDataDictionary.Add("ShoulderMeasuredAngleActiveTeacher", Convert.ToDouble(lineSubstrings[4]));
                unpackedDataDictionary.Add("ShoulderMeasuredAnglePassiveTeacher", Convert.ToDouble(lineSubstrings[5]));

                unpackedDataDictionary.Add("ElbowMeasuredAngleActiveLearner", Convert.ToDouble(lineSubstrings[6]));
                unpackedDataDictionary.Add("ElbowMeasuredAngleActiveTeacher", Convert.ToDouble(lineSubstrings[7]));

                unpackedDataDictionary.Add("ShoulderMeasuredTorqueTeacher", Convert.ToDouble(lineSubstrings[8]));
                unpackedDataDictionary.Add("ShoulderDesiredTorqueTeacher", Convert.ToDouble(lineSubstrings[9]));
                unpackedDataDictionary.Add("ShoulderMeasuredTorqueLearner", Convert.ToDouble(lineSubstrings[10]));
                unpackedDataDictionary.Add("ShoulderDesiredTorqueLearner", Convert.ToDouble(lineSubstrings[11]));

                unpackedDataDictionary.Add("ElbowMeasuredTorqueLearner", Convert.ToDouble(lineSubstrings[12]));
                unpackedDataDictionary.Add("ElbowMeasuredTorqueTeacher", Convert.ToDouble(lineSubstrings[13]));
                unpackedDataDictionary.Add("ElbowDesiredTorqueLearner", Convert.ToDouble(lineSubstrings[14]));
                unpackedDataDictionary.Add("ElbowDesiredTorqueTeacher", Convert.ToDouble(lineSubstrings[15]));

                unpackedDataDictionary.Add("LoopIteration", Convert.ToDouble(lineSubstrings[16]));
                unpackedDataDictionary.Add("UdpCommand", Convert.ToDouble(lineSubstrings[17]));
            }

            if (isFirstPacket != true)
            {
                string configuration = "";
                if (lineSubstrings[1] == "0.000000")
                {
                    configuration = "single exo";
                }
                else if (lineSubstrings[1] == "1.000000")
                {
                    configuration = "two exos";
                }
                GetColumnHeaders(configuration);
                isFirstPacket = true;
            }

            return unpackedDataDictionary;
        }

        protected override void CheckTransmissionMessage()
        {
            if (externalDeviceName == "Exoskeleton" && (commandMessage == "1" || commandMessage == "0"))
            {
                WriteLogMessage("--- WARNING --- In " + externalDeviceName + " the variable commandMessage is not correct (" + commandMessage + "); attempting to retransmit data...");
                if (commandMessage == "1")
                {
                    commandMessage = getStartTrigger();
                }
                else if (commandMessage == "0")
                {
                    commandMessage = getStopTrigger();
                }
                messageToSlaveBytes = Encoding.ASCII.GetBytes(commandMessage);
                transmitterMaster.Send(messageToSlaveBytes, messageToSlaveBytes.Length, this.receiverEpSlave);
                WriteLogMessage("--- WARNING --- Retransmitted " + commandMessage + " to " + externalDeviceName);
                isMessageRetransmitted = true;
            }
            else
            {
                isMessageRetransmitted = false;
            }
        }

        protected void GetColumnHeaders(string configuration)
        {
            if (configuration == "single exo")
            {
                columnHeadersExternalDevice = string.Join("\t", "Timestamp", "ExoConfig",
                    "ShoulderMeasuredAngleActiveTeacher", "ElbowMeasuredAngleActiveTeacher",
                    "ShoulderMeasuredAngleActiveLearner", "ElbowMeasuredAngleActiveLearner",
                    "ShoulderMeasuredTorqueLearner", "ShoulderDesiredTorqueLearner",
                    "ShoulderMeasuredAnglePassiveLearner", "ElbowDesiredTorqueLearner",
                    "ElbowMeasuredTorqueLearner", "LoopIteration",
                    "\n");
            }
            else
            {
                // Learner = E1; Teacher = E2
                columnHeadersExternalDevice = string.Join("\t", "Timestamp", "ExoConfig",
                    "ShoulderMeasuredAngleActiveLearner", "ShoulderMeasuredAnglePassiveLearner",
                    "ShoulderMeasuredAngleActiveTeacher", "ShoulderMeasuredAnglePassiveTeacher",
                    "ElbowMeasuredAngleActiveLearner", "ElbowMeasuredAngleActiveTeacher",
                    "ShoulderMeasuredTorqueTeacher", "ShoulderDesiredTorqueTeacher",
                    "ShoulderMeasuredTorqueLearner", "ShoulderDesiredTorqueLearner",
                    "ElbowMeasuredTorqueLearner", "ElbowMeasuredTorqueTeacher",
                    "ElbowDesiredTorqueLearner", "ElbowDesiredTorqueTeacher",
                    "LoopIteration", "UdpCommand", "\n");
            }
        }

        public void SetTriggerValues(string experimentalCondition)
        {
            // value:5 = low
            // value:6 = high
            if (experimentalCondition == "record")
            {
                triggerStart = "Value:3;";
                triggerEnd = "Value:4;";
            }
            else if (experimentalCondition == "playback")
            {
                triggerStart = "Value:1;";
                triggerEnd = "Value:2;";
            }
            else if (experimentalCondition == "bidirectional")
            {
                triggerStart = "Value:5;";
                triggerEnd = "Value:6;";
            }
            else if (experimentalCondition == "undetermined")
            {
                triggerStart = "none";
                triggerEnd = "none";
            }
            //Console.WriteLine("Trigger start = " + triggerStart + "; trigger stop = " + triggerEnd);
        }
    }
}
