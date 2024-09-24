using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;
using System.IO;

namespace CentralControlSoftware
{
    public class Metronome
    {
        public SoundPlayer metronomeSound;
        public bool isPlaying;

        public Metronome()
        {
            // Test to implement comboBox with multiple metronome choices (ctr has no input args because filename is selected automatically based on the choice) 
        }

        public Metronome(string workingDirectory, string filename)
        {
            try
            {
                metronomeSound = new SoundPlayer(Path.Combine(workingDirectory, filename));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void GetMetronomeFile(string workingDirectory, string filename)
        {
            try
            {
                metronomeSound = new SoundPlayer(Path.Combine(workingDirectory, filename));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }            
        }

        public void PlayMetronome()
        {
            try
            {
                metronomeSound.Play();
                isPlaying = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void StopMetronome()
        {
            try
            {
                metronomeSound.Stop();
                isPlaying = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
