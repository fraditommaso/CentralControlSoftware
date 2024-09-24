using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace CentralControlSoftware
{
    public class Serial
    {
        private SerialPort stream;

        //public bool isSerialCreated;

        public void Setup(string com, int baudRate, int readTimeout = 500) //bool
        {
            //try
            //{
                stream = new SerialPort(com, baudRate);
                stream.ReadTimeout = readTimeout;
                stream.Open();
            //    isSerialCreated = true;
            //}
            //catch
            //{
            //    isSerialCreated = false;
            //    Console.WriteLine("Please connect to a COM port.");
            //}
            //return isSerialCreated;
        }

        public void SendTrigger(char command, int n_write)
        {

            byte[] cmd = new byte[1];
            cmd[0] = (byte)command;

            if (stream.IsOpen)
            {
                stream.Write(cmd, 0, n_write);
                stream.BaseStream.Flush();
            }
            else
            {
                Console.WriteLine("Serial Communication not initialized.");
            }
        }

        public void CloseSerialCom()
        {
            if (stream.IsOpen)
            {
                stream.Close();
            }
            else
            {
                Console.WriteLine("Serial Com already closed");
            }
        }

        public IEnumerator AsynchronousReadFromSerial(Action<string> callback, Action fail = null, float timeout = float.PositiveInfinity)
        {
            DateTime initialTime = DateTime.Now;
            DateTime nowTime;
            TimeSpan diff = default(TimeSpan);

            string dataString = null;

            do
            {
                try
                {
                    dataString = stream.ReadLine();
                }
                catch (TimeoutException)
                {
                    dataString = null;
                }

                if (dataString != null)
                {
                    callback(dataString);
                    yield return null;
                }
                else
                {
                    Thread.Sleep(5);
                    yield return null;
                }

                nowTime = DateTime.Now;
                diff = nowTime - initialTime;

            } while (diff.Milliseconds < timeout);

            if (fail != null)
                fail();
            yield return null;
        }
    }
}
