using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace CentralControlSoftware
{
    public class MultimediaPlayer : ExternalDevice
    {
        public string multimediaFilename;
        public VlcRemote vlcRemote;
        public Process process;
        private string experimentalCondition;
        private string playerApi;

        public MultimediaPlayer() : base()
        {
            externalDeviceName = "MultimediaPlayer";
            triggerStart = "1";
            triggerEnd = "0";

            vlcRemote = new VlcRemote();
        }

        public MultimediaPlayer(string _transmitterIpMaster, int _transmitterPortMaster, string _receiverIpSlave,
            int _receiverPortSlave) : base(_transmitterIpMaster, _transmitterPortMaster, _receiverIpSlave, _receiverPortSlave)
        {
            externalDeviceName = "MultimediaPlayer";
            triggerStart = "1";
            triggerEnd = "0";
        }

        protected override void StartConnection(Dictionary<string, string> ManageConnectionInputParameters)
        {
            string defaultDirectory = ManageConnectionInputParameters["defaultDirectory"];
            playerApi = ManageConnectionInputParameters["playerApi"];
            experimentalCondition = ManageConnectionInputParameters["experimentalCondition"];
            string foldernameMedia = "media"; // default media directory name

            if (playerApi == "vlc")
            {
                OpenFileDialog fdlg = new OpenFileDialog();
                fdlg.Title = "Open File Dialog";
                fdlg.InitialDirectory = defaultDirectory;

                if (fdlg.ShowDialog() == DialogResult.OK)
                {
                    multimediaFilename = fdlg.FileName;

                    // Get local path for media folder
                    foldernameMedia = Path.GetDirectoryName(multimediaFilename);
                }

                // Add video to playlist
                vlcRemote.Add(multimediaFilename);

                //// TEST 1 (2023/07/10)
                //// When start button is pressed, a window pops up and the video starts automatically.
                //// TODO: check if the latency in opening the video + starting the video causes desynchronization with haptics
                //vlcRemote.Play();
                //vlcRemote.Pause();
                //vlcRemote.Pause();
                //vlcRemote.Seek(0);

                // TEST 2 (2023/07/10)
                // This works: two videos are added to the queue, a window pops up and the first video starts automatically.
                // When start button is pressed, the player skips to the second video in the playlist, which is supposedly 
                // synchronized with the haptics.)

                string filenameOnHoldVideo = "CONBOTS_onhold_video_5min.mp4";

                // Add on hold video from cloud directory to playlist
                //vlcRemote.Add(string.Join("\\", defaultDirectory, foldernameMedia, filenameOnHoldVideo));
                // Add on hold video from local directory to playlist
                //vlcRemote.Add(multimediaFilename);
                vlcRemote.Add(string.Join("\\", foldernameMedia, filenameOnHoldVideo));

                //vlcRemote.GoToFullScreen();
            }
            else if (playerApi == "python")
            {
                PlayMultimediaPython(defaultDirectory);
            }

            DataFromExternalDeviceEventArgs args = new DataFromExternalDeviceEventArgs
            {
                name = externalDeviceName,
                isExternalDeviceSynchronized = true,
            };
            OnDataFromExternalDevice(args);
        }

        protected override void StopConnection()
        {
            if (playerApi == "vlc")
            {
                vlcRemote.Clear();
                //vlcRemote.Quit();
            }
            else if (playerApi == "python")
            {

            }
        }

        protected override void SendDataToSlaveCustomized()
        {
            if (commandMessage == "1")
            {
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " - " + "Multimedia player: video is starting...");
                // TEST 1
                //vlcRemote.Play();

                //// TEST 2 (this works)
                vlcRemote.Next();

                //vlcRemote.GoToFullScreen();

                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " - " + "Multimedia player: video has started");

            }
            else if (commandMessage == "0")
            {
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " - " + "Multimedia player: video is stopping...");

                vlcRemote.Stop();

                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " - " + "Multimedia player: video has stopped");
            }
        }

        protected override void ReceiveMessagesFromSlave(string receivedMessage) { }

        protected override Dictionary<String, Object> UnpackArrayData(string[] lineSubstrings)
        {
            Dictionary<String, Object> unpackedDataDictionary = new Dictionary<string, object>();

            return unpackedDataDictionary;
        }

        protected override void CheckTransmissionMessage() { }

        public string GetMultimediaFilename()
        {
            string[] filenameSubstrings = multimediaFilename.Split(new char[] { '\\' });
            filename = filenameSubstrings.Last();
            return filename;
        }

        public void PlayMultimediaPython(string defaultPath)
        {
            process = new Process();

            process.StartInfo.FileName = "cmd.exe";
            // This line opens a cmd window and pops it up when launched
            //process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            // These lines redirect input to Visual Studio Output Window instead of showing to cmd window.
            //process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            //process.StartInfo.UseShellExecute = false;
            //process.StartInfo.RedirectStandardInput = true;

            // This line does not show cmd window
            //process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            // This line opens a cmd window but minimizes it so it does not pop up to the screen when launched
            process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;

            string[] subfolders = { defaultPath, "AudioVideoRecorder_Python" };

            string workingDirectoryPath = Path.Combine(subfolders);

            process.StartInfo.WorkingDirectory = workingDirectoryPath;
            process.StartInfo.Arguments = "/k python AudioVideoPlayer.py"; ;
            process.Start();
        }
    }
}
