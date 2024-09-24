using Rug.Osc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CentralControlSoftware
{
    public class OscSynch
    {
        static OscReceiver receiver;
        static Thread thread;

        // these are the clock values, 0 by default
        static int clock_hour = 0;
        static int clock_min = 0;
        static int clock_sec = 0;
        static int clock_frame = 0;
        static int clock_subframe = 0;
        static DateTime clock_receivedTime = DateTime.Now;

        public OscSynch()
        {

        }

        public void InitializeOscSynch()
        {
            // Convention: we use port 6575 for the clock (lab-wide broadcast)
            int port = 6575;

            // Create the receiver
            receiver = new OscReceiver(port);

            // Create a thread to do the listening
            thread = new Thread(new ThreadStart(ListenLoop));

            // Connect the receiver
            receiver.Connect();

            // Start the listen thread
            thread.Start();

            //// wait for a key press to exit
            //Console.WriteLine("Listening on OSC port 6575 for clock signal. Press any key to get current clock.  After 10 times demo program quits.");
            //for (int i = 0; i < 10; i++)
            //{
            //    Console.ReadKey(true);
            //    int[] clock = getCurrentClock();
            //    Console.WriteLine("ASIL clock at aquisition time: " + clock[0] + ":" + clock[1] + ":" + clock[2] + ":" + clock[3] + ":" + clock[4] + ":" + clock[5]);

            //}

        }

        public void CloseOscSynch()
        {
            // close the Reciver 
            receiver.Close();

            // Wait for the listen thread to exit
            thread.Join();
        }

        //method called when clock is received.  Attention: runs in different thread!
        //convention:  the message contains 5 ints: hour, minute, second, frame , subframe
        static void clockReceived(OscMessage message)
        {
            if (message.Count == 5)
            {
                clock_hour = Int32.Parse(message[0].ToString());
                clock_min = Int32.Parse(message[1].ToString());
                clock_sec = Int32.Parse(message[2].ToString());
                clock_frame = Int32.Parse(message[3].ToString());
                clock_subframe = Int32.Parse(message[4].ToString());
                clock_receivedTime = DateTime.Now;

                // Debug
                //Console.WriteLine("ASIL clock received: : " + clock_hour + ":" + clock_min + ":" + clock_sec + ":" +
                //    clock_frame + ":" + clock_subframe); 
            }
        }

        // this would be the method to be called when you receive a sample somewhere to get the timestamp
        // it returns a list of 6 numbers: the complete lab clock (h, m, s, frame, subframe) + additionally,
        // the time since last sample was received (milliseconds). 

        public static int[] getCurrentClock()
        {
            int elapsedMillisecs = (int)((TimeSpan)(DateTime.Now - clock_receivedTime)).TotalMilliseconds;

            return new int[] { clock_hour, clock_min, clock_sec, clock_frame, clock_subframe, elapsedMillisecs };
        }


        //OSC Listener
        static void ListenLoop()
        {
            try
            {

                OscAddressManager m_Listener = new OscAddressManager();
                m_Listener.Attach("/asil/clock", clockReceived); //again convention

                while (receiver.State != OscSocketState.Closed)
                {
                    // if we are in a state to recieve
                    if (receiver.State == OscSocketState.Connected)
                    {
                        // get the next message 
                        // this will block until one arrives or the socket is closed
                        OscPacket packet = receiver.Receive();

                        //adress matching
                        switch (m_Listener.ShouldInvoke(packet))
                        {
                            case OscPacketInvokeAction.Invoke:
                                m_Listener.Invoke(packet);
                                break;
                            default:
                                break;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                // if the socket was connected when this happens
                // then tell the user
                if (receiver.State == OscSocketState.Connected)
                {
                    Console.WriteLine("Exception in listen loop");
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}