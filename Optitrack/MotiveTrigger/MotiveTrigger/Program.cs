using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NatNetML;

namespace MotiveTrigger
{
    

    static class Program
    {

        [STAThread]
        static void Main()
        {

            Console.WriteLine("Connection attempt ...");

            //create natnet class and initialize natnet client
            NatNetClass nat=new NatNetClass();
            nat.CreateClient();

            //connect to motive
            nat.Connect();

            nat.SetRecordingTakeButton_Click();

            while (true)
            {


                //wait for user input
                Console.ReadLine();

                //start recording
                nat.RecordButton_Click();

                //wait for user input
                Console.ReadLine();

                //stop recording
                nat.StopRecordButton_Click();

                //wait for user input
                string a=Console.ReadLine();
                if (a == "e")
                {
                    nat.Disconnect();
                }


            }
            

        }

       

       

    }




}
