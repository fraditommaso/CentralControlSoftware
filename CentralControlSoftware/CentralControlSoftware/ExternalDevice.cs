using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using static CentralControlSoftware.CCS;
using System.IO;
using System.Diagnostics;

namespace CentralControlSoftware
{
    public abstract class ExternalDevice
    {
        // PROPERTIES

        // 17.01.2023 Possible improvment: create an auxiliary class to store and handle external device properties,
        // because these "global" properties seem to show issues in the assignment (mixing values among different child classes)

        // UDP Socket Properties
        protected string transmitterIpMaster { get; set; }
        protected int transmitterPortMaster { get; set; }
        protected string receiverIpMaster { get; set; }
        protected int receiverPortMaster { get; set; }
        protected string receiverIpSlave { get; set; }
        protected int receiverPortSlave { get; set; }

        protected IPEndPoint transmitterEpMaster;
        protected IPEndPoint receiverEpMaster;
        protected IPEndPoint receiverEpSlave;

        protected UdpClient transmitterMaster;
        protected UdpClient receiverMaster;

        // Threads
        protected Thread transmitterThread;
        protected Thread receiverThread;

        private bool stopTransmitterThread;
        private bool stopReceiverThread;

        private static readonly object receiverLock = new object();

        // Global Attributes
        protected string externalDeviceName;
        protected string columnHeadersExternalDevice;
        protected string columnHeadersCCS = string.Join("\t", "GlobalTimestamp", "Marker");
        protected string columnHeaders;

        protected string filename;
        protected string filePath;
        protected string workingDirectory;
        protected string defaultDirectory;

        protected string ackMessageSynchronization;
        protected string triggerStart;
        protected string triggerEnd;
        protected bool isExtClockActive;
        protected bool isFileCreated;
        protected bool? isTriggerSent;
        protected bool? isDataSaved = null;

        protected string temporalMarker;
        protected int sampleNumber;

        protected byte[] messageToSlaveBytes;
        protected byte[] receivedBytes;
        protected string receivedString;
        protected string builtReceivedString;
        protected float[] receivedFloat;
        protected string receivedDataFormatType = "string";

        static CancellationTokenSource udpReceiveAsync_cts;
        static CancellationToken udpReceiveAsync_ct;

        protected volatile string commandMessage;
        protected volatile string messageToSlave;
        public static volatile string message;
        protected bool? isMessageRetransmitted;
        protected bool isEstimationSamplingFrequency = false;
        protected static volatile List<String> listReceivers = new List<string>();

        protected Dictionary<String, String> ManageConnectionInputParameters = new Dictionary<String, String>();

        protected List<String> receivedStringList = new List<string>();
        protected Dictionary<String, Object> dictionaryData;

        // Declaration of Event Arguments
        public class DataFromExternalDeviceEventArgs : EventArgs
        {
            public bool? isExternalDeviceSynchronized { get; set; } // nullable boolean value (true, false or null)
            public string name { get; set; }
            public Dictionary<String, Object> unpackedDataDictionary { get; set; }
            public int sampleNumber { get; set; }
            public double streamingFrequency { get; set; }
            public bool? isDataSaved { get; set; }
        }

        // Declaration of Event Handler (with previously declared Event Arguments)
        public static event EventHandler<DataFromExternalDeviceEventArgs> DataFromExternalDevice;

        //public DataFromExternalDeviceEventArgs DataFromExternalDeviceArgs;
        public DataFromExternalDeviceEventArgs DataFromExternalDeviceArgs = new DataFromExternalDeviceEventArgs();


        #region CONSTRUCTORS
        // Default Constructor
        // If a derived class does not invoke a base-class constructor explicitly, the default constructor is called implicitly
        public ExternalDevice()
        {
            // Assign Default Properties
            transmitterIpMaster = null;
            transmitterPortMaster = 0;
            receiverIpMaster = null;
            receiverPortMaster = 0;
            receiverIpSlave = null;
            receiverPortSlave = 0;

            // Subscribe to notifications from CCS
            CCS.NotificationRaised += CCS_NotificationRaised;

            //isAnalogDevice = true;
        }

        // Instance Constructors 
        // If a derived class invokes a class-constructor explicitly, the input parameters are assigned to base class properties

        // Case 1 - Transmitting Master and Receiving Slave
        // Description: Master Device is only transmitting data (trigger) and Slave Device is only receiving data (trigger)
        // Properties to be declared: transmitterMaster and receiverSlave
        public ExternalDevice(string _transmitterIpMaster, int _transmitterPortMaster,
            string _receiverIpSlave, int _receiverPortSlave)
        {
            // Assign Properties
            transmitterIpMaster = _transmitterIpMaster;
            transmitterPortMaster = _transmitterPortMaster;
            receiverIpSlave = _receiverIpSlave;
            receiverPortSlave = _receiverPortSlave;

            // Subscribe to notifications from CCS
            CCS.NotificationRaised += CCS_NotificationRaised;
        }

        // Case 2 - Transmitting and Receiving Master and Receiving Slave
        // Description: Master Device is transmitting data (trigger) and receiving data (data) and Slave Device is only receiving data (trigger)
        // Properties to be declared: transmitterMaster, receiverMaster and receiverSlave
        public ExternalDevice(string _transmitterIpMaster, int _transmitterPortMaster,
            string _receiverIpMaster, int _receiverPortMaster,
            string _receiverIpSlave, int _receiverPortSlave)
        {
            // Assign Properties
            transmitterIpMaster = _transmitterIpMaster;
            transmitterPortMaster = _transmitterPortMaster;
            receiverIpMaster = _receiverIpMaster;
            receiverPortMaster = _receiverPortMaster;
            receiverIpSlave = _receiverIpSlave;
            receiverPortSlave = _receiverPortSlave;

            // Subscribe to notifications from CCS
            CCS.NotificationRaised += CCS_NotificationRaised;

            //// Instantiate DataFromExternalDevice event arguments
            //DataFromExternalDeviceArgs = new DataFromExternalDeviceEventArgs();
        }
        #endregion

        // METHODS

        // Event Handlers
        protected virtual void OnDataFromExternalDevice(DataFromExternalDeviceEventArgs args)
        {
            try
            {
                if (DataFromExternalDevice != null)
                {
                    DataFromExternalDevice(this, args);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        protected virtual void CCS_NotificationRaised(object source, NotificationRaisedEventArgs args)
        {
            if (args.filename != null)
            {
                defaultDirectory = args.defaultDirectory;         
                filename = args.filename;
                workingDirectory = args.workingDirectory;
                string[] absoluteFilePath = { workingDirectory, args.filename + "_" + externalDeviceName + ".txt" };
                filePath = String.Join("\\", absoluteFilePath.ToList());
                //Console.WriteLine("File complete path is: " + filename);
            }

            if (args.messageToSlave != null)
            {
                //messageToSlave = args.messageToSlave;
                message = args.messageToSlave;

                if (message == "1")
                {
                    isTriggerSent = true;
                }
                else if (message == "0")
                {
                    isTriggerSent = false;
                    sampleNumber = 0;
                }
                else
                {
                    isTriggerSent = null;
                }

                if (isTriggerSent != null)
                {
                    WriteLogMessage("(" + externalDeviceName + ")" + ": isTriggerSent = " + isTriggerSent);
                }               
            }

            if (args.isExtClockActive)
            {
                isExtClockActive = args.isExtClockActive;
            }

            if (args.temporalMarker != null)
            {
                temporalMarker = args.temporalMarker;
            }

            if (args.listReceivers != null)
            {
                listReceivers = args.listReceivers;
            }
        }


        #region OVERRIDDEN METHODS
        
        // Manage connection
        protected abstract void StartConnection(Dictionary<String, String> ManageConnectionInputParameters);

        protected abstract void StopConnection();

        // Communication
        protected abstract void SendDataToSlaveCustomized();

        protected abstract void ReceiveMessagesFromSlave(string receivedMessage);

        protected abstract Dictionary<String, Object> UnpackArrayData(string[] lineSubstrings);

        protected abstract void CheckTransmissionMessage();
        #endregion


        #region INHERITED METHODS
        
        // Manage connection
        public void OpenConnection(Dictionary<String, String> ManageConnectionInputParameters) //string defaultDirectory, string comPort = "COM0", string macAddress = "default"
        {
            try
            {
                // Base class logic

                // Transmitter
                if (null != transmitterThread)
                    return;

                if (transmitterIpMaster != null & transmitterPortMaster != 0)
                {
                    //// Debug 09/11/2023
                    //bool alreadyInUse = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().Any(p => p.Port == transmitterPortMaster);
                    //Console.WriteLine("The port " + transmitterPortMaster.ToString() + " is already in use: " + alreadyInUse.ToString());
                    ////bool alreadyInUseIp = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().Any(ip => ip.Address);

                    List<IPEndPoint> activeUdpListeners = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().ToList();
                    Console.WriteLine("List of currently active UDP listeners: " + String.Join(", ", activeUdpListeners));

                    transmitterEpMaster = new IPEndPoint(IPAddress.Parse(transmitterIpMaster), transmitterPortMaster);

                    if (transmitterIpMaster == "127.0.0.1" | transmitterIpMaster == "192.168.43.176")
                    {
                        transmitterMaster = new UdpClient(transmitterEpMaster);
                    }
                    else
                    {
                        transmitterMaster = new UdpClient();
                    }
                }
                transmitterThread = new Thread(TransmitterToSlave);
                transmitterThread.Start();
                transmitterThread.Name = "TransmitterThread_" + externalDeviceName;

                WriteLogMessage("Successfully started " + transmitterThread.Name);

                // Receiver
                if (null != receiverThread)
                    return;

                if (receiverIpMaster != null & receiverPortMaster != 0)
                {
                    receiverEpMaster = new IPEndPoint(IPAddress.Parse(receiverIpMaster), receiverPortMaster);
                    receiverMaster = new UdpClient(receiverEpMaster);
                    receiverThread = new Thread(ReceiverFromSlaveAsync);
                    receiverThread.Start();
                    receiverThread.Name = "ReceiverThread_" + externalDeviceName;
                    WriteLogMessage("Successfully started " + receiverThread.Name);
                }

                if (receiverIpSlave != null & receiverPortSlave != 0)
                {
                    receiverEpSlave = new IPEndPoint(IPAddress.Parse(receiverIpSlave), receiverPortSlave);
                }

                // Child class logic
                StartConnection(ManageConnectionInputParameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Unexpected error in externalDevice (" + externalDeviceName + ") - OpenConnection()");
            }
        }

        public void CloseConnection()
        {
            // Base class logic
            try
            {
                if (transmitterMaster != null) //& transmitterThread != null
                {
                    transmitterMaster.Close();
                    transmitterMaster.Dispose();
                    

                    // Test ()
                    transmitterEpMaster = null;
                    transmitterMaster = null;

                    //// 127.0.0.1:50211 (multimediarecorder transmitter is not closed at this point)
                    //List<IPEndPoint> activeUdpListeners = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().ToList();
                    //Console.WriteLine("List of currently active UDP listeners: " + activeUdpListeners.ToString());
                }

                if (transmitterThread != null)
                {
                    string threadName = transmitterThread.Name.ToString();
                    
                    stopTransmitterThread = true;
                    transmitterThread.Join();
                    stopTransmitterThread = false;
                    transmitterThread = null;

                    WriteLogMessage("Successfully terminated " + threadName);
                }

                //lock (receiverLock)
                //{
                    if (receiverMaster != null) //&receiverThread != null
                    {
                        // Test 09/10/2023
                        udpReceiveAsync_cts.Cancel();
                        //await receiveUdpPacketBytesTask;
                        WriteLogMessage("UDP reception in externalDevice (" + externalDeviceName + ") stopped");

                        receiverMaster.Dispose();
                        receiverMaster.Close();

                        // This goes here
                        receiverEpMaster = null; //the exception is here because receiverMaster has been disposed
                        receiverMaster = null;
                    }

                    // Original
                    if (receiverThread != null)
                    {
                        string threadName = receiverThread.Name.ToString();
                        stopReceiverThread = true;
                        receiverThread.Join();
                        //receiverThread.Abort();
                        stopReceiverThread = false;
                        receiverThread = null;

                        WriteLogMessage("Successfully terminated " + threadName);
                    }
                //}


                //if (receiverMaster != null) //&receiverThread != null
                //{
                //    // Test 09/10/2023
                //    udpReceiveAsync_cts.Cancel();

                //    receiverMaster.Close();
                //    receiverMaster.Dispose();

                //    receiverEpMaster = null;
                //    receiverMaster = null;
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error in externalDevice (" + externalDeviceName + ") - CloseConnection()");
                Console.WriteLine(ex.ToString());
            }

            // Child class logic
            StopConnection();
        }


        // Transmitter
        protected void TransmitterToSlave()
        {
            // TODO: is it really necessary to use a thread? Consider other forms of implementation (recursive methods, async callbacks, events)
            try
            {
                while (!stopTransmitterThread) //true
                {
                    if (transmitterMaster != null) // external device is connected via UDP
                    {
                        if (message == "1")
                        {
                            commandMessage = getStartTrigger();
                        }
                        else if (message == "0")
                        {
                            commandMessage = getStopTrigger();
                            sampleNumber = 0;
                        }
                        else
                        {
                            commandMessage = message;
                        }

                        if (commandMessage != null & listReceivers.Contains(externalDeviceName))
                        {
                            messageToSlaveBytes = Encoding.ASCII.GetBytes(commandMessage);
                            transmitterMaster.Send(messageToSlaveBytes, messageToSlaveBytes.Length, this.receiverEpSlave);

                            WriteLogMessage("[" + transmitterThread.Name.ToString() + "]" +
                                " Sending commandMessage = '" + commandMessage + "'" +
                                " from " + transmitterEpMaster.ToString() +
                                " to " + receiverEpSlave.ToString());

                            //message = null;
                            CheckTransmissionMessage();

                            // THIS "IF" HAS TO STAY BECAUSE CANCELLATION TOKEN SOURCE HAS TO BE CANCELED
                            if (message == "0" & receiverMaster != null)
                            {
                                udpReceiveAsync_cts.Cancel();
                            }
                            message = null;
                        }
                    }
                    else // external device is not connected via UDP
                    {
                        if (message != null) // a message is notified by event sender
                        {
                            commandMessage = message;
                            SendDataToSlaveCustomized();

                            WriteLogMessage("[" + transmitterThread.Name.ToString() + "]" +
                                " Sending message = '" + commandMessage + "'");

                            message = null;
                            CheckTransmissionMessage();
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error in externalDevice (" + externalDeviceName + ") - TransmitterToSlave()");
                Console.WriteLine(ex.ToString());
            }
        }

        // Receiver
        private async Task<byte[]> GetUdpPacketAsync(CancellationToken cancellationToken)
        {
            // Test
            //Console.WriteLine("ExternalDevice - GetUpdPacketAsync: isTriggerSent = " + isTriggerSent.ToString());

            // Asynchronously wait for task (receiverMaster.ReceiveAsync) to complete (i.e., a udp packet is received)
            // or terminate it if cancellation token is raised.
            byte[] datagram = null;
            //var receiveTask = receiverMaster.ReceiveAsync();
            //var tcs = new TaskCompletionSource<bool>();

            //try
            //{
            //    using (cancellationToken.Register ( s => tcs.TrySetResult(true), null))
            //    {
            //        if (receiveTask != await Task.WhenAny(receiveTask, tcs.Task))
            //        {
            //            // Debug
            //            //Console.WriteLine("I AM STUCK HERE");
            //        }
            //        else
            //        {
            //            UdpReceiveResult result = receiveTask.Result;
            //            datagram = result.Buffer;
            //        }
            //    }
            //}
            //catch(Exception ex)
            //{
            //    Console.WriteLine (ex.ToString());
            //    Console.WriteLine("Unexpected error in externalDevice (" + externalDeviceName + ") - GetUdpPacketAsync()");
            //}

            // Test 23/10/2023
            if (receiverMaster != null) 
            {
                var receiveTask = receiverMaster.ReceiveAsync();
                var tcs = new TaskCompletionSource<bool>();

                try
                {
                    using (cancellationToken.Register(s => tcs.TrySetResult(true), null))
                    {
                        if (receiveTask != await Task.WhenAny(receiveTask, tcs.Task))
                        {
                            // Debug
                            //Console.WriteLine("I AM STUCK HERE");
                        }
                        else
                        {
                            UdpReceiveResult result = receiveTask.Result;
                            datagram = result.Buffer;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unexpected error in externalDevice (" + externalDeviceName + ") - GetUdpPacketAsync()");
                    Console.WriteLine("Exception: " + ex.ToString());
                    Console.WriteLine("Stack trace: " + ex.StackTrace.ToString());
                }
            }
            return datagram;
        }

        protected string DecodeIncomingBytes(byte[] receivedBytes, string receivedDataFormat)
        {
            string receivedString = "";
            if (receivedDataFormat == "string")
            {
                receivedString = Encoding.ASCII.GetString(receivedBytes);
            }
            else if (receivedDataFormat == "float")
            {
                float[] receivedFloat = new float[receivedBytes.Length / 4];
                Buffer.BlockCopy(receivedBytes, 0, receivedFloat, 0, receivedBytes.Length);

                receivedString = string.Join("\t", receivedFloat);
                receivedString = string.Join("\t", receivedString, "\r\n");
            }
            else if (receivedDataFormat == "floatAR")
            {
                float[] receivedFloat = new float[receivedBytes.Length];
                Buffer.BlockCopy(receivedBytes, 0, receivedFloat, 0, receivedBytes.Length);

                receivedString = string.Join("\t", receivedFloat);
                receivedString = string.Join("\t", receivedString, "\r\n");
            }
            else
            {
                Console.WriteLine("Unable to unpack received data due to undefined format.\n");
            }
            return receivedString;

        }

        protected double GetReceivingFrequency(string previousTimestamp, string currentTimestamp)
        {
            double streamingFrequency = 0;
            if (previousTimestamp != "NaN")
            {
                int previousTimestampMs = int.Parse(previousTimestamp.Substring(10));
                int currentTimestampMs = int.Parse(currentTimestamp.Substring(10));
                if (currentTimestampMs - previousTimestampMs > 0)
                {
                    streamingFrequency = 1000 / (currentTimestampMs - previousTimestampMs);

                }
                else if ((currentTimestampMs - previousTimestampMs) < 0)
                {
                    streamingFrequency = 1000 / (1000 - previousTimestampMs + currentTimestampMs);
                }
                //Console.WriteLine("Streaming frequency = " + streamingFrequency.ToString());
                //WriteLogMessage("[ReceiverThread (" + externalDeviceName + ")]" +
                //    " Streaming frequency = " + streamingFrequency.ToString());
            }
            return streamingFrequency;
            
        }

        protected async void ReceiverFromSlaveAsync()
        {
            int[] oscClockInt;
            string oscClockString; // = null;
            string currentTimestamp;
            string previousTimestamp = "NaN";
            double streamingFrequency = 0;

            isTriggerSent = null;
            DataFromExternalDeviceArgs.name = externalDeviceName;
            DataFromExternalDeviceArgs.isExternalDeviceSynchronized = false;

            udpReceiveAsync_cts = new CancellationTokenSource();
            udpReceiveAsync_ct = udpReceiveAsync_cts.Token;

            try
            {
                while (!stopReceiverThread & !udpReceiveAsync_cts.Token.IsCancellationRequested) //Opt1: !stopReceiverThread; Opt2: !stopReceiverThread & receiverMaster != null
                {
                    //// Test with lock (does not work)
                    //Task<byte[]>  receiveUdpPacketBytesTask = null;
                    //lock (receiverLock)
                    //{
                    //    receiveUdpPacketBytesTask = GetUdpPacketAsync(udpReceiveAsync_ct);
                    //}


                    Task<byte[]> receiveUdpPacketBytesTask = GetUdpPacketAsync(udpReceiveAsync_ct);

                    // Asynchronously wait for a udp packet to be received by GetUdpPacketAsynch
                    byte[] receivedBytes = await receiveUdpPacketBytesTask;

                    // Test
                    //Console.WriteLine("ExternalDevice - ReceiverFromSlaveAsync: isTriggerSent = " + isTriggerSent.ToString());

                    // Move on when the packet is received
                    if ((isTriggerSent == true | isTriggerSent == null) & receivedBytes != null)
                    {
                        // Decode bytes according to the expected format
                        receivedString = DecodeIncomingBytes(receivedBytes, receivedDataFormatType);

                        // Check if received string packet is a generic message (start trigger has not been sent yet)
                        // or a data packet (start trigger has been sent)
                        if (isTriggerSent == null) // recording has not started yet
                        {
                            // To read messages from an external device, the ack message must be included
                            if (receivedString.Contains(getAckMessageSynchronization()))
                            {
                                ReceiveMessagesFromSlave(receivedString);

                                // Notify synchronization
                                if (DataFromExternalDeviceArgs.isExternalDeviceSynchronized == false)
                                    DataFromExternalDeviceArgs.isExternalDeviceSynchronized = true;
                                OnDataFromExternalDevice(DataFromExternalDeviceArgs);
                                DataFromExternalDeviceArgs.isExternalDeviceSynchronized = null;
                            }
                            else
                            {
                                ReceiveMessagesFromSlave(receivedString);

                                // Notify message
                                //DataFromExternalDeviceArgs.someCustomVariable = true;
                            }
                        }
                        else if (isTriggerSent == true) // recording has started
                        {
                            // Combine received string with other info (global timestamp, GU timestamp or marker)
                            currentTimestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                            builtReceivedString = string.Join("\t", currentTimestamp, temporalMarker);

                            // Estimation of streaming frequency
                            if (isEstimationSamplingFrequency)
                            {
                                streamingFrequency = GetReceivingFrequency(previousTimestamp, currentTimestamp);
                                previousTimestamp = currentTimestamp;
                            }
                            else
                            {
                                streamingFrequency = 0;
                            }

                            // To modify because it is not used the object oscSynch instatiated in CCS
                            if (isExtClockActive)
                            {
                                oscClockInt = OscSynch.getCurrentClock();
                                oscClockString = oscClockInt[0].ToString() + ":" + oscClockInt[1].ToString() + ":" + oscClockInt[2].ToString() +
                                    ":" + oscClockInt[3].ToString() + ":" + oscClockInt[4].ToString() + ":" + oscClockInt[5].ToString();
                                builtReceivedString = string.Join("\t", builtReceivedString, oscClockString);
                            }

                            // Store received strings in a list
                            builtReceivedString = string.Join("\t", builtReceivedString, receivedString);
                            receivedStringList.Add(builtReceivedString);

                            //WriteLogMessage("[" + receiverThread.Name.ToString() + "]" +
                            //    " Received message = '" + builtReceivedString + "'");

                            // Increment sample number
                            sampleNumber++;

                            // Reset temporal marker (otherwise this value will be written for every received packet and not once)
                            if (temporalMarker != "0")
                            {
                                temporalMarker = "0";
                            }

                            // TODO: convert to async method and move after reception of the message to unpack data asynchronously and do something else in the meantime.
                            dictionaryData = ReadIncomingPackets(builtReceivedString);

                            if (externalDeviceName == "MachineLearning")
                            {
                                WriteLogMessage("[" + receiverThread.Name.ToString() + "]" +
                                    " Received message = '" + String.Join(", ", dictionaryData.Select(x => x.Value).ToArray()) + "'");
                            }

                            // Fire new unpacked data event
                            DataFromExternalDeviceArgs.unpackedDataDictionary = dictionaryData;
                            DataFromExternalDeviceArgs.sampleNumber = sampleNumber;
                            DataFromExternalDeviceArgs.streamingFrequency = streamingFrequency;
                            OnDataFromExternalDevice(DataFromExternalDeviceArgs);

                            if (sampleNumber == 1)
                            {
                                DataFromExternalDeviceArgs.isDataSaved = false;
                                OnDataFromExternalDevice(DataFromExternalDeviceArgs); //error is here
                                DataFromExternalDeviceArgs.isDataSaved = null;
                            }
                        }
                    }
                    else if (isTriggerSent == false | receivedBytes == null) //isTriggerSent == false & 
                    {   
                        if (receivedStringList.Count > 0)
                        {
                            // Test
                            await receiveUdpPacketBytesTask;

                            // Create new cancellation token
                            udpReceiveAsync_cts = new CancellationTokenSource();
                            udpReceiveAsync_ct = udpReceiveAsync_cts.Token;

                            WriteDataToFile();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation exception
                Console.WriteLine("UDP reception in externalDevice (" + externalDeviceName + ") canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error in externalDevice (" + externalDeviceName + ") - ReceiverFromSlaveAsync()");
                Console.WriteLine("Exception: " + ex.ToString());
                Console.WriteLine("Stack trace: " + ex.StackTrace.ToString());
            }

            //// Test 09/10/2023
            //if (udpReceiveAsync_cts != null)
            //{
            //    udpReceiveAsync_cts.Cancel();
            //}
        }

        protected Dictionary<String, Object> ReadIncomingPackets(string receivedString)
        {
            Dictionary<String, Object> unpackedDataDictionaryCommon = new Dictionary<string, object>();
            Dictionary<String, Object> unpackedDataDictionaryPartial;
            Dictionary<String, Object> unpackedDataDictionaryFull = new Dictionary<string, object>();


            char[] separator = { '\t' };
            string[] lineSubstrings = receivedString.Split(separator);

            // Unpack UDP packet (shared data)
            unpackedDataDictionaryCommon.Add("GlobalTimestamp", lineSubstrings[0]);
            unpackedDataDictionaryCommon.Add("Marker", Convert.ToInt32(lineSubstrings[1]));
            if (isExtClockActive)
            {
                unpackedDataDictionaryCommon.Add("GU Timestamp", lineSubstrings[2]);
            }

            // Unpack UDP packet (depending on external device)
            try
            {
                unpackedDataDictionaryPartial = UnpackArrayData(lineSubstrings.Skip(unpackedDataDictionaryCommon.Count)
                    .Take(lineSubstrings.Length - (unpackedDataDictionaryCommon.Count)).ToArray());

                unpackedDataDictionaryFull = unpackedDataDictionaryCommon.Concat(unpackedDataDictionaryPartial).GroupBy(d => d.Key)
                    .ToDictionary(d => d.Key, d => d.First().Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return unpackedDataDictionaryFull;

            //// Fire new unpacked data event
            //DataFromExternalDeviceArgs.unpackedDataDictionary = dictionaryData;
            //DataFromExternalDeviceArgs.sampleNumber = sampleNumber;
            //OnDataFromExternalDevice(DataFromExternalDeviceArgs);
        }

        public void WriteDataToFile()
        {
            if (isExtClockActive)
            {
                columnHeaders = string.Join("\t", columnHeadersCCS, "GU Timestamp", columnHeadersExternalDevice);
            }
            else
            {
                columnHeaders = string.Join("\t", columnHeadersCCS, columnHeadersExternalDevice);
            }

            if (receiverThread != null)
            {
                WriteLogMessage("[" + receiverThread.Name.ToString() + "]" +
                    " Saving recorded data.");
            }
            else
            {
                WriteLogMessage("[ReceiverThread in " + externalDeviceName + " ALREADY STOPPED]" +
                    " Attempting to save recorded data...");
            }

            if (receivedStringList.Count > 0)
            { 
                File.AppendAllText(filePath, columnHeaders);
                File.AppendAllText(filePath, string.Join("", receivedStringList));

                receivedStringList.Clear();
                isTriggerSent = null;

                DataFromExternalDeviceArgs.isDataSaved = true;
                OnDataFromExternalDevice(DataFromExternalDeviceArgs);
            }
            else
            { 
                WriteLogMessage("WARNING in " + externalDeviceName + ": nothing to save (receivedListString is empty)");
            }
        }

        // Get values
        protected string getAckMessageSynchronization()
        {
            return ackMessageSynchronization;
        }

        protected string getStartTrigger()
        {
            return triggerStart;
        }

        protected string getStopTrigger()
        {
            return triggerEnd;
        }

        // Utils
        protected void WriteLogMessage(string messageToPrint)
        {
            string logMessage = DateTime.Now.ToString("HH:mm:ss.fff") + " - " + messageToPrint;
            Console.WriteLine(logMessage);
        }

        #endregion

    }
}
