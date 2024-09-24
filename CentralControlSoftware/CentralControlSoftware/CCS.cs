using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Globalization;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;

using System.Windows.Forms.DataVisualization.Charting;
using System.Media;
using NatNetML;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;


// AUTHOR

// Francesco Di Tommaso, PhD student
// Università Campus Bio-Medico di Roma, Rome, Italy
// Unit of Advanced Robotics and Human-Centred Technologies


// SOFTWARE VERSIONS

// v02 (v01 was originally developed as a Console Application and then discarded) - October 2022
// First implementation of the platform combining the stand-alone GUIs previously developed for WearableSensors and Exoskeleton integration.
// To add a new class of devices, the following steps must be taken:
//  1. Create the class with its properties and methods
//  2. Instantiate an object from the class
//  3. Add the proper switch-case of the newly created object in the methods buttonConfirmDevices_Click and buttonClearAllSelected_Click
//  4. Add the proper logic to trigger the device in the method SendTrigger

// v03 - 10/10/2022
// Implementation of abstract classes and inheritance to improve code testing, maintenance and re-usability

// v03.2 - 20/10/2022
// Updates to improve maintenance and data unpacking

// v03.3 - 03/11/2022
// Review Meeting Demo

// v03.4 - 08/11/2022
// Integration of Myro table and AR games on Hololens

// v03.5 - 16/01/23
// Debug of Optitrack class

// v0.3.5.2 - 27/02/23
// Minor reviews, mostly related to graphical user interface.

// v04.1 - 08/03/2023
// Modifications of exoskeleton packet structure to receive data in Human-Human scenario

// V05.1 - 31/03/2023
// Testing performances

// V05.2 - 03/04/2023
// Modifications of ExternalDevice class towards more abstraction

// V05.3 - 26/04/2023
// Modifications of Exoskeleton class to comply with two different exo routines (single exo VS two exos).
// Software used for Lessons recording session in Gent.

// V05.4 - 05/07/2023
// Integration of VLC as multimedia player
// Split multimedia class into audiovideorecorder and audiovideoplayer
// TODO: automatically open vlc on second monitor

// V05.5 - 11/09/2023
// Improved Multimedia management and CheckSave

// V05.6 - 23/03/2023
// Fixed automatic data storage issue
// TO BE FIXED: MultimediaRecorder returns exception when using playback
// TO BE FIXED: Bioharness does not disconnect properly (socket not released)

// V05.7 - 04/12/2023
// Changed TestingManager class to retrieve more data from resource consumption

// V05.8 - 20/02/2024
// Implementation of MachineLearning Model and streaming of wearable sensors data to EML

namespace CentralControlSoftware
{
    public partial class CCS : Form
    {
        // Tree View List to store checked elements
        public List<String> treeViewCheckedNodes = new List<String>();

        // Dictionary of instantiated objects
        //public Dictionary<String, Object> selectedDevices = new Dictionary<String, Object>();

        // Custom dictionary of instantiated objects (with class that holds key properties
        public class DeviceProperties
        {
            public object deviceObject { get; set; }
            public bool isDeviceSync { get; set; }
            public bool? isDataSaved { get; set; }

            public DeviceProperties(object deviceObject, bool isDeviceSync)
            {
                this.deviceObject = deviceObject;
                this.isDeviceSync = isDeviceSync;
                this.isDataSaved = null;
            }
        }

        IDictionary<string, DeviceProperties> selectedDevices = new Dictionary<string, DeviceProperties>();

        // Custom dictionary of instantiated objects
        //public class Device

        // Data Handler class to handle incoming data from external device and instantiation of handlers
        public class DataHandler
        {
            public double xValue { get; set; }
            public double yValue { get; set; }
        }

        #region Instantiation of objects from child classes (CUSTOMIZE)
        // If a new class of objects is included, please instantiate the object here and then modify
        // the method buttonConfirmDevices_Click.
        public XsensDOT xsensDot;
        public Bioharness bioharness;
        public Shimmer shimmer;
        public InstrumentedObjects instrumentedObjects;
        public MachineLearning machineLearning;
        public ARgames arGames;
        public Exoskeleton exoskeleton;
        public Myro myro;
        public Serial serialPort;
        public XsensMTw xsensMTw;
        public Optitrack optitrack;
        public OscSynch oscSynch;
        public Metronome metronome;
        public MultimediaRecorder multimediaRecorder;
        public MultimediaPlayer multimediaPlayer;

        public DataHandler shimmerDataHandler;
        public DataHandler bioharnessDataHandler;
        public DataHandler xsensDOTDataHandler;
        public DataHandler instrumentedObjectsDataHandler;
        public DataHandler machineLearningDataHandler;
        public DataHandler arGamesDataHandler;
        public DataHandler exoskeletonDataHandler;
        public DataHandler myroDataHandler;

        public DeviceProperties xsensDotProperties;
        public DeviceProperties bioharnessProperties;
        public DeviceProperties shimmerProperties;
        public DeviceProperties machineLearningProperties;
        public DeviceProperties instrumentedObjectsProperties;
        public DeviceProperties arGamesProperties;
        public DeviceProperties exoskeletonProperties;
        public DeviceProperties myroProperties;
        public DeviceProperties xsensMtwProperties;
        public DeviceProperties optitrackProperties;
        public DeviceProperties multimediaRecorderProperties;
        public DeviceProperties multimediaPlayerProperties;
        public DeviceProperties metronomeProperties;
        public DeviceProperties oscSynchProperties;
        public bool isOscClockActive;
        public string xsensDotStreamingMode;

        public UdpClient machineLearningTx;
        public IPEndPoint machineLearningTxEp;
        public IPEndPoint machineLearningRxEp;

        #endregion

        // Declaration of global variables
        public string subjID;
        public string trialID;
        public string filenameRoot;
        public string experimentalCondition;
        public string defaultDirectory;
        public string workingDirectory;
        public string triggerValue;
        public int temporalMarker = 0;
        public int nExternalDevicesSynchronized;
        public string foldernameData = "Data";
        public string foldernameMedia = "media";
        public string filenameMetronome;
        public string folderpathData;
        public string multimediaPlayerApi = "vlc";
        public int timerInterval = 1000;
        private static readonly object dictLock = new object();
        public string combinedData = "";
        public bool isWearableDataThreadActive;

        // UDP Properties and Threads
        public string transmitterIP = "127.0.0.1";
        public int transmitterPort;
        public IPEndPoint transmitterEP;
        public UdpClient transmitter;
        public UdpClient transmitterToExo;

        public string triggerMessageBinary;
        public byte[] triggerBytesBinary;
        public string triggerMessageToExo;
        public byte[] triggerBytesToExo;

        public Thread multimediaThread;
        public bool isMultimediaThreadAlive = false;
        public bool isExoskeletonThreadAlive = false;
        public Thread wearableSensorsThread;
        public bool stopCombineWearableDataThread = false;

        public class TestingManager
        {
            private List<String> testingLogs = new List<string>();
            private string logMessage;
            private int nClicks;
            private List<String> performanceCounterLogs = new List<string>();
            private string performanceCounterLog;
            public int performanceCounterSample = 0;

            //private static string processName = "Microsoft Visual Studio 2022";
            public static string processName = Process.GetCurrentProcess().ProcessName;

            public PerformanceCounter perfCountCpuProcessorTimeTotal = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            public PerformanceCounter perfCountCpuProcessorUtility = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
            public PerformanceCounter perfCountCpuProcessorTime = new PerformanceCounter("Process", "% Processor Time", processName);
            public PerformanceCounter perCountRamProcessWorkingSet = new PerformanceCounter("Process", "Working Set", processName);
            public PerformanceCounter perCountRamMemoryTot = new PerformanceCounter("Memory", "% Committed Bytes In Use");
            public PerformanceCounter perCountRamMemoryCommBytes = new PerformanceCounter("Memory", "Committed Bytes");

            public void GenericButtonClicked(string buttonName)
            {
                string logString = buttonName + " clicked\n";
                GenerateLogString(logString);
                nClicks++;
            }

            public void GenerateLogString(string logString)
            {
                logMessage = DateTime.Now.ToString("HH:mm:ss.fff") + " - " + logString;
                testingLogs.Add(logMessage);
            }

            public void GenerateTestingLogFile(string filenameTestingLogs)
            {
                // Write Testing Logs to file
                File.AppendAllText(filenameTestingLogs, string.Join("", testingLogs) + "\nNumber of clicks = " + nClicks.ToString() + "\n\n");

                // Write Performance Counter to file
                File.AppendAllText(filenameTestingLogs, "Performance Counter" +
                    "\nGlobalTimestamp\tCounter\t" +
                    "CPU Utility (%)\tCPU Total (%)\tCPU Process (%)\tRAM Total (%)\tRAM Process (%)\tRAM Process (bytes)\tTrigger\n");
                File.AppendAllText(filenameTestingLogs, string.Join("", performanceCounterLogs));
            }

            public void GeneratePerformanceCounterReport(string dateTime, int counter, double cpuUtility, double cpuTotal, double cpuProcess, 
                double ramTotal, double ramProcess, double ramProcessBytes, string trigger)
            {
                if (trigger == null | trigger == "")
                {
                    trigger = "0";
                }
                performanceCounterLog = dateTime + "\t" + counter.ToString() + "\t" + cpuUtility.ToString() + "\t" + cpuTotal.ToString() + "\t" + cpuProcess.ToString() + 
                    "\t" + ramTotal.ToString() + "\t" + ramProcess.ToString() + "\t" + ramProcessBytes.ToString() + "\t" + Int16.Parse(trigger) + "\n";
                performanceCounterLogs.Add(performanceCounterLog);
            }
        }
        public bool isTestingPerformance = true;
        public TestingManager testingManager = new TestingManager();

        public bool isRelease = false;

        // EVENT MANAGER

        // Declaration of Publisher's Event Arguments
        public class NotificationRaisedEventArgs : EventArgs
        {
            public string defaultDirectory { get; set; }
            public string workingDirectory { get; set; }
            public string filename { get; set; }
            public bool isExtClockActive { get; set; }
            public string temporalMarker { get; set; }
            public string messageToSlave { get; set; }
            public List<string> listReceivers { get; set; }
        }

        // Declaration of Publisher's Event Handler (with previously declared Event Arguments) (aka delegate)
        public static event EventHandler<NotificationRaisedEventArgs> NotificationRaised;

        public Stopwatch stopwatch;

        public CCS()
        {
            InitializeComponent();

            // Change the current culture to th-TH.
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
            //Console.WriteLine("CurrentCulture is now {0}.", CultureInfo.CurrentCulture.Name);

            // Get initial information on the system
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " - " + ".NET framework: " + RuntimeInformation.FrameworkDescription.ToString());
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " - " + "current process name: " + Process.GetCurrentProcess().ProcessName);

            // Initialize timers
            timerDateAndTime.Interval = timerInterval;
            timerDateAndTime.Start();

            timerPerformanceCounter.Interval = timerInterval;
            timerPerformanceCounter.Start();

            timerRecording.Interval = timerInterval;
            timerRecording.Start();
            stopwatch = new Stopwatch();

            // Get Default Directory and pre-assign it to Working Directory (in case it is not changed)
            defaultDirectory = GetDefaultDirectory();
            workingDirectory = defaultDirectory;

            folderpathData = string.Join("\\", defaultDirectory, foldernameData);
            Directory.CreateDirectory(folderpathData);

            // Subscribe to event senders
            radioButtonRecord.CheckedChanged += new EventHandler(radioButtons_CheckedChanged);
            radioButtonPlayback.CheckedChanged += new EventHandler(radioButtons_CheckedChanged);
            radioButtonBidirectional.CheckedChanged += new EventHandler(radioButtons_CheckedChanged);
            radioButtonHaptic.CheckedChanged += new EventHandler(radioButtons_CheckedChanged);

            ExternalDevice.DataFromExternalDevice += ExternalDevice_DataFromExternalDevice;

            if (isTestingPerformance) testingManager.GenerateLogString("Application started.\n");
        }

        private void timerDateAndTime_Tick(object sender, EventArgs e)
        {
            this.labelDateAndTime.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss");
        }

        private void timerPerformanceCounter_Tick(object sender, EventArgs e)
        {
            if (isTestingPerformance == true)
            {
                testingManager.performanceCounterSample++;

                // CPU
                double cpuProcessorTimeTotal = testingManager.perfCountCpuProcessorTimeTotal.NextValue();
                double cpuProcessorTime = testingManager.perfCountCpuProcessorTime.NextValue();
                double cpuProcessorUtility = testingManager.perfCountCpuProcessorUtility.NextValue();

                // RAM
                double ramMemoryTot = testingManager.perCountRamMemoryTot.NextValue();
                double ramMemoryCommBytes = testingManager.perCountRamMemoryCommBytes.NextValue();
                double ramProcessWorkingSet = testingManager.perCountRamProcessWorkingSet.NextValue();
                
                double ramMemoryProcess = (double)ramProcessWorkingSet / ramMemoryCommBytes * 100;

                //Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " - " + "PerformanceCounter = " + testingManager.performanceCounterSample +
                //    "; CPU Utility (%) = " + cpuProcessorUtility.ToString() + "; CPU Total (%) = " + cpuProcessorTimeTotal.ToString() + "; CPU Process (%) = " + cpuProcessorTime.ToString() +
                //    "; RAM Total (%) = " + ramMemoryTot.ToString() + "; RAM Process (%) = " + ramMemoryProcess.ToString() + 
                //    "; trigger = " + triggerValue);

                testingManager.GeneratePerformanceCounterReport(DateTime.Now.ToString("HH:mm:ss.fff"), testingManager.performanceCounterSample,
                    cpuProcessorUtility, cpuProcessorTimeTotal, cpuProcessorTime, ramMemoryTot, ramMemoryProcess, ramProcessWorkingSet, triggerValue);
            }
        }

        private void timerRecording_Tick(object sender, EventArgs e)
        {
            TimeSpan ts = this.stopwatch.Elapsed;
            string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
            labelTimerRecording.Text = elapsedTime;
        }

        private string GetDefaultDirectory()
        {
            string workingDirectory = Environment.CurrentDirectory;

            if (!isRelease)
            {
                string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
                defaultDirectory = Directory.GetParent(projectDirectory).FullName;
            }
            else
            {
               string releaseParentDirectory = Directory.GetParent(workingDirectory).FullName;
               defaultDirectory = releaseParentDirectory;
            }
            //PrintLogWindow("The default directory is " + defaultDirectory);
            return defaultDirectory;
        }

        private void PrintLogWindow(string logMessage)
        {
            textBoxWarning.AppendText(DateTime.Now.ToString() + " - " + logMessage + ".\r\n");
        }

        protected virtual void OnNotificationRaised(NotificationRaisedEventArgs args) // Publisher sends raised event to subscribers
        {
            if (NotificationRaised != null)
            {
                NotificationRaised(this, args);
            }
        }

        #region Real-time charts of data from objects  (CUSTOMIZE)
        private void ExternalDevice_DataFromExternalDevice(object sender, ExternalDevice.DataFromExternalDeviceEventArgs e)
        {
            // Check dynamically external device synchronization
            if (e.isExternalDeviceSynchronized == true)
            {
                try
                {
                    foreach (var device in selectedDevices)
                    {
                        if (e.name == device.Key.ToString())
                        {
                            device.Value.isDeviceSync = true;

                            this.BeginInvoke(new Action(() =>
                            {
                                PrintLogWindow(e.name + " is synchronized");
                                CheckDevicesSynchronization();
                            }));
                            break;
                        }
                    }

                    switch (e.name)
                    {
                        case "XsensDOT":
                            this.BeginInvoke(new Action(() =>
                            {
                                labelStatusXsensDOT.Text = "Synchronized";
                                labelStatusXsensDOT.ForeColor = Color.Green;
                            }));
                            break;
                        case "Bioharness":
                            this.BeginInvoke(new Action(() =>
                            {
                                labelStatusBioharness.Text = "Synchronized";
                                labelStatusBioharness.ForeColor = Color.Green;
                            }));
                            break;
                        case "Shimmer":
                            this.BeginInvoke(new Action(() =>
                            {
                                labelStatusShimmer.Text = "Synchronized";
                                labelStatusShimmer.ForeColor = Color.Green;
                            }));
                            break;
                        case "InstrumentedObjects":
                            this.BeginInvoke(new Action(() =>
                            {
                                labelStatusInstrumentedObjects.Text = "Synchronized";
                                labelStatusInstrumentedObjects.ForeColor = Color.Green;
                            }));
                            break;
                        case "MachineLearning":
                            this.BeginInvoke(new Action(() =>
                            {
                                labelStatusMachineLearning.Text = "Synchronized";
                                labelStatusMachineLearning.ForeColor = Color.Green;
                            }));
                            break;
                        case "ARgames":
                            this.BeginInvoke(new Action(() =>
                            {
                                labelStatusARgames.Text = "Synchronized";
                                labelStatusARgames.ForeColor = Color.Green;
                            }));
                            break;
                        case "Exoskeleton":
                            this.BeginInvoke(new Action(() =>
                            {
                                string exoskeletonScenario = "None";
                                string exoskeleton1Status = "None";
                                string exoskeleton2Status = "None";

                                if (experimentalCondition == "playback")
                                {
                                    exoskeletonScenario = "Playback";
                                    exoskeleton1Status = "Connected";
                                    exoskeleton2Status = "Not connected";

                                    labelStatusExoskeleton1.ForeColor = Color.Green;
                                    labelStatusExoskeleton2.ForeColor = Color.Red;

                                    labelExoskeleton1ModeValue.Text = "Haptic";
                                }
                                else if (experimentalCondition == "record")
                                {
                                    exoskeletonScenario = "Record";
                                    exoskeleton1Status = "Connected";
                                    exoskeleton2Status = "Not connected";

                                    labelStatusExoskeleton1.ForeColor = Color.Green;
                                    labelExoskeleton1ModeValue.Text = "Transparent";
                                }
                                else if (experimentalCondition == "haptic")
                                {
                                    exoskeletonScenario = "Haptic";
                                    exoskeleton1Status = "Connected";
                                    exoskeleton2Status = "Connected";

                                    labelStatusExoskeleton1.ForeColor = Color.Green;
                                    labelStatusExoskeleton2.ForeColor = Color.Green;

                                    // TODO: this is not always true, it depends on IUVO/SSSA settings (learner could be the reference for teacher)
                                    labelExoskeleton1ModeValue.Text = "Haptic";
                                    labelExoskeleton2ModeValue.Text = "Transparent";
                                }
                                else if (experimentalCondition == "bidirectional")
                                {
                                    exoskeletonScenario = "Bidirectional";
                                    exoskeleton1Status = "Connected";
                                    exoskeleton2Status = "Connected";
                                    labelStatusExoskeleton1.ForeColor = Color.Green;
                                    labelStatusExoskeleton2.ForeColor = Color.Green;

                                    labelExoskeleton1ModeValue.Text = "Haptic";
                                    labelExoskeleton2ModeValue.Text = "Haptic";
                                }
                                labelExoskeletonScenarioStatus.Text = exoskeletonScenario;
                                labelStatusExoskeleton1.Text = exoskeleton1Status;
                                labelStatusExoskeleton2.Text = exoskeleton2Status;
                            }));
                            break;
                        case "Myro":
                            this.BeginInvoke(new Action(() =>
                            {
                                labelStatusMyro.Text = "Synchronized";
                                labelStatusMyro.ForeColor = Color.Green;
                            }));
                            break;
                        case "XsensMTw":
                            this.BeginInvoke(new Action(() =>
                            {
                                labelStatusXsensMTw.Text = "Synchronized";
                                labelStatusXsensMTw.ForeColor = Color.Green;
                            }));
                            break;
                        case "Optitrack":
                            this.BeginInvoke(new Action(() =>
                            {

                            }));
                            break;
                        case "MultimediaRecorder":
                            this.BeginInvoke(new Action(() =>
                            {
                                labelStatusMultimediaRecorder.Text = "Synchronized";
                                labelStatusMultimediaRecorder.ForeColor = Color.Green;
                            }));
                            break;
                        case "MultimediaPlayer":
                            this.BeginInvoke(new Action(() =>
                            {
                                labelStatusMultimediaPlayer.Text = "Synchronized";
                                labelStatusMultimediaPlayer.ForeColor = Color.Green;
                            }));
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            
            // Feed live charts with incoming unpacked data (to be improved)
            try
            {
                if (e.name != null & e.unpackedDataDictionary != null)
                {
                    switch (e.name)
                    {
                        case "XsensDOT":
                            //if (((int)e.sampleNumber + 1) % (30 * 1) == 0)
                            //{
                            if (selectedDevices.ContainsKey("MachineLearning"))
                            {
                                lock (dictLock)
                                {
                                    combinedData = String.Join("\t", e.unpackedDataDictionary.Select(x => x.Value).ToArray());
                                    //Console.WriteLine(combinedData);
                                }
                            }

                            this.BeginInvoke(new Action(() =>
                                {
                                    xsensDOTDataHandler.xValue = (int)e.sampleNumber;
                                    //Console.WriteLine("sample number " + xsensDOTDataHandler.xValue.ToString());
                                    if (xsensDotStreamingMode == "Custom Mode 1")
                                    {
                                        xsensDOTDataHandler.yValue = (double)e.unpackedDataDictionary["Euler_X"];
                                        UpdateLiveChart(chartXsensDOTData, "xsensDOTEulerX", xsensDOTDataHandler.xValue, xsensDOTDataHandler.yValue, 600);

                                        xsensDOTDataHandler.yValue = (double)e.unpackedDataDictionary["Euler_Y"];
                                        UpdateLiveChart(chartXsensDOTData, "xsensDOTEulerY", xsensDOTDataHandler.xValue, xsensDOTDataHandler.yValue, 600);

                                        xsensDOTDataHandler.yValue = (double)e.unpackedDataDictionary["Euler_Z"];
                                        UpdateLiveChart(chartXsensDOTData, "xsensDOTEulerZ", xsensDOTDataHandler.xValue, xsensDOTDataHandler.yValue, 600);
                                    }
                                    else if (xsensDotStreamingMode == "Rate quantities")
                                    {
                                        xsensDOTDataHandler.yValue = (double)e.unpackedDataDictionary["Acc_X"];
                                        UpdateLiveChart(chartXsensDOTData, "xsensDOTEulerX", xsensDOTDataHandler.xValue, xsensDOTDataHandler.yValue, 600);

                                        xsensDOTDataHandler.yValue = (double)e.unpackedDataDictionary["Acc_Y"];
                                        UpdateLiveChart(chartXsensDOTData, "xsensDOTEulerY", xsensDOTDataHandler.xValue, xsensDOTDataHandler.yValue, 600);

                                        xsensDOTDataHandler.yValue = (double)e.unpackedDataDictionary["Acc_Z"];
                                        UpdateLiveChart(chartXsensDOTData, "xsensDOTEulerZ", xsensDOTDataHandler.xValue, xsensDOTDataHandler.yValue, 600);
                                    }

                                    //labelXsensDOTStreamingFrequencyValue.Text = e.streamingFrequency.ToString();
                                }));
                            //}

                            break;
                        case "Bioharness":

                            //if (((int)e.sampleNumber + 1) % (4 * 1) == 0)
                            //{
                                this.BeginInvoke(new Action(() =>
                                {
                                    bioharnessDataHandler.xValue = (int)e.sampleNumber;
                                    bioharnessDataHandler.yValue = (double)e.unpackedDataDictionary["Heart Rate"];
                                    UpdateLiveChart(chartBioharnessData, "bioharnessHeartRate", bioharnessDataHandler.xValue, bioharnessDataHandler.yValue, 1000);
                                    labelBioharnessHeartRate.Text = bioharnessDataHandler.yValue.ToString();

                                    bioharnessDataHandler.yValue = (double)e.unpackedDataDictionary["Respiratory Rate"];
                                    UpdateLiveChart(chartBioharnessData, "bioharnessRespiratoryRate", bioharnessDataHandler.xValue, bioharnessDataHandler.yValue, 1000);
                                    labelBioharnessRespiratoryRate.Text = bioharnessDataHandler.yValue.ToString();

                                   //labelBioharnessStreamingFrequencyValue.Text = e.streamingFrequency.ToString();
                                }));
                            //}


                            //int subsamplesNumber = ((double[])e.unpackedDataDictionary["ECG"]).Length + 1;
                            //foreach (double dataValue in (double[])e.unpackedDataDictionary["ECG"])
                            //{
                            //    this.BeginInvoke(new Action(() =>
                            //    {
                            //        bioharnessDataHandler.xValue = (int)e.sampleNumber + (int)e.sampleNumber / subsamplesNumber;
                            //        bioharnessDataHandler.yValue = dataValue;
                            //        UpdateLiveChart(chartBioharnessData, "bioharnessECG", bioharnessDataHandler.xValue, bioharnessDataHandler.yValue, 1000);
                            //    }));
                            //    subsamplesNumber--;
                            //}

                            //subsamplesNumber = ((double[])e.unpackedDataDictionary["Breathing"]).Length + 1;
                            //foreach (double dataValue in (double[])e.unpackedDataDictionary["Breathing"])
                            //{
                            //    this.BeginInvoke(new Action(() =>
                            //    {
                            //        bioharnessDataHandler.xValue = (int)e.sampleNumber + (int)e.sampleNumber / subsamplesNumber;
                            //        bioharnessDataHandler.yValue = dataValue;
                            //        UpdateLiveChart(chartBioharnessData, "bioharnessBreathing", bioharnessDataHandler.xValue, bioharnessDataHandler.yValue, 1000);
                            //    }));
                            //    subsamplesNumber--;
                            //}

                            break;
                        case "Shimmer":

                            //if (((int)e.sampleNumber + 1) % (50 * 1) == 0)
                            //{
                                this.BeginInvoke(new Action(() =>
                                {
                                    shimmerDataHandler.xValue = (int)e.sampleNumber;
                                    shimmerDataHandler.yValue = (double)e.unpackedDataDictionary["GSR"];
                                    UpdateLiveChart(chartShimmerData, "shimmerGSR", shimmerDataHandler.xValue, shimmerDataHandler.yValue, 600);
                                    //labelShimmerStreamingFrequencyValue.Text = e.streamingFrequency.ToString();
                                }));
                            //}


                            //// Test fake machine learning model
                            //if ((shimmerDataHandler.xValue + 1) % (50 * 15) == 0)
                            //{
                            //    this.BeginInvoke(new Action(() =>
                            //    {
                            //        machineLearningDataHandler.xValue = (int)e.sampleNumber;
                            //        Random randomBinary = new Random();
                            //        machineLearningDataHandler.yValue = randomBinary.Next(0, 2);
                            //        UpdateLiveChart(chartMachineLearningData, "machineLearningModel", machineLearningDataHandler.xValue, machineLearningDataHandler.yValue, 1000);
                            //    }));
                            //}
                            //else if ((shimmerDataHandler.xValue + 1) % (50 * 1) == 0)
                            //{
                            //    this.BeginInvoke(new Action(() =>
                            //    {
                            //        machineLearningDataHandler.xValue = (int)e.sampleNumber;
                            //        UpdateLiveChart(chartMachineLearningData, "machineLearningModel", machineLearningDataHandler.xValue, machineLearningDataHandler.yValue, 1000);
                            //    }));
                            //}

                            break;
                        case "MachineLearning":
                            this.BeginInvoke(new Action(() =>
                            {
                                machineLearningDataHandler.xValue = (int)e.sampleNumber;
                                machineLearningDataHandler.yValue = (double)e.unpackedDataDictionary["Engagement"];
                                UpdateLiveChart(chartMachineLearningData, "machineLearningEngagement", machineLearningDataHandler.xValue, machineLearningDataHandler.yValue, 8);
                                //Console.WriteLine("Engagement = " + machineLearningDataHandler.yValue.ToString());
                            }));
                            break;
                        case "InstrumentedObjects":
                            int instrumentedObjectsWindowLength = 1000;
                            this.BeginInvoke(new Action(() =>
                            {
                                instrumentedObjectsDataHandler.xValue = (int)e.sampleNumber;
                                instrumentedObjectsDataHandler.yValue = (double)e.unpackedDataDictionary["Acc_xv"];
                                UpdateLiveChart(chartInstrumentedObjectsData, "instrumentedObjectsAcc_xv", instrumentedObjectsDataHandler.xValue, instrumentedObjectsDataHandler.yValue, instrumentedObjectsWindowLength);

                                instrumentedObjectsDataHandler.yValue = (double)e.unpackedDataDictionary["Acc_yv"];
                                UpdateLiveChart(chartInstrumentedObjectsData, "instrumentedObjectsAcc_yv", instrumentedObjectsDataHandler.xValue, instrumentedObjectsDataHandler.yValue, instrumentedObjectsWindowLength);

                                instrumentedObjectsDataHandler.yValue = (double)e.unpackedDataDictionary["Acc_zv"];
                                UpdateLiveChart(chartInstrumentedObjectsData, "instrumentedObjectsAcc_zv", instrumentedObjectsDataHandler.xValue, instrumentedObjectsDataHandler.yValue, instrumentedObjectsWindowLength);
                            }));
                            break;
                        case "Exoskeleton":
                            int exoskeletonWindowLength = 800;

                            this.BeginInvoke(new Action(() =>
                            {
                                exoskeletonDataHandler.xValue = (int)e.sampleNumber;

                                if (exoskeletonDataHandler.xValue < 20)
                                {
                                    double exoskeletonScenarioValue;
                                    string exoskeletonScenario = "None";
                                    string exoskeleton1Status = "None";
                                    string exoskeleton2Status = "None";

                                    exoskeletonScenarioValue = (double)e.unpackedDataDictionary["UdpCommand"];

                                    if (exoskeletonScenarioValue == 1 || exoskeletonScenarioValue == 2)
                                    {
                                        exoskeletonScenario = "Playback";
                                        exoskeleton1Status = "Connected";
                                        exoskeleton2Status = "Not connected";

                                        labelStatusExoskeleton1.ForeColor = Color.Green;
                                        labelStatusExoskeleton2.ForeColor = Color.Red;

                                        labelExoskeleton1ModeValue.Text = "Haptic";
                                        //labelExoskeleton2ModeValue.Text = "Transparent";
                                    }
                                    else if (exoskeletonScenarioValue == 3 || exoskeletonScenarioValue == 4)
                                    {
                                        if (experimentalCondition == "record")
                                        {
                                            exoskeletonScenario = "Record";
                                            exoskeleton1Status = "Connected";
                                            exoskeleton2Status = "Not connected";
                                        }
                                        else if (experimentalCondition == "haptic")
                                        {
                                            exoskeletonScenario = "Haptic";
                                            exoskeleton1Status = "Connected";
                                            exoskeleton2Status = "Connected";
                                        }

                                        labelStatusExoskeleton1.ForeColor = Color.Green;
                                        labelStatusExoskeleton2.ForeColor = Color.Green;

                                        labelExoskeleton1ModeValue.Text = "Transparent";
                                        //labelExoskeleton2ModeValue.Text = "Haptic";
                                    }
                                    else if (exoskeletonScenarioValue == 5 || exoskeletonScenarioValue == 6)
                                    {
                                        exoskeletonScenario = "Bidirectional";
                                        exoskeleton1Status = "Connected";
                                        exoskeleton2Status = "Connected";
                                        labelStatusExoskeleton1.ForeColor = Color.Green;
                                        labelStatusExoskeleton2.ForeColor = Color.Green;

                                        labelExoskeleton1ModeValue.Text = "Haptic";
                                        labelExoskeleton2ModeValue.Text = "Haptic";
                                    }
                                    labelExoskeletonScenarioStatus.Text = exoskeletonScenario;
                                    labelStatusExoskeleton1.Text = exoskeleton1Status;
                                    labelStatusExoskeleton2.Text = exoskeleton2Status;

                                    //Console.WriteLine("Exoskeleton - Updating Status info");
                                }
                                
                                if (exoskeletonDataHandler.xValue % 1000 == 0)
                                {
                                    labelExoskeletonStreamingFrequencyValue.Text = e.streamingFrequency.ToString();
                                }

                                // Plot data on live chart every x samples
                                int stepChart = 2;
                                if (exoskeletonDataHandler.xValue % stepChart == 0)
                                {
                                    // Chart Exoskeleton 1 (Learner)
                                    exoskeletonDataHandler.yValue = (double)e.unpackedDataDictionary["ShoulderMeasuredAngleActiveLearner"];
                                    UpdateLiveChart(chartExoskeletonData, "exoskeleton1ShoulderMeasuredAngleActive", exoskeletonDataHandler.xValue, exoskeletonDataHandler.yValue, exoskeletonWindowLength);

                                    exoskeletonDataHandler.yValue = (double)e.unpackedDataDictionary["ShoulderMeasuredAnglePassiveLearner"];
                                    UpdateLiveChart(chartExoskeletonData, "exoskeleton1ShoulderMeasuredAnglePassive", exoskeletonDataHandler.xValue, exoskeletonDataHandler.yValue, exoskeletonWindowLength);

                                    exoskeletonDataHandler.yValue = (double)e.unpackedDataDictionary["ElbowMeasuredAngleActiveLearner"];
                                    UpdateLiveChart(chartExoskeletonData, "exoskeleton1ElbowMeasuredAngleActive", exoskeletonDataHandler.xValue, exoskeletonDataHandler.yValue, exoskeletonWindowLength);

                                    exoskeletonDataHandler.yValue = (double)e.unpackedDataDictionary["ShoulderMeasuredTorqueLearner"];
                                    UpdateLiveChart(chartExoskeletonData, "exoskeleton1ShoulderMeasuredTorque", exoskeletonDataHandler.xValue, exoskeletonDataHandler.yValue, exoskeletonWindowLength);

                                    exoskeletonDataHandler.yValue = (double)e.unpackedDataDictionary["ShoulderDesiredTorqueLearner"];
                                    UpdateLiveChart(chartExoskeletonData, "exoskeleton1ShoulderDesiredTorque", exoskeletonDataHandler.xValue, exoskeletonDataHandler.yValue, exoskeletonWindowLength);

                                    exoskeletonDataHandler.yValue = (double)e.unpackedDataDictionary["ElbowMeasuredTorqueLearner"];
                                    UpdateLiveChart(chartExoskeletonData, "exoskeleton1ElbowMeasuredTorque", exoskeletonDataHandler.xValue, exoskeletonDataHandler.yValue, exoskeletonWindowLength);

                                    exoskeletonDataHandler.yValue = (double)e.unpackedDataDictionary["ElbowDesiredTorqueLearner"];
                                    UpdateLiveChart(chartExoskeletonData, "exoskeleton1ElbowDesiredTorque", exoskeletonDataHandler.xValue, exoskeletonDataHandler.yValue, exoskeletonWindowLength);

                                    // Chart Exoskeleton 2 (Teacher)
                                    exoskeletonDataHandler.yValue = (double)e.unpackedDataDictionary["ShoulderMeasuredAngleActiveTeacher"];
                                    UpdateLiveChart(chartExoskeletonData, "exoskeleton2ShoulderMeasuredAngleActive", exoskeletonDataHandler.xValue, exoskeletonDataHandler.yValue, exoskeletonWindowLength);

                                    exoskeletonDataHandler.yValue = (double)e.unpackedDataDictionary["ShoulderMeasuredAnglePassiveTeacher"];
                                    UpdateLiveChart(chartExoskeletonData, "exoskeleton2ShoulderMeasuredAnglePassive", exoskeletonDataHandler.xValue, exoskeletonDataHandler.yValue, exoskeletonWindowLength);

                                    exoskeletonDataHandler.yValue = (double)e.unpackedDataDictionary["ElbowMeasuredAngleActiveTeacher"];
                                    UpdateLiveChart(chartExoskeletonData, "exoskeleton2ElbowMeasuredAngleActive", exoskeletonDataHandler.xValue, exoskeletonDataHandler.yValue, exoskeletonWindowLength);

                                    exoskeletonDataHandler.yValue = (double)e.unpackedDataDictionary["ShoulderMeasuredTorqueTeacher"];
                                    UpdateLiveChart(chartExoskeletonData, "exoskeleton2ShoulderMeasuredTorque", exoskeletonDataHandler.xValue, exoskeletonDataHandler.yValue, exoskeletonWindowLength);

                                    exoskeletonDataHandler.yValue = (double)e.unpackedDataDictionary["ShoulderDesiredTorqueTeacher"];
                                    UpdateLiveChart(chartExoskeletonData, "exoskeleton2ShoulderDesiredTorque", exoskeletonDataHandler.xValue, exoskeletonDataHandler.yValue, exoskeletonWindowLength);

                                    exoskeletonDataHandler.yValue = (double)e.unpackedDataDictionary["ElbowMeasuredTorqueTeacher"];
                                    UpdateLiveChart(chartExoskeletonData, "exoskeleton2ElbowMeasuredTorque", exoskeletonDataHandler.xValue, exoskeletonDataHandler.yValue, exoskeletonWindowLength);

                                    exoskeletonDataHandler.yValue = (double)e.unpackedDataDictionary["ElbowDesiredTorqueTeacher"];
                                    UpdateLiveChart(chartExoskeletonData, "exoskeleton2ElbowDesiredTorque", exoskeletonDataHandler.xValue, exoskeletonDataHandler.yValue, exoskeletonWindowLength);
                                }
                            }));
                            break;
                        case "Myro":
                            if ((string)e.unpackedDataDictionary["RemoteTrigger"] == "1")
                            {
                                this.BeginInvoke(new Action(() =>
                                {
                                    labelStatusMyro.Text = "Recording";
                                    labelStatusMyro.ForeColor = Color.Orange;
                                    buttonStart.PerformClick();
                                }));
                            }
                            else if ((string)e.unpackedDataDictionary["RemoteTrigger"] == "0")
                            {
                                this.BeginInvoke(new Action(() =>
                                {
                                    labelStatusMyro.Text = "Synchronized";
                                    labelStatusMyro.ForeColor = Color.Green;
                                    buttonStop.PerformClick();
                                }));
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            // Check dynamically if data from each selected device has been saved
            // TODO: this has to be changed because at every notification the for loop is performed, even if isDataSaved will always be false until the stop trigger is sent
            if (e.isDataSaved != null)
            {
                try
                {
                    foreach (var device in selectedDevices)
                    {
                        if (e.name == device.Key.ToString()) // this only executes once
                        {
                            device.Value.isDataSaved = e.isDataSaved;
                            //Console.WriteLine("Changed " + e.name.ToString() + " properties: isDataSave = " + device.Value.isDataSaved.ToString());
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        private void UpdateLiveChart(Chart chart, string lineSeries, double xValue, double yValue, int windowLength)
        {
            try
            {
                if (windowLength != 0)
                {
                    //Console.WriteLine("coordinates in " + lineSeries + ": (" + xValue.ToString() + "; " + yValue.ToString() + ")");
                    if (chart.Series[lineSeries].Points.Count < windowLength)
                    {
                        if (chart.Series[lineSeries].Points.Count != 0)
                        {
                            if (chart.Series[lineSeries].Points[0].XValue > 10)
                            {
                                Console.WriteLine("Removing in " + lineSeries + ": (" + chart.Series[lineSeries].Points[0].XValue.ToString() + "; " + chart.Series[lineSeries].Points[0].YValues.ToString() + ")");
                                chart.Series[lineSeries].Points.RemoveAt(0);
                            }
                        }
                    }
                    else if (chart.Series[lineSeries].Points.Count > windowLength)
                    {
                        chart.Series[lineSeries].Points.RemoveAt(0);
                        chart.ResetAutoValues();
                    }
                    chart.Series[lineSeries].Points.AddXY(xValue, yValue);
                }
                else //if (windowLength == 0)
                {
                    //if (chart.Series[lineSeries].Points != null)
                    chart.Series[lineSeries].Points.Clear();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void SetLiveChart(Chart chart, string lineSeries) //ChartArea chartArea
        {
            //chartArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            //chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;

            chart.Series[lineSeries].Enabled = true;
            chart.Series[lineSeries].BorderColor = Color.Black;
            chart.Series[lineSeries].BorderWidth = 4;

            chart.Legends[lineSeries].Enabled = true;

        }

        private void CombineWearableData(string model)
        {
            while (!stopCombineWearableDataThread)
            {
                string dataString = null;
                //Console.WriteLine("--------- Waiting for combinedData before lock...");
                lock (dictLock)
                {
                    if (model == "Global")
                    {

                    }
                    else if (model == "Motion")
                    {

                    }
                    else if (model == "Biometric")
                    {

                    }
                    else if (model == "Wrist Motion")
                    {

                    }
                    dataString = combinedData;
                    combinedData = "";
                }

                if (!string.IsNullOrEmpty(dataString) & triggerValue == "1")
                {
                    SendWearableData(dataString);
                    //Console.WriteLine("Sending " + dataString);
                }
            }
        }

        private void SendWearableData(string data)
        {
            //NotificationRaisedEventArgs args = new NotificationRaisedEventArgs
            //{
            //    messageToSlave = data,
            //    listReceivers = "MachineLearning".Split().ToList()
            //};
            //OnNotificationRaised(args);

            //Console.WriteLine("Sending " + data);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            machineLearningTx.Send(dataBytes, dataBytes.Length, machineLearningRxEp);
        }
        #endregion

        private string CheckDevicesSynchronization()
        {
            nExternalDevicesSynchronized = 0;
            string syncDevices = "";

            foreach (var device in selectedDevices)
            {
                syncDevices = syncDevices + device.Key.ToString() + ": ";
                if (device.Value.isDeviceSync)
                {
                    syncDevices = syncDevices + device.Value.isDeviceSync.ToString();
                    nExternalDevicesSynchronized++;
                }
                syncDevices = syncDevices + "\n";
            }

            if (nExternalDevicesSynchronized == selectedDevices.Count & nExternalDevicesSynchronized != 0)
            {
                groupBoxCommandsRecording.Enabled = true;
            }

            return syncDevices;
        }

        private string CheckDataSave()
        {
            string savedData = "";
            string saveStatus = "";
            
            foreach (var device in selectedDevices)
            {
                savedData = savedData + device.Key.ToString() + ": ";

                if (device.Value.isDataSaved != null)
                {
                    saveStatus = device.Value.isDataSaved.ToString();
                }
                else
                {
                    saveStatus = "null";
                }
                savedData = savedData + saveStatus + "\n";
            }
            return savedData;
        }

        private void ChangeAppearanceCheckSaveButton(bool isToHighlight)
        {
            if (isToHighlight)
            {
                buttonCheckSave.FlatStyle = FlatStyle.Flat;
                buttonCheckSave.FlatAppearance.BorderColor = Color.Red;
            }
            else
            {
                buttonCheckSave.FlatStyle = FlatStyle.System;
            }
        }

        // MAIN MENU
        #region Command Menu
        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            try
            {
                folderBrowserDialogDefault.Description = "Please select the folder where you want to save your data";
                if (folderBrowserDialogDefault.ShowDialog() == DialogResult.OK)
                {
                    foldernameData = folderBrowserDialogDefault.SelectedPath;
                    PrintLogWindow("Changed storage folder to " + foldernameData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            if (isTestingPerformance) testingManager.GenericButtonClicked("Button Browse");
        }

        private void buttonCheckSync_Click(object sender, EventArgs e)
        {
            MessageBox.Show(CheckDevicesSynchronization(), "Synchronized devices");

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button CheckSync");
            }
        }

        private void buttonCheckSync_MouseHover(object sender, EventArgs e)
        {
            toolTipSync.Show("Press this button to see the list of synchronized devices", buttonCheckSync);
        }

        private void buttonCheckSave_Click(object sender, EventArgs e)
        {
            // DEBUG 16/11/23: MultimediaRecorder does not open when experimentalCondition == playback & bioharness != null
            // In this condition, the method CheckSaveMultimediaFiles will return "False". 
            // TEMPORARY SOLUTION: disable instatiation of multimediaRecorder in playback condition and avoid checking
            // if multimediaRecorder files have been saved successfully
            if (multimediaRecorder != null & experimentalCondition != "playback")
            {
                multimediaRecorder.CheckSaveMultimediaFiles();
            }

            MessageBox.Show(CheckDataSave(), "Saved data");

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button CheckSave");
            }

            //ChangeAppearanceCheckSaveButton(false);
        }

        private void buttonCheckSave_MouseHover(object sender, EventArgs e)
        {
            toolTipCheckSavedData.Show("Press this button to see the list of devices whose data have been successfully saved.", buttonCheckSave);
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (filenameRoot == null)
                filenameRoot = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "_" + subjID + "_" + trialID;
            
            triggerValue = "1";
            //isWearableDataThreadActive = true;

            NotificationRaisedEventArgs args = new NotificationRaisedEventArgs
            {
                defaultDirectory = defaultDirectory,
                workingDirectory = folderpathData,
                filename = filenameRoot,
                messageToSlave = triggerValue,
                isExtClockActive = isOscClockActive,
                temporalMarker = temporalMarker.ToString(),
                listReceivers = selectedDevices.Keys.ToList(),
            };
            OnNotificationRaised(args);

            PrintLogWindow("Collecting data from " + string.Join(", ", selectedDevices.Keys));
            //textBoxWarning.AppendText(DateTime.Now.ToString() + " - " + "Recording on ");
            //textBoxWarning.AppendText(string.Join(", ", selectedDevices.Keys));
            //textBoxWarning.AppendText("\r\n");

            buttonStart.Enabled = false;
            buttonSaveData.Enabled = false;

            labelExoskeletonStreamingFrequencyValue.Text = "100";

            stopwatch.Start();
            labelTimerRecording.ForeColor = Color.Red;
            labelFilenameRoot.Text = filenameRoot;

            if (metronome != null)
            {
                if (buttonMetronome.Text == "Disable")
                    metronome.PlayMetronome();
            }

            if (multimediaRecorder != null)
            {
                labelStatusMultimediaRecorder.Text = "Recording";
                labelStatusMultimediaRecorder.ForeColor = Color.Red;
            }

            if (multimediaPlayer != null)
            {
                if (experimentalCondition == "playback")
                {
                    labelStatusMultimediaPlayer.Text = "Playing";
                    labelStatusMultimediaPlayer.ForeColor = Color.Red;
                    labelMultimediaPlayerFileReproduced.Text = multimediaPlayer.GetMultimediaFilename();
                }
            }

            if (isTestingPerformance) testingManager.GenericButtonClicked("Button Start");
        }

        private void buttonStart_MouseHover(object sender, EventArgs e)
        {
            toolTipStartRecording.Show("Press this button to start recording", buttonStart);
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            triggerValue = "0";
            //isWearableDataThreadActive = false;

            NotificationRaisedEventArgs args = new NotificationRaisedEventArgs
            {
                messageToSlave = triggerValue,
                listReceivers = selectedDevices.Keys.ToList(),
            };
            OnNotificationRaised(args);
            temporalMarker = 0;
            buttonStart.Enabled = true;

            // TODO: Create dedicated method with foreach device in selectedDevices? (but deviceDataHandler should be added to deviceDataProperties)
            if (xsensDot != null)
            {
                UpdateLiveChart(chartXsensDOTData, "xsensDOTEulerX", 0, 0, 0);
                UpdateLiveChart(chartXsensDOTData, "xsensDOTEulerY", 0, 0, 0);
                UpdateLiveChart(chartXsensDOTData, "xsensDOTEulerZ", 0, 0, 0);
            }

            if (machineLearning != null)
            {
                UpdateLiveChart(chartMachineLearningData, "machineLearningEngagement", 0, 0, 0);
            }


            //UpdateLiveChart(chartBioharnessData, "bioharnessHeartRate", 0, 0, 0);
            //UpdateLiveChart(chartBioharnessData, "bioharnessRespiratoryRate", 0, 0, 0);
            //UpdateLiveChart(chartShimmerData, "shimmerGSR", 0, 0, 0);


            if (exoskeleton != null)
            {
                UpdateLiveChart(chartExoskeletonData, "exoskeleton1ShoulderMeasuredAngleActive", 0, 0, 0);
                UpdateLiveChart(chartExoskeletonData, "exoskeleton1ShoulderMeasuredAnglePassive", 0, 0, 0);
                UpdateLiveChart(chartExoskeletonData, "exoskeleton1ElbowMeasuredAngleActive", 0, 0, 0);
                UpdateLiveChart(chartExoskeletonData, "exoskeleton1ShoulderMeasuredTorque", 0, 0, 0);
                UpdateLiveChart(chartExoskeletonData, "exoskeleton1ShoulderDesiredTorque", 0, 0, 0);
                UpdateLiveChart(chartExoskeletonData, "exoskeleton1ElbowMeasuredTorque", 0, 0, 0);
                UpdateLiveChart(chartExoskeletonData, "exoskeleton1ElbowDesiredTorque", 0, 0, 0);
                UpdateLiveChart(chartExoskeletonData, "exoskeleton2ShoulderMeasuredAngleActive", 0, 0, 0);
                UpdateLiveChart(chartExoskeletonData, "exoskeleton2ShoulderMeasuredAnglePassive", 0, 0, 0);
                UpdateLiveChart(chartExoskeletonData, "exoskeleton2ElbowMeasuredAngleActive", 0, 0, 0);
                UpdateLiveChart(chartExoskeletonData, "exoskeleton2ShoulderMeasuredTorque", 0, 0, 0);
                UpdateLiveChart(chartExoskeletonData, "exoskeleton2ShoulderDesiredTorque", 0, 0, 0);
                UpdateLiveChart(chartExoskeletonData, "exoskeleton2ElbowMeasuredTorque", 0, 0, 0);
                UpdateLiveChart(chartExoskeletonData, "exoskeleton2ElbowDesiredTorque", 0, 0, 0);
            }

            PrintLogWindow("Stopped recording on " + string.Join(", ", selectedDevices.Keys));
            PrintLogWindow("Saving data in " + filenameRoot);

            buttonCheckSave.Enabled = true;
            buttonSaveData.Enabled = true;
            stopwatch.Stop();
            stopwatch.Reset();
            labelTimerRecording.ForeColor = Color.Black;
            labelFilenameRoot.Text = "filename";

            //Backup solution
            //MessageBox.Show("SAVE DATA MANUALLY!");
            //ChangeAppearanceCheckSaveButton(true);


            labelExoskeletonStreamingFrequencyValue.Text = "None";


            if (metronome != null)
            {
                metronome.StopMetronome();
                buttonMetronome.Text = "Disable";
            }

            if (multimediaRecorder != null)
            {
                labelStatusMultimediaRecorder.Text = "Synchronized";
                labelStatusMultimediaRecorder.ForeColor = Color.Green;
                labelMultimediaRecorderFileRecorded.Text = "None";
            }

            if (multimediaPlayer != null)
            {
                labelStatusMultimediaPlayer.Text = "Synchronized";
                labelStatusMultimediaPlayer.ForeColor = Color.Green;
                labelMultimediaPlayerFileReproduced.Text = "None";
            }

            //// TODO: Dedicated method to clear charts by setting to 0 deviceDataHandler data
            //if (exoskeleton != null)
            //{
            //    exoskeletonDataHandler.xValue = 0;
            //    exoskeletonDataHandler.yValue = 0;
            //}

            //if (multimediaRecorder != null | multimediaPlayer != null | exoskeleton != null)
            //{
            //    groupBoxCommandsRecording.Enabled = false;
            //}

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button Stop");
            }
        }

        private void buttonStop_MouseHover(object sender, EventArgs e)
        {
            toolTipStopRecording.Show("Press this button to stop recording", buttonStop);
        }

        private void buttonSetMarker_Click(object sender, EventArgs e)
        {
            temporalMarker++;
            NotificationRaisedEventArgs args = new NotificationRaisedEventArgs
            {
                temporalMarker = (temporalMarker).ToString()
            };
            OnNotificationRaised(args);

            PrintLogWindow("Sample marked with marker number " + args.temporalMarker + " in recordings");

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button Marker");
            }
        }

        private void buttonSetMarker_MouseHover(object sender, EventArgs e)
        {
            toolTipSetMarker.Show("Set a marker to mark the current sample in the recordings", buttonSetMarker);
        }

        private void buttonSaveData_Click(object sender, EventArgs e)
        {
            foreach (var device in selectedDevices)
            {
                if (device.Value.isDataSaved == true)
                {
                    PrintLogWindow("Data from " + device.Key.ToString() + " have already been saved successfully");
                }
                else if (device.Value.isDataSaved == false)
                {
                    (device.Value.deviceObject as ExternalDevice).WriteDataToFile();
                    PrintLogWindow("WARNING: Data from " + device.Key.ToString() + " have now been saved successfully");                }
            }

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button Save");
            }
        }

        private void buttonSaveData_MouseHover(object sender, EventArgs e)
        {
            toolTipSaveData.Show("Press this button to save data manually", buttonSaveData);
        }

        #endregion

        #region Tab Manager
        private void tabControlCCS_Click(object sender, EventArgs e)
        {
            if (isTestingPerformance) testingManager.GenericButtonClicked("TabControl");
        }
        #endregion

        #region Tab General: Tree View Manager (CUSTOMIZE)
        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        {
            // Updates all child tree nodes recursively.
            foreach (TreeNode node in treeNode.Nodes)
            {
                node.Checked = nodeChecked;

                // If the current (child) node has (other) child nodes, call the CheckAllChildNodes method recursively.
                if (node.Nodes.Count > 0)
                {
                    this.CheckAllChildNodes(node, nodeChecked);
                }

                if (node.Checked)
                {
                    treeViewCheckedNodes.Add(node.FullPath.ToString());
                }
                else
                {
                    treeViewCheckedNodes.Remove(node.FullPath.ToString());
                }
            }
        }

        private void UncheckAllTreeViewDevices(TreeNodeCollection checkedTreeNodes)
        {
            foreach (TreeNode node in checkedTreeNodes)
            {
                node.Checked = false;

                // If the current (child) node has (other) child nodes, call the CheckAllChildNodes method recursively.
                if (node.Nodes.Count > 0)
                {
                    this.CheckAllChildNodes(node, false);
                }
            }
        }

        private void treeViewDevices_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // The code only executes if the user caused the checked state to change.
            if (e.Action != TreeViewAction.Unknown)
            {
                if (e.Node.Nodes.Count > 0)
                {
                    /* Calls the CheckAllChildNodes method, passing in the current 
                    Checked value of the TreeNode whose checked state changed. */
                    this.CheckAllChildNodes(e.Node, e.Node.Checked);
                }
                else
                {
                    if (e.Node.Checked)
                    {
                        treeViewCheckedNodes.Add(e.Node.FullPath.ToString());
                    }
                    else
                    {
                        treeViewCheckedNodes.Remove(e.Node.FullPath.ToString());
                    }
                }
            }
        }

        private void treeViewDevices_Click(object sender, EventArgs e)
        {
            if (isTestingPerformance) testingManager.GenericButtonClicked("Select devices");
        }

        private void buttonConfirmDevices_Click(object sender, EventArgs e)
        {
            foreach (string checkedNode in treeViewCheckedNodes)
            {
                bool deviceFound = false;
                try
                {
                    switch (checkedNode.Substring(checkedNode.LastIndexOf('\\') + 1))
                    {
                        case "XsensDOT":
                            if (xsensDot == null)
                            {
                                xsensDot = new XsensDOT(transmitterIP, 50111, transmitterIP, 50114, transmitterIP, 50112);

                                // Custom dictionary
                                xsensDotProperties = new DeviceProperties(xsensDot, false);
                                selectedDevices.Add("XsensDOT", xsensDotProperties);

                                deviceFound = true;
                                groupBoxXsensDot.Enabled = true;

                                xsensDOTDataHandler = new DataHandler();
                            }
                            break;
                        case "Bioharness":
                            if (bioharness == null)
                            {
                                bioharness = new Bioharness(transmitterIP, 50131, transmitterIP, 50134, transmitterIP, 50132);

                                bioharnessProperties = new DeviceProperties(bioharness, false);
                                selectedDevices.Add("Bioharness", bioharnessProperties);

                                deviceFound = true;
                                groupBoxBioharness.Enabled = true;

                                bioharnessDataHandler = new DataHandler();
                            }
                            break;
                        case "Shimmer":
                            if (shimmer == null)
                            {
                                shimmer = new Shimmer(transmitterIP, 50121, transmitterIP, 50124, transmitterIP, 50122);
                                //selectedDevices.Add("Shimmer", shimmer);
                                
                                shimmerProperties = new DeviceProperties(shimmer, false);
                                selectedDevices.Add("Shimmer", shimmerProperties);
                                
                                deviceFound = true;
                                groupBoxShimmer.Enabled = true;

                                shimmerDataHandler = new DataHandler();
                                //SetLiveChart(this.chartShimmerData, "shimmerGSR");
                                //chartShimmerData.Series["shimmerGSR"].IsVisibleInLegend = false;

                                //machineLearningDataHandler = new DataHandler();
                            }
                            break;
                        case "Instrumented Objects":
                            if (instrumentedObjects == null)
                            {
                                string instrumentedObjectsIp = "192.168.0.4";
                                string transmitterToInstrumentedObjectsIp = "192.168.0.66";

                                instrumentedObjects = new InstrumentedObjects(transmitterToInstrumentedObjectsIp, 50411, transmitterToInstrumentedObjectsIp, 50414, instrumentedObjectsIp, 50412);
                                //selectedDevices.Add("Instrumented Objects", instrumentedObjects);

                                instrumentedObjectsProperties = new DeviceProperties(instrumentedObjects, false);
                                selectedDevices.Add("InstrumentedObjects", instrumentedObjectsProperties);
                                
                                deviceFound = true;
                                groupBoxInstrumentedObject.Enabled = true;
                                //nExternalDevicesSynchronized++;
                                instrumentedObjectsDataHandler = new DataHandler();
                            }
                            break;
                        case "Machine Learning":
                            if (machineLearning == null)
                            {
                                machineLearning = new MachineLearning(transmitterIP, 50611, transmitterIP, 50614, transmitterIP, 50612);
                                //selectedDevices.Add("Machine Learning", machineLearning);

                                machineLearningProperties = new DeviceProperties(machineLearning, false);
                                selectedDevices.Add("MachineLearning", machineLearningProperties);
                                
                                deviceFound = true;
                                groupBoxMachineLearning.Enabled = true;

                                machineLearningDataHandler = new DataHandler();

                                machineLearningTxEp = new IPEndPoint(IPAddress.Parse(transmitterIP), 50619);
                                machineLearningTx = new UdpClient(machineLearningTxEp);
                                machineLearningRxEp = new IPEndPoint(IPAddress.Parse(transmitterIP), 50612);
                            }
                            break;
                        case "AR games":
                            if (arGames == null)
                            {
                                string arGamesIp = "192.168.43.43";
                                string transmitterToARgames = "192.168.43.176";
                                arGames = new ARgames(transmitterToARgames, 9800, transmitterToARgames, 9802, arGamesIp, 9801);
                                //selectedDevices.Add("AR games", arGames);

                                arGamesProperties = new DeviceProperties(arGames, false);
                                selectedDevices.Add("ArGames", arGamesProperties);
                                
                                deviceFound = true;
                                groupBoxARgames.Enabled = true;

                                arGamesDataHandler = new DataHandler();
                            }
                            break;
                        case "Exoskeleton":
                            if (exoskeleton == null)
                            {
                                bool isExoDebug = false;
                                string exoskeletonIp;
                                string transmitterToExoIp;
                                if (isExoDebug)
                                {
                                    exoskeletonIp = "127.0.0.1";
                                    transmitterToExoIp = "127.0.0.1";
                                }
                                else
                                {
                                    exoskeletonIp = "192.168.0.22";
                                    transmitterToExoIp = "192.168.0.66";                                    
                                }
                                int portNumberTriggerTx = 50311;
                                int portNumberTriggerRx = 50312;
                                int portNumberDataRx = 50314;
                                exoskeleton = new Exoskeleton(transmitterToExoIp, portNumberTriggerTx, transmitterToExoIp, portNumberDataRx, exoskeletonIp, portNumberTriggerRx);
                                
                                exoskeletonProperties = new DeviceProperties(exoskeleton, false);
                                selectedDevices.Add("Exoskeleton", exoskeletonProperties);

                                deviceFound = true;

                                groupBoxExperimentalCondition.Enabled = true;
                                groupBoxExoskeleton.Enabled = true;
                                groupBoxExoskeletonImpedance.Enabled = true;

                                exoskeletonDataHandler = new DataHandler();

                                // Alternative (works)
                                Dictionary<String, String> exoskeletonInputParameters = new Dictionary<string, string>();
                                exoskeletonInputParameters.Add("experimentalCondition", experimentalCondition);
                                exoskeleton.OpenConnection(exoskeletonInputParameters);
                            }
                            break;
                        case "Myro":
                            if (myro == null)
                            {
                                string myroIp = "192.168.247.152";
                                string transmitterToMyroIp = "192.168.247.202";
                                myro = new Myro(transmitterToMyroIp, 50711, transmitterToMyroIp, 50714, myroIp, 50712);
                                //selectedDevices.Add("Myro", myro);
                                
                                myroProperties = new DeviceProperties(myro, false);
                                selectedDevices.Add("Myro", myroProperties);
                                
                                deviceFound = true;

                                groupBoxMyro.Enabled = true;

                                myroDataHandler = new DataHandler();
                            }
                            break;
                        case "Metronome":
                            if (metronome == null)
                            {
                                //metronome = new Metronome(workingDirectory, "metronome.wav");
                                //metronome = new Metronome(string.Join("\\", workingDirectory, foldernameMedia), filenameMetronome);
                                metronome = new Metronome();
                                //selectedDevices.Add("Metronome", metronome);
                                
                                metronomeProperties = new DeviceProperties(metronome, true);
                                selectedDevices.Add("Metronome", metronomeProperties);
                                
                                deviceFound = true;
                                groupBoxMetronome.Enabled = true;
                                comboBoxMetronomeTempo.SelectedItem = "60bpm";
                                CheckDevicesSynchronization();
                            }
                            break;
                        case "MTw Awinda":
                            if (xsensMTw == null)
                            {
                                xsensMTw = new XsensMTw();
                                //selectedDevices.Add("MTw Awinda", xsensMTw);

                                xsensMtwProperties = new DeviceProperties(xsensMTw, false);
                                selectedDevices.Add("XsensMTw", xsensMtwProperties);

                                deviceFound = true;
                                groupBoxXsensMTw.Enabled = true;
                            }
                            break;
                        case "Optitrack":
                            if (optitrack == null)
                            {
                                optitrack = new Optitrack();
                                //selectedDevices.Add("Optitrack", optitrack);

                                optitrackProperties = new DeviceProperties(optitrack, false);
                                selectedDevices.Add("Optitrack", optitrackProperties);

                                Dictionary<String, String> optitrackInputParameters = new Dictionary<String, String>();
                                optitrack.OpenConnection(optitrackInputParameters);
                                deviceFound = true;
                            }
                            break;
                        case "OpenSoundControl":
                            if (oscSynch == null)
                            {
                                oscSynch = new OscSynch();
                                oscSynch.InitializeOscSynch();
                                //selectedDevices.Add("OpenSoundControl", oscSynch);

                                oscSynchProperties = new DeviceProperties(oscSynch, true);
                                selectedDevices.Add("OpenSoundControl", oscSynchProperties);

                                deviceFound = true;
                                isOscClockActive = true;
                                //nExternalDevicesSynchronized++;
                            }
                            break;
                        case "MultimediaRecorder":
                            if (multimediaRecorder == null)
                            {
                                multimediaRecorder = new MultimediaRecorder(transmitterIP, 50215, transmitterIP, 50212);
                                //selectedDevices.Add("MultimediaRecorder", multimediaRecorder);
                                
                                multimediaRecorderProperties = new DeviceProperties(multimediaRecorder, false);
                                selectedDevices.Add("MultimediaRecorder", multimediaRecorderProperties);
                                
                                deviceFound = true;
                                groupBoxExperimentalCondition.Enabled = true;
                                groupBoxMultimediaRecorder.Enabled = true;
                                labelStatusMultimediaRecorder.Text = "Connected";
                            }
                            break;
                        case "MultimediaPlayer":
                            if (multimediaPlayer == null)
                            {
                                if (multimediaPlayerApi == "vlc")
                                {
                                    multimediaPlayer = new MultimediaPlayer();
                                }
                                else if (multimediaPlayerApi == "python")
                                {
                                    multimediaPlayer = new MultimediaPlayer(transmitterIP, 50221, transmitterIP, 50222);
                                }

                                multimediaPlayerProperties = new DeviceProperties(multimediaPlayer, true);
                                selectedDevices.Add("MultimediaPlayer", multimediaPlayerProperties);
                                
                                deviceFound = true;
                                groupBoxMultimediaPlayer.Enabled = true;
                                groupBoxExperimentalCondition.Enabled = true;
                                labelStatusMultimediaPlayer.Text = "Connected";
                            }
                            break;
                        default:
                            PrintLogWindow("Cannot find " + checkedNode);
                            deviceFound = false;
                            break;
                    }
                    if (deviceFound)
                    {
                        PrintLogWindow("Successfully created " + checkedNode);
                    }
                }
                catch(Exception ex)
                {
                    PrintLogWindow("Something went wrong with " + checkedNode);
                    PrintLogWindow("Exception: " + ex);
                }
            }

            if (isTestingPerformance) testingManager.GenericButtonClicked("Button Submit devices");
        }

        private void buttonClearAllSelected_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < selectedDevices.Count; i++)
            {
                bool deviceFound = true;
                string device = selectedDevices.ElementAt(i).Key;
                try
                {
                    switch (device.Substring(device.LastIndexOf('.') + 1))
                    {
                        case "XsensDOT":
                            xsensDot = null;
                            groupBoxXsensDot.Enabled = false;
                            buttonConnectXsensDOT.Text = "Connect";
                            labelStatusXsensDOT.Text = "Not connected";
                            xsensDOTDataHandler = null;
                            break;
                        case "Bioharness":
                            bioharness.CloseConnection();
                            bioharnessProperties = null;
                            bioharnessDataHandler = null;
                            bioharness = null;
                            groupBoxBioharness.Enabled = false;
                            buttonConnectBioharness.Text = "Connect";
                            labelStatusBioharness.Text = "Not connected";
                            textBoxBioharnessAddress.Text = "";
                            break;
                        case "Shimmer":
                            shimmer.CloseConnection();
                            shimmerProperties = null;
                            shimmerDataHandler = null;
                            shimmer = null;
                            groupBoxShimmer.Enabled = false;
                            buttonConnectShimmer.Text = "Connect";
                            labelStatusShimmer.Text = "Not connected";
                            labelStatusShimmer.ForeColor = Color.Red;
                            textBoxShimmerPortCom.Text = "";
                            break;
                        case "InstrumentedObjects":
                            instrumentedObjects = null;
                            instrumentedObjectsDataHandler = null;
                            groupBoxInstrumentedObject.Enabled = false;
                            break;
                        case "MachineLearning":
                            machineLearning.CloseConnection();
                            machineLearning = null;
                            groupBoxMachineLearning.Enabled = false;
                            machineLearningDataHandler = null;
                            break;
                        case "AR games":
                            arGames.CloseConnection();
                            arGames = null;
                            arGamesDataHandler = null;
                            groupBoxARgames.Enabled = false;
                            break;
                        case "Exoskeleton":
                            exoskeleton = null;
                            groupBoxExperimentalCondition.Enabled = false;
                            groupBoxExoskeleton.Enabled = false;
                            groupBoxExoskeletonImpedance.Enabled = true;
                            exoskeletonDataHandler = null;
                            break;
                        case "Myro":
                            myro = null;
                            groupBoxMyro.Enabled = false;
                            myroDataHandler = null;
                            break;
                        case "Metronome":
                            if (metronome.isPlaying == false)
                                metronome.StopMetronome();
                            metronome = null;
                            groupBoxMetronome.Enabled = false;
                            break;
                        case "XsensMTw": // MTw Awinda Class
                            xsensMTw = null;
                            break;
                        case "Optitrack": // Optitrack Class
                            //nat = null;
                            optitrack.CloseConnection();
                            optitrack = null;
                            break;
                        case "OpenSoundControl":
                            oscSynch.CloseOscSynch();
                            oscSynch = null;
                            isOscClockActive = false;
                            break;
                        case "MultimediaRecorder":
                            if (isMultimediaThreadAlive)
                                multimediaRecorder.CloseConnection();
                            multimediaRecorder = null;
                            labelStatusMultimediaRecorder.Text = "Not connected";
                            labelMultimediaRecorderFileRecorded.Text = "";
                            groupBoxMultimediaRecorder.Enabled = false;
                            break;
                        case "MultimediaPlayer":
                            if (isMultimediaThreadAlive)
                                multimediaPlayer.CloseConnection();
                            multimediaPlayer = null;
                            labelStatusMultimediaPlayer.Text = "Not connected";
                            labelMultimediaPlayerFileReproduced.Text = "";
                            groupBoxMultimediaPlayer.Enabled = false;
                            break;
                        default:
                            deviceFound = false;
                            PrintLogWindow("Cannot remove " + device.Substring(device.LastIndexOf('.') + 1));
                            break;
                    }
                    if (deviceFound)
                    {
                        PrintLogWindow("Successfully removed " + device.Substring(device.LastIndexOf('.') + 1));
                    }
                }
                catch (Exception ex)
                {
                    PrintLogWindow("Something went wrong with " + device.Substring(device.LastIndexOf('.') + 1));
                    PrintLogWindow("Exception: " + ex);
                }
            }
            selectedDevices.Clear();
            treeViewCheckedNodes.Clear();
            UncheckAllTreeViewDevices(treeViewDevices.Nodes);
            nExternalDevicesSynchronized = 0;
            PrintLogWindow("");
            //groupBoxCommandsRecording.Enabled = false;
            buttonClearInfo.PerformClick();

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button Clear all devices");
            }
        }
        #endregion

        #region Tab General: General Info Manager
        private void textBoxSubjectID_Validated(object sender, EventArgs e)
        {
            subjID = textBoxSubjectID.Text;
            PrintLogWindow("Created subject " + subjID);
            radioButtonRecord.Checked = false;
            radioButtonPlayback.Checked = false;
            radioButtonBidirectional.Checked = false;
            radioButtonHaptic.Checked = false;
        }

        private void textBoxSubjectID_MouseClick(object sender, MouseEventArgs e)
        {
            if (isTestingPerformance) testingManager.GenericButtonClicked("Textbox subjectID");
        }

        private void textBoxTrialID_Validated(object sender, EventArgs e)
        {
            trialID = textBoxTrialID.Text;
            PrintLogWindow("Created trial " + trialID);

            if (radioButtonRecord.Checked == true)
                radioButtonRecord.Checked = false;

            if (radioButtonPlayback.Checked == true)
                radioButtonPlayback.Checked = false;
            
            if (radioButtonBidirectional.Checked == true)
                radioButtonBidirectional.Checked = false;
            
            if (radioButtonHaptic.Checked == true)
                radioButtonHaptic.Checked = false;

            if (multimediaRecorder != null | multimediaPlayer != null | exoskeleton != null)
            {
                groupBoxCommandsRecording.Enabled = false;
            }

            filenameRoot = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "_" + subjID + "_" + trialID;

            NotificationRaisedEventArgs args = new NotificationRaisedEventArgs
            {
                defaultDirectory = defaultDirectory,
                workingDirectory = folderpathData,
                filename = filenameRoot,
            };
            OnNotificationRaised(args);
        }

        private void textBoxTrialID_Click(object sender, EventArgs e)
        {
            if (isTestingPerformance) testingManager.GenericButtonClicked("Textbox trialID");
        }

        private void radioButtons_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;

            //Dictionary<String, String> multimediaInputParameters = new Dictionary<string, string>();
            Dictionary<String, String> exoskeletonInputParameters = new Dictionary<string, string>();

            Dictionary<String, String> multimediaRecorderInputParameters = new Dictionary<string, string>();
            Dictionary<String, String> multimediaPlayerInputParameters = new Dictionary<string, string>();

            try 
            {
                if (isMultimediaThreadAlive | isExoskeletonThreadAlive) //exoskeleton != null) //(multimediaRecorder != null | multimediaPlayer != null | exoskeleton != null)
                {
                    //Impelement this control because the very first time it enters here exoskeleton is not null and the following operations are executed even if nothing has to be cleared
                    if (isExoskeletonThreadAlive)
                    {
                        if (exoskeleton != null)
                        {
                            // Original
                            //exoskeleton.CloseConnection(); // catch exception is here at the second trial
                            //exoskeletonInputParameters.Clear();

                            // Alternative (works)
                            exoskeleton.SetTriggerValues("undetermined");

                            labelStatusExoskeleton1.Text = "Not connected";
                            labelStatusExoskeleton1.ForeColor = Color.Red;

                            labelStatusExoskeleton2.Text = "Not connected";
                            labelStatusExoskeleton2.ForeColor = Color.Red;
                
                            isExoskeletonThreadAlive = false;
                        }
                    }

                    if (isMultimediaThreadAlive)
                    {
                        if (multimediaRecorder != null)
                        {
                            multimediaRecorder.CloseConnection();
                            multimediaRecorderInputParameters.Clear();
                            //isMultimediaThreadAlive = false;
                            //Console.WriteLine("Closing connection to multimedia recorder");
                        }

                        if (multimediaPlayer != null)
                        {
                            multimediaPlayer.CloseConnection();
                            multimediaPlayerInputParameters.Clear();
                            //isMultimediaThreadAlive = false;
                        }
                        isMultimediaThreadAlive = false;
                    }
                    experimentalCondition = "undetermined";
                    //filenameRoot = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Something went wrong when clearing existing options for exoskeleton or multimedia.");
            }

            if (radioButton.Text == "Record" & radioButton.Checked)
            {
                experimentalCondition = "record";
            }
            else if (radioButton.Text == "Haptic" & radioButton.Checked)
            {
                experimentalCondition = "haptic";
            }
            else if (radioButton.Text == "Playback" & radioButton.Checked)
            {
                experimentalCondition = "playback";
            }
            else if (radioButton.Text == "Bidirectional" & radioButton.Checked)
            {
                experimentalCondition = "bidirectional";
            }

            try
            {
                if (experimentalCondition != "undetermined")
                {
                    if (selectedDevices.ContainsKey("MultimediaRecorder") & multimediaRecorder != null)
                    {
                        // DEBUG: TEMPORARY SOLUTION TO FIX EXCEPTION WHEN USING MULTIMEDIA RECORDER IN PLAYBACK CONDITION
                        // SOLUTION: AVOID INSTATIATING MULTIMEDIA RECORDER WHEN EXPERIMENTAL CONDITION = PLAYBACK
                        if (experimentalCondition != "playback")
                        {
                            multimediaRecorderInputParameters.Add("defaultDirectory", defaultDirectory);
                            multimediaRecorderInputParameters.Add("experimentalCondition", experimentalCondition);

                            if (filenameRoot == null)
                            {
                                filenameRoot = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "_" + subjID + "_" + trialID;
                            }
                            string filenameMultimedia = filenameRoot;
                            multimediaRecorderInputParameters.Add("filename", filenameMultimedia);
                            multimediaRecorder.OpenConnection(multimediaRecorderInputParameters);
                            isMultimediaThreadAlive = true;
                            labelMultimediaRecorderFileRecorded.Text = filenameMultimedia;

                            //Console.WriteLine("Opening connection with multimedia recorder");
                        }
                    }

                    if (selectedDevices.ContainsKey("MultimediaPlayer") & multimediaPlayer != null)
                    {
                        if (experimentalCondition == "playback")
                        {
                            multimediaPlayerInputParameters.Add("defaultDirectory", defaultDirectory);
                            multimediaPlayerInputParameters.Add("playerApi", multimediaPlayerApi); // or python
                            multimediaPlayerInputParameters.Add("experimentalCondition", experimentalCondition);
                            multimediaPlayer.OpenConnection(multimediaPlayerInputParameters);
                            isMultimediaThreadAlive = true;
                        }
                    }

                    if (selectedDevices.ContainsKey("Exoskeleton") & exoskeleton != null)
                    {
                        // Original
                        //exoskeletonInputParameters.Add("experimentalCondition", experimentalCondition);
                        //exoskeleton.OpenConnection(exoskeletonInputParameters);

                        // Alternative (works)
                        exoskeleton.SetTriggerValues(experimentalCondition);
                        isExoskeletonThreadAlive = true;
                    }
                }
                PrintLogWindow("Selected option: " + experimentalCondition);

                //groupBoxCommandsRecording.Enabled = true;
                CheckDevicesSynchronization();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Something went wrong when opening connection with exoskeleton or multimedia.");
            }
        }

        private void radioButtonRecord_Click(object sender, EventArgs e)
        {
            if (isTestingPerformance) testingManager.GenericButtonClicked("Radiobutton Record");
        }

        private void radioButtonPlayback_Click(object sender, EventArgs e)
        {
            if (isTestingPerformance) testingManager.GenericButtonClicked("Radiobutton Playback");
        }

        private void radioButtonBidirectional_Click(object sender, EventArgs e)
        {
            if (isTestingPerformance) testingManager.GenericButtonClicked("Radiobutton Bidirectional");
        }

        private void radioButtonHaptic_Click(object sender, EventArgs e)
        {
            if (isTestingPerformance) testingManager.GenericButtonClicked("Radiobutton Haptic");
        }

        private void buttonConfirmInfo_Click(object sender, EventArgs e)
        {
            if (selectedDevices.Count == 0)
            {
                MessageBox.Show("Please confirm selected devices first.");
            }
            else
            {
                if (nExternalDevicesSynchronized == selectedDevices.Count)
                {
                    if (selectedDevices.ContainsKey("MultimediaRecorder") | selectedDevices.ContainsKey("MultimediaPlayer") | selectedDevices.ContainsKey("Exoskeleton"))
                    {
                        if (radioButtonRecord.Checked | radioButtonPlayback.Checked | radioButtonBidirectional.Checked | radioButtonHaptic.Checked)
                        {
                            //if (groupBoxCommandsRecording.Enabled == false)
                            //{
                            //    groupBoxCommandsRecording.Enabled = true;
                            //}
                        }
                        else
                        {
                            MessageBox.Show("Please select one option among Record, Haptic, Bidirectional or Playback.");
                        }
                    }
                    else
                    {
                        if (groupBoxCommandsRecording.Enabled == false)
                        {
                            groupBoxCommandsRecording.Enabled = true;
                        }
                    }
                    MessageBox.Show("Options confirmed");
                }
                else
                {
                    MessageBox.Show("Options confirmed. Not all selected devices are synchronized yet.");
                }
            }

            if (isTestingPerformance) testingManager.GenericButtonClicked("Button Confirm info");
        }

        private void buttonClearInfo_Click(object sender, EventArgs e)
        {
            textBoxSubjectID.Text = "";
            textBoxTrialID.Text = "";
            radioButtonRecord.Checked = false;
            radioButtonPlayback.Checked = false;
            radioButtonHaptic.Checked = false;
            radioButtonBidirectional.Checked = false;
            groupBoxCommandsRecording.Enabled = false;

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button Clear info");
            }
        }

        private void textBoxFilenameNotes_Click(object sender, EventArgs e)
        {
            if (isTestingPerformance) testingManager.GenericButtonClicked("Textbox Filename");
        }

        private void textBoxNotes_Click(object sender, EventArgs e)
        {
            if (isTestingPerformance) testingManager.GenericButtonClicked("Textbox Notes");
        }

        private void buttonSaveNotes_Click(object sender, EventArgs e)
        {
            try
            {
                string filenameNotes;
                if (textBoxFilenameNotes.Text != "")
                {
                    filenameNotes = textBoxFilenameNotes.Text + ".txt";
                }
                else
                {
                    filenameNotes = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "_" + subjID + "_Notes.txt";
                }
                string[] filePathComponents = { folderpathData, filenameNotes };
                string filePath = String.Join("\\", filePathComponents.ToList());

                if (File.Exists(filePath))
                    File.Delete(filePath);
                File.AppendAllText(filePath, "Notes\n\n" + textBoxNotes.Text + "\n\nLog Window\n\n" + textBoxWarning.Text);

                //textBoxWarning.AppendText(DateTime.Now.ToString() + " - " + filenameNotes + " saved successfully in " + workingDirectory + ".\r\n");
                PrintLogWindow(filenameNotes + " saved successfully in " + workingDirectory);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            //if (isTestingPerformance) testingManager.GenericButtonClicked("Button Save notes");
        }
        #endregion

        // TABS (CUSTOMIZE)
        #region Tab Wearable Sensors
        private void buttonConnectXsensDOT_Click(object sender, EventArgs e)
        {
            Dictionary<String, String> xsensInputParameters = new Dictionary<string, string>();
            if (comboBoxXsensDOTMode.SelectedItem.ToString() != null)
            {
                xsensDotStreamingMode = comboBoxXsensDOTMode.SelectedItem.ToString();
            }
            else
            {
                xsensDotStreamingMode = "";
                MessageBox.Show("Please select a streaming mode.");
            }
            
            if (xsensDotStreamingMode != null)
            {
                if (buttonConnectXsensDOT.Text == "Connect")
                {
                    xsensInputParameters.Add("defaultDirectory", defaultDirectory);
                    xsensInputParameters.Add("streamingMode", xsensDotStreamingMode);
                    xsensDot.OpenConnection(xsensInputParameters);

                    labelStatusXsensDOT.Text = "Synchronizing";
                    labelStatusXsensDOT.ForeColor = Color.Orange;
                    buttonConnectXsensDOT.Text = "Disconnect";
                }
                else if (buttonConnectXsensDOT.Text == "Disconnect")
                {
                    xsensDot.CloseConnection();
                    labelStatusXsensDOT.Text = "Not synchronized";
                    labelStatusXsensDOT.ForeColor = Color.Red;
                    buttonConnectXsensDOT.Text = "Connect";
                    xsensInputParameters.Clear();
                }
            }

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button Connect XsensDOT");
            }
        }

        private void comboBoxXsensDOTMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            PrintLogWindow("Selected XsensDOT streaming mode: " + comboBoxXsensDOTMode.SelectedItem.ToString());

            if (isTestingPerformance) testingManager.GenericButtonClicked("Combobox XsensDOT");
        }

        private void buttonConnectShimmer_Click(object sender, EventArgs e)
        {
            string comPortShimmer = "COM" + textBoxShimmerPortCom.Text; //"COM4";
            Dictionary<String, String> shimmerInputParameters = new Dictionary<String, String>();

            if (buttonConnectShimmer.Text == "Connect")
            {
                shimmerInputParameters.Add("defaultDirectory", defaultDirectory);
                shimmerInputParameters.Add("comPort", comPortShimmer);
                shimmer.OpenConnection(shimmerInputParameters);

                labelStatusShimmer.Text = "Synchronizing";
                labelStatusShimmer.ForeColor = Color.Orange;
                buttonConnectShimmer.Text = "Disconnect";
            }
            else if (buttonConnectShimmer.Text == "Disconnect")
            {
                shimmer.CloseConnection();
                labelStatusShimmer.Text = "Not synchronized";
                labelStatusShimmer.ForeColor = Color.Red;
                buttonConnectShimmer.Text = "Connect";
                shimmerInputParameters.Clear();
            }

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button Connect Shimmer");
            }
        }

        private void textBoxShimmerPortCom_Click(object sender, EventArgs e)
        {
            if (isTestingPerformance) testingManager.GenericButtonClicked("Textbox Shimmer COM");
        }

        private void buttonConnectBioharness_Click(object sender, EventArgs e)
        {
            Dictionary<String, String> bioharnessInputParameters = new Dictionary<string, string>();
            if (buttonConnectBioharness.Text == "Connect")
            {
                string macAddressBioharness = textBoxBioharnessAddress.Text; //"a0:e6:f8:fd:15:cd";

                string minicondaPath = "C:\\Users\\integ\\miniconda3\\Scripts";
                //string minicondaPath = null;
                if (minicondaPath == null)
                {
                    folderBrowserMiniconda.Description = "Please select the folder containing miniconda .exe (Users>username>miniconda3>Script)";
                    if (folderBrowserMiniconda.ShowDialog() == DialogResult.OK)
                    {
                        minicondaPath = folderBrowserMiniconda.SelectedPath;
                        Console.WriteLine(minicondaPath);
                    }
                }

                bioharnessInputParameters.Add("defaultDirectory", defaultDirectory);
                bioharnessInputParameters.Add("accessoryPath", minicondaPath);
                bioharnessInputParameters.Add("macAddress", macAddressBioharness);
                bioharness.OpenConnection(bioharnessInputParameters);

                labelStatusBioharness.Text = "Synchronizing";
                labelStatusBioharness.ForeColor = Color.Orange;
                buttonConnectBioharness.Text = "Disconnect";
            }
            else if (buttonConnectBioharness.Text == "Disconnect")
            {
                bioharness.CloseConnection();
                labelStatusBioharness.Text = "Not synchronized";
                labelStatusBioharness.ForeColor = Color.Red;
                buttonConnectBioharness.Text = "Connect";
                bioharnessInputParameters.Clear();
            }

            if (isTestingPerformance) testingManager.GenericButtonClicked("Button Connect Bioharness");
        }

        private void textBoxBioharnessAddress_Click(object sender, EventArgs e)
        {
            if (isTestingPerformance) testingManager.GenericButtonClicked("Textbox Bioharness MAC");
        }

        private void buttonConnectMachineLearning_Click(object sender, EventArgs e)
        {
            Dictionary<String, String> machineLearningInputParameters = new Dictionary<string, string>();
            if (buttonConnectMachineLearning.Text == "Connect")
            {
                if (comboBoxMLModelSelection.SelectedItem != null)
                {
                    string filenameMachineLearningModel = "";
                    string machineLearningModel = "";
                    if (comboBoxMLModelSelection.SelectedItem.ToString() == "Global")
                    {
                        filenameMachineLearningModel = "pre_trained_XGboost_model.json";
                        machineLearningModel = "Global";
                    }
                    else if (comboBoxMLModelSelection.SelectedItem.ToString() == "Motion")
                    {
                        filenameMachineLearningModel = "pre_trained_XGboost_motion_model.json";
                        machineLearningModel = "Motion";
                    }
                    else if (comboBoxMLModelSelection.SelectedItem.ToString() == "Biometric")
                    {
                        filenameMachineLearningModel = "pre_trained_XGboost_biometric_model.json";
                        machineLearningModel = "Biometric";
                    }
                    else if (comboBoxMLModelSelection.SelectedItem.ToString() == "Wrist Motion")
                    {
                        filenameMachineLearningModel = "pre_trained_XGboost_wrist_motion_model.json";
                        machineLearningModel = "WristMotion";
                    }

                    machineLearningInputParameters.Add("defaultDirectory", defaultDirectory);
                    machineLearningInputParameters.Add("emlModel", filenameMachineLearningModel);
                    machineLearning.OpenConnection(machineLearningInputParameters);
                    wearableSensorsThread = new Thread(() => CombineWearableData(machineLearningModel));
                    wearableSensorsThread.Start();

                    labelStatusMachineLearning.Text = "Synchronizing";
                    labelStatusMachineLearning.ForeColor = Color.Orange;
                    buttonConnectMachineLearning.Text = "Disconnect";
                }
                else
                {
                    MessageBox.Show("Please select a machine learning model type first.");
                }
            }
            else if (buttonConnectMachineLearning.Text == "Disconnect")
            {
                //machineLearning.CloseConnection();
                labelStatusMachineLearning.Text = "Not synchronized";
                labelStatusMachineLearning.ForeColor = Color.Red;
                buttonConnectMachineLearning.Text = "Connect";
                machineLearningInputParameters.Clear();
            }

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button Connect MachineLearning");
            }
        }

        private void comboBoxMLModelSelection_SelectedIndexChanged(object sender, EventArgs e)
        {          
            if (isTestingPerformance) testingManager.GenericButtonClicked("Textbox ML model");
        }
        #endregion

        #region Tab Instrumented Objects
        private void buttonConnectInstrumentedObjects_Click(object sender, EventArgs e)
        {

            Dictionary<String, String> instrumentedObjectsInputParameters = new Dictionary<String, String>();

            if (buttonConnectInstrumentedObjects.Text == "Connect")
            {
                instrumentedObjectsInputParameters.Add("defaultDirectory", defaultDirectory);
                instrumentedObjects.OpenConnection(instrumentedObjectsInputParameters);

                labelStatusInstrumentedObjects.Text = "Synchronizing";
                labelStatusInstrumentedObjects.ForeColor = Color.Orange;
                buttonConnectInstrumentedObjects.Text = "Disconnect";
            }
            else if (buttonConnectInstrumentedObjects.Text == "Disconnect")
            {
                instrumentedObjects.CloseConnection();
                labelStatusInstrumentedObjects.Text = "Not synchronized";
                labelStatusInstrumentedObjects.ForeColor = Color.Red;
                buttonConnectInstrumentedObjects.Text = "Connect";
                instrumentedObjectsInputParameters.Clear();
            }

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button Connect InstrumentedObjects");
            }
        }
        #endregion

        #region Tab AR Games
        private void buttonConnectARgames_Click(object sender, EventArgs e)
        {
            Dictionary<String, String> arGamesInputParameters = new Dictionary<string, string>();
            if (buttonConnectARgames.Text == "Connect")
            {
                arGamesInputParameters.Add("defaultDirectory", defaultDirectory);
                arGames.OpenConnection(arGamesInputParameters);

                labelStatusARgames.Text = "Synchronizing";
                labelStatusARgames.ForeColor = Color.Orange;
                buttonConnectARgames.Text = "Disconnect";
            }
            else if (buttonConnectARgames.Text == "Disconnect")
            {
                arGames.CloseConnection();
                labelStatusARgames.Text = "Not synchronized";
                labelStatusARgames.ForeColor = Color.Red;
                buttonConnectARgames.Text = "Connect";
                arGamesInputParameters.Clear();
            }

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button Connect ARgames");
            }
        }
        #endregion

        #region Tab Exoskeleton
        private void buttonExoskeletonLowImpedance_Click(object sender, EventArgs e)
        {
            NotificationRaisedEventArgs args = new NotificationRaisedEventArgs
            {
                messageToSlave = "Value:5;"
            };
            OnNotificationRaised(args);

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button Exo Low impedance");
            }
        }

        private void buttonExoskeletonHighImpedance_Click(object sender, EventArgs e)
        {
            NotificationRaisedEventArgs args = new NotificationRaisedEventArgs
            {
                messageToSlave = "Value:6;"
            };
            OnNotificationRaised(args);

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button Exo High impedance");
            }
        }
        #endregion

        #region Tab Myro
        private void buttonConnectMyro_Click(object sender, EventArgs e)
        {
            Dictionary<String, String> myroInputParameters = new Dictionary<string, string>();
            if (buttonConnectMyro.Text == "Connect")
            {
                myroInputParameters.Add("defaultDirectory", defaultDirectory);
                myro.OpenConnection(myroInputParameters);

                labelStatusMyro.Text = "Synchronizing";
                labelStatusMyro.ForeColor = Color.Orange;
                buttonConnectMyro.Text = "Disconnect";
            }
            else if (buttonConnectMyro.Text == "Disconnect")
            {
                myro.CloseConnection();
                labelStatusMyro.Text = "Not synchronized";
                labelStatusMyro.ForeColor = Color.Red;
                buttonConnectMyro.Text = "Connect";
                myroInputParameters.Clear();
            }

            if (isTestingPerformance)
            {
                testingManager.GenericButtonClicked("Button Connect Myro");
            }
        }
        #endregion

        #region Tab Subsidiary Devices
        private void buttonConnectXsensMTw_Click(object sender, EventArgs e)
        {
            string comPortXsensMTw = "COM" + textBoxXsensMTwPortCom.Text; 
            Dictionary<String, String> xsensMTwInputParameters = new Dictionary<String, String>();

            if (buttonConnectXsensMTw.Text == "Connect")
            {
                xsensMTwInputParameters.Add("defaultDirectory", defaultDirectory);
                xsensMTwInputParameters.Add("comPort", comPortXsensMTw);
                xsensMTw.OpenConnection(xsensMTwInputParameters);

                labelStatusXsensMTw.Text = "Synchronizing";
                labelStatusXsensMTw.ForeColor = Color.Orange;
                buttonConnectXsensMTw.Text = "Disconnect";
            }
            else if (buttonConnectShimmer.Text == "Disconnect")
            {
                xsensMTw.CloseConnection();
                labelStatusXsensMTw.Text = "Not synchronized";
                labelStatusXsensMTw.ForeColor = Color.Red;
                buttonConnectXsensMTw.Text = "Connect";
                xsensMTwInputParameters.Clear();
            }

            if (isTestingPerformance) testingManager.GenericButtonClicked("Button Connect Xsens MTw");
        }

        private void textBoxXsensMTwPortCom_Click(object sender, EventArgs e)
        {
            if (isTestingPerformance) testingManager.GenericButtonClicked("Textbox Xsens MTw COM");
        }

        private void buttonMetronome_Click(object sender, EventArgs e)
        {
            if (buttonMetronome.Text == "Disable")
            {
                if (metronome.isPlaying)
                {
                    metronome.StopMetronome();
                    //buttonMetronome.Text = "Enable";
                    PrintLogWindow("Metronome has been disabled");
                }
                buttonMetronome.Text = "Enable";
            }
            else if (buttonMetronome.Text == "Enable")
            {
                metronome.PlayMetronome();
                buttonMetronome.Text = "Disable";
                PrintLogWindow("Metronome has been enabled");
            }

            if (isTestingPerformance) testingManager.GenericButtonClicked("Button Start Metronome");
        }

        private void comboBoxMetronomeTempo_SelectedIndexChanged(object sender, EventArgs e)
        {
            //string metronomeFilename = comboBoxMetronomeTempo.SelectedItem.ToString() + "_metronome.wav";
            filenameMetronome = comboBoxMetronomeTempo.SelectedItem.ToString() + "_metronome.wav";
            string metronomeFileFolder = string.Join("\\", workingDirectory, foldernameMedia);
            metronome.GetMetronomeFile(metronomeFileFolder, filenameMetronome);

            PrintLogWindow("Selected metronome tempo: " + comboBoxMetronomeTempo.SelectedItem.ToString());
            
            if (isTestingPerformance) testingManager.GenericButtonClicked("ComboBox Metronome Tempo");
        }

        #endregion

        private void CCS_FormClosing(object sender, FormClosingEventArgs e)
        {
            PrintLogWindow("Closing application");
            for (int i = 0; i < selectedDevices.Count; i++)
            {
                string device = selectedDevices.ElementAt(i).Key;
                try
                {
                    switch (device.Substring(device.LastIndexOf('.') + 1))
                    {
                        case "Exoskeleton":
                            if (exoskeleton != null)//(exoskeletonProperties.isDeviceSync)
                                exoskeleton.CloseConnection();
                            break;
                        case "XsensDOT":
                            if (xsensDot != null)//(xsensDotProperties.isDeviceSync == true)
                                xsensDot.CloseConnection();
                            break;
                        case "Bioharness":
                            if (bioharness != null)//(bioharnessProperties.isDeviceSync == true)
                                bioharness.CloseConnection();
                            break;
                        case "Shimmer":
                            if (shimmer != null)//(shimmerProperties.isDeviceSync == true)
                                shimmer.CloseConnection();
                            break;
                        case "MachineLearning":
                            if (machineLearning != null)
                            {
                                machineLearning.CloseConnection();
                                machineLearningTx.Close();
                                stopCombineWearableDataThread = true;
                            }
                            if (wearableSensorsThread != null)
                            {
                                wearableSensorsThread.Join();
                            }
                            break;
                        case "MultimediaRecorder":
                            if (multimediaRecorder != null)//(multimediaRecorderProperties.isDeviceSync == true) //isMultimediaRecorderSynchronized
                                multimediaRecorder.CloseConnection();
                            break;
                        case "MultimediaPlayer":
                            if (multimediaPlayer != null)//(multimediaPlayerProperties.isDeviceSync == true) //isMultimediaPlayerSynchronized
                                multimediaPlayer.CloseConnection();
                            break;
                        //case "OpenSoundControl":
                        //    if (oscSynchProperties.isDeviceSync == true)
                        //        oscSynch.CloseOscSynch();
                            //break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            buttonSaveNotes.PerformClick();

            if (isTestingPerformance)
            {
                testingManager.GenerateLogString("Application closed.\n");
                string filenameTestingLogs = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "_" + subjID + "_TestingLogs.txt"; ;

                string[] filePathComponents = { folderpathData, filenameTestingLogs };
                string filePathLogs = String.Join("\\", filePathComponents.ToList());

                testingManager.GenerateTestingLogFile(filePathLogs);
            }
        }
    }
}
