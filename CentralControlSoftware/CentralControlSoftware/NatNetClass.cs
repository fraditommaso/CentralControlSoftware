using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NatNetML;

namespace CentralControlSoftware
{
    public partial class NatNetClass
    {

        public NatNetClass()
        {

        }

        NatNetClientML m_NatNet;

        // [NatNet] Description of the Active Model List from the server (e.g. Motive)
        public static NatNetML.ServerDescription desc = new NatNetML.ServerDescription();

        // auto-connect
        public static bool mWantAutoconnect = false;
        public static bool mServerDetected = false;
        public static bool mServerEstablished = false;
        public static string mDetectedLocalIP = "";
        public static string mDetectedServerIP = "";

        // server information
        public static string mServerIP = "";
        public static double m_ServerFramerate = 1.0f;
        public static float m_ServerToMillimeters = 1.0f;
        public static int m_UpAxis = 1;   // 0=x, 1=y, 2=z (Y default)
        public static int mAnalogSamplesPerMocpaFrame = 0;
        public static int mDroppedFrames = 0;
        public static int mLastFrame = 0;
        public static int mUIBusyCount = 0;
        public static bool mNeedTrackingListUpdate = false;

        /// <summary>
        /// Connect to a NatNet server (e.g. Motive)
        /// </summary>
        /// 

        public int CreateClient()
        {
            // release any previous instance
            if (m_NatNet != null)
            {
                m_NatNet.Disconnect();
            }

            // [NatNet] create a new NatNet instance
            m_NatNet = new NatNetML.NatNetClientML();

            // [NatNet] set a "Frame Ready" callback function (event handler) handler that will be
            // called by NatNet when NatNet receives a frame of data from the server application
            //   m_NatNet.OnFrameReady += new NatNetML.FrameReadyEventHandler(m_NatNet_OnFrameReady);

            /*
            // [NatNet] for testing only - event signature format required by some types of .NET applications (e.g. MatLab)
            m_NatNet.OnFrameReady2 += new FrameReadyEventHandler2(m_NatNet_OnFrameReady2);
            */

            // [NatNet] print version info
            int[] ver = new int[4];
            ver = m_NatNet.NatNetVersion();
            String strVersion = String.Format("NatNet Version : {0}.{1}.{2}.{3}", ver[0], ver[1], ver[2], ver[3]);
            Console.WriteLine(strVersion);

            return 0;
        }

        public void Connect()
        {
            // [NatNet] connect to a NatNet server
            int returnCode = 0;
            string strLocalIP = "127.0.0.1";
            string strServerIP = "127.0.0.1";

            NatNetClientML.ConnectParams connectParams = new NatNetClientML.ConnectParams();

            //multicast
            connectParams.ConnectionType = ConnectionType.Multicast;

            ////broadcast
            //connectParams.ConnectionType = ConnectionType.Multicast;
            //connectParams.MulticastAddress = "255.255.255.255";

            connectParams.ServerAddress = strServerIP;
            connectParams.LocalAddress = strLocalIP;

            // Test: subscribed data only:
            //connectParams.SubscribedDataOnly = SubscribeOnlyCheckBox.Checked;

            // Test : requested bitstream version
            /*
            connectParams.BitstreamMajor = 1;
            connectParams.BitstreamMinor = 2;
            connectParams.BitstreamRevision = 3;
            connectParams.BitstreamBuild = 4;
            */

            returnCode = m_NatNet.Connect(connectParams);
            if (returnCode == 0)
            {
                Console.WriteLine("Initialization Succeeded.");
            }
            else
            {
                Console.WriteLine("Error Initializing.");

            }

            // [NatNet] validate the connection
            returnCode = m_NatNet.GetServerDescription(desc);
            if (returnCode == 0)
            {
                Console.WriteLine("Connection Succeeded.");
                Console.WriteLine("   Server App Name: " + desc.HostApp);
                Console.WriteLine(String.Format("   Server App Version: {0}.{1}.{2}.{3}", desc.HostAppVersion[0], desc.HostAppVersion[1], desc.HostAppVersion[2], desc.HostAppVersion[3]));
                Console.WriteLine(String.Format("   Server NatNet Version: {0}.{1}.{2}.{3}", desc.NatNetVersion[0], desc.NatNetVersion[1], desc.NatNetVersion[2], desc.NatNetVersion[3]));


                mServerEstablished = true;
                mServerIP = String.Format("{0}.{1}.{2}.{3}", desc.HostComputerAddress[0], desc.HostComputerAddress[1], desc.HostComputerAddress[2], desc.HostComputerAddress[3]);


                // Tracking Tools and Motive report in meters - lets convert to millimeters
                if (desc.HostApp.Contains("TrackingTools") || desc.HostApp.Contains("Motive"))
                    m_ServerToMillimeters = 1000.0f;

                // [NatNet] [optional] Query mocap server for the current camera framerate
                int nBytes = 0;
                byte[] response = new byte[10000];
                int rc;
                rc = m_NatNet.SendMessageAndWait("FrameRate", out response, out nBytes);
                if (rc == 0)
                {
                    try
                    {
                        m_ServerFramerate = BitConverter.ToSingle(response, 0);
                        Console.WriteLine(String.Format("   Camera Framerate: {0}", m_ServerFramerate));
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                // [NatNet] [optional] Query mocap server for the current analog framerate
                rc = m_NatNet.SendMessageAndWait("AnalogSamplesPerMocapFrame", out response, out nBytes);
                if (rc == 0)
                {
                    try
                    {
                        mAnalogSamplesPerMocpaFrame = BitConverter.ToInt32(response, 0);
                        Console.WriteLine(String.Format("   Analog Samples Per Camera Frame: {0}", mAnalogSamplesPerMocpaFrame));
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                // [NatNet] [optional] Query mocap server for the current up axis
                rc = m_NatNet.SendMessageAndWait("UpAxis", out response, out nBytes);
                if (rc == 0)
                {
                    m_UpAxis = BitConverter.ToInt32(response, 0);
                }

                mDroppedFrames = 0;
            }
            else
            {
                Console.WriteLine("Error Connecting.");
            }
        }

        public void Disconnect()
        {
            // [NatNet] disconnect
            // optional : for unicast clients only - notify Motive we are disconnecting
            int nBytes = 0;
            byte[] response = new byte[10000];
            int rc;
            rc = m_NatNet.SendMessageAndWait("Disconnect", out response, out nBytes);
            if (rc == 0)
            {

            }
            // shutdown our client socket
            m_NatNet.Disconnect();

        }

        public void SetRecordingTakeButton_Click()
        {
            int nBytes = 0;
            byte[] response = new byte[10000];
            String strCommand = "SetRecordTakeName," + "CONBOTS_EXO_OPTITRACK";
            int rc = m_NatNet.SendMessageAndWait(strCommand, out response, out nBytes);
        }

        public void RecordButton_Click()
        {
            string command = "StartRecording";

            int nBytes = 0;
            byte[] response = new byte[10000];
            int rc = m_NatNet.SendMessageAndWait(command, 3, 100, out response, out nBytes);
            if (rc != 0)
            {
                Console.WriteLine(command + " not handled by server");
            }
            else
            {
                int opResult = System.BitConverter.ToInt32(response, 0);
                if (opResult == 0)
                    Console.WriteLine(command + " handled and succeeded.");
                else
                    Console.WriteLine(command + " handled but failed.");
            }
        }

        public void StopRecordButton_Click()
        {
            string command = "StopRecording";

            int nBytes = 0;
            byte[] response = new byte[10000];
            int rc = m_NatNet.SendMessageAndWait(command, out response, out nBytes);

            if (rc != 0)
            {
                Console.WriteLine(command + " not handled by server");
            }
            else
            {
                int opResult = System.BitConverter.ToInt32(response, 0);
                if (opResult == 0)
                    Console.WriteLine(command + " handled and succeeded.");
                else
                    Console.WriteLine(command + " handled but failed.");
            }
        }
    }
}
