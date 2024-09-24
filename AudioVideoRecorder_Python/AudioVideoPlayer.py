#!/usr/bin/env python3.9.11

# AUTHOR
#	Francesco Di Tommaso, PhD Student
#		Advanced Robotics and Human Centered Technologies, CREO Lab
#		Universita Campus Bio-Medico di Roma
#		Via Alvaro del Portillo 21, 00128 Roma, Italy
# 		email: f.ditommaso@unicampus.it

# v01 (23.03.22)
#   - class VideoPlayer and class AudioPlayer

# v02
#   - Properly working

# v03
#   - UDP trigger
#   - TO DO: select file

# v04
#   - Improved coding


from importlib.resources import path
import os
import cv2
from cv2 import exp
import numpy
import time
import threading
import pyaudio
import wave
import socket
import _thread
from tkinter import Tk
from tkinter.filedialog import askdirectory
from tkinter import messagebox
import datetime

# ===================================== CLASSES =====================================
class VideoPlayer:
    def __init__(self, path_file):
        self.isSourceOpen = True
        self.frameSize = desiredFrameSize
        #self.path_file = path_video
        #self.video_cap = cv2.VideoCapture(self.path_file) #use this when constructor has no input (path_file)
        self.stop_time_video = None
        try:
            self.video_cap = cv2.VideoCapture(path_file)
            self.fps = int(self.video_cap.get(cv2.CAP_PROP_FPS))

            # Debug
            self.frame_count = int(self.video_cap.get(cv2.CAP_PROP_FRAME_COUNT))
            self.video_length = self.frame_count/self.fps
            print_logs('Recorded video length = ' + str(self.video_length) + ' s; ' + 'Recorded video fps = ' + str(self.fps) + '; Recorded video frames = ' + str(self.frame_count))
        except:
            print_logs("File not found. Please try again.")

    def play(self):
        isVideoPlayed = False

        #wait_time = round((1/self.fps )*1000 - 15)
        previous_time = 0

        #global isTrigger

        # while(self.isSourceOpen==True):
        #     ret, video_frame = self.video_cap.read()

        #     # Manage video player
        #     if(ret == 0):
        #         self.stop()
        #         break

        #     if cv2.waitKey(1) & 0xFF == ord('q'):
        #         audio_thread.close()
        #         video_thread.stop()
        #         break

        #     # Show frame
        #     if(isVideoPlayed==False):
        #         while(trigger != "1"):
        #             cv2.imshow('Video', video_frame)
        #             time.sleep(0.01)

        #         isVideoPlayed = True
        #         self.start_time_video = time.time()
        #     else:
        #         if trigger == "1":
        #             cv2.imshow('Video', video_frame)

        #             # Debug
        #             sleep = 0.01 + 0.004 #0.0035
        #             time.sleep(1/self.fps - sleep)
        #             #print("Slept for " + str(1/self.fps - sleep))
        #         elif trigger == "0":
        #             self.stop()
        #             
        
        while self.isSourceOpen is True:
            if isTrigger is True:
                ret, video_frame = self.video_cap.read()

                if not ret:
                    print_logs("Can't read frame (last frame?). Exiting...")
                    self.stop()
                    break

                cv2.imshow('Video', video_frame)

                
                if isVideoPlayed is False:
                    isVideoPlayed = True
                    self.start_time_video = time.time()
                    print_logs("Started playing video at " + datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3])
                    elapsed_time = 0
                else:
                    current_time = time.time()
                    elapsed_time = current_time - previous_time

                wait_time = round((1/self.fps - elapsed_time)*1000) - 7
                #print_logs("Elapsed time = " + str(elapsed_time*1000) + " ms ; Sleep time = " + str(wait_time) + " ms")

                if cv2.waitKey(wait_time) & 0xFF == ord('q'):
                    audio_thread.close()
                    video_thread.stop()
                    break

                previous_time = time.time()
                
            elif isTrigger is False:
                 self.stop()
            elif isTrigger is None:
                 continue
                

    # Method to stop VideoPlayer
    def stop(self):
        #global isTrigger
        if self.isSourceOpen==True:
            #isTrigger = False
            #self.stop_time_video = time.time()

            
            self.video_cap.release()
            print_logs("Stopped playing video at " + datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3])
            self.stop_time_video = time.time()
            cv2.destroyAllWindows()

            track_video_length = self.stop_time_video - self.start_time_video
            
            print_logs('Played video length = ' + str(track_video_length) + ' s; ' 
            + 'Started at: ' + str(self.start_time_video) + ', Ended at: ' + str(self.stop_time_video))

            self.isSourceOpen = False
        else:
            pass

    def start(self):
        video_thread = threading.Thread(target=self.play)
        video_thread.start()

class AudioPlayer:
    chunk = 1024

    def __init__(self, file):
        """ Init audio stream """ 
        try:
            self.isSourceOpen = True
            self.wf = wave.open(file, 'rb')
            self.p = pyaudio.PyAudio()
            self.stream = self.p.open(
                format = self.p.get_format_from_width(self.wf.getsampwidth()),
                channels = self.wf.getnchannels(),
                rate = self.wf.getframerate(),
                output = True
            )

            # Debug
            self.nframes = self.wf.getnframes()
            self.audio_length = self.nframes/self.stream._rate
            self.stop_time_audio = None
            print_logs('Recorded audio length = ' + str(self.audio_length) + ' s; ' + 'Recorded audio fps = ' + str(self.stream._rate))
        except:
            print_logs("File not found. Please try again.")


    def play(self):
        """ Play entire file """
        isAudioPlayed = False
        data = self.wf.readframes(self.chunk)
        # while data != b'':
        #     if trigger == "1":
        #         if isAudioPlayed == False:
        #             isAudioPlayed = True
        #             self.start_time_audio = time.time()
        #         else:
        #             self.stream.write(data)
        #             data = self.wf.readframes(self.chunk)
        #     elif trigger == "0":
        #         if isAudioPlayed == False:
        #             continue
        #         elif isAudioPlayed == True:
        #             self.close()
        #             break

        # if trigger == "1":
        #     self.close()

        while data != b'':
             
            if isTrigger is True:
                if isAudioPlayed is False:
                    isAudioPlayed = True
                    self.start_time_audio = time.time()
                    print_logs("Started playing audio at " + datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3])
                else:
                    self.stream.write(data)
                    data = self.wf.readframes(self.chunk)
            elif isTrigger is False:
                self.close()
                break
            elif isTrigger is None:
                continue
        
        #if isTrigger is True:
        self.close()

    def close(self):
        # global isTrigger
        # isTrigger = False

        """ Graceful shutdown """ 
        
        self.stream.close()
        self.stop_time_audio = time.time()
        print_logs("Stopped playing audio at " + datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3])
        self.p.terminate()

        track_audio_length = self.stop_time_audio - self.start_time_audio
        #print("Played audio length = " + str(track_audio_length) + ' s')
        print_logs('Played audio length = ' + str(track_audio_length) + ' s; ' 
            + 'Started at: ' + str(self.start_time_audio) + ', Ended at: ' + str(self.stop_time_audio))
        
        self.isSourceOpen = False

    def start(self):
        audio_thread = threading.Thread(target=self.play)
        audio_thread.start()

# ===================================== METHODS =====================================
def start_AVplayer():
    global video_thread
    global audio_thread

    video_thread = VideoPlayer(path_file=path_video)
    audio_thread = AudioPlayer(file=path_audio)

    video_thread.start()
    audio_thread.start()

# Method to start a thread to listen from a server (CCS) and receive a trigger signal
def receiver_thread():
	global isTrigger
	while True:
		data, addr = receiver_socket.recvfrom(3)
		trigger = data.decode("utf-8")
		#print('Trigger = ' + trigger)
		print_logs('Trigger = ' + trigger)

		if trigger == "1":
			isTrigger = True
		elif trigger == "0":
			isTrigger = False
		else:
			isTrigger = None

def print_logs(message):
	log = datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3] + " - " + message + "\n"
	logs.append(log)
	print(log)


def get_filename_path():
    
    # Get local path of the folder where the routine is stored
    local_path = os.getcwd()
    parent_dir = os.path.abspath(os.path.join(local_path, os.pardir))
    
    absolute_path = askdirectory(initialdir=parent_dir, title='Select the folder with multimedia files') # shows dialog box and return the path

    if not absolute_path:
        answer = messagebox.askyesno("Question", "Would you like to try again?")
        if answer is True:
            absolute_path = askdirectory(title='Select Folder') # shows dialog box and return the path
        else:
            print("If you want to play multimedia files, please select a folder.")
    
    folder_name = os.path.basename(absolute_path)
    # _video.avi works
    path_video = os.path.join(absolute_path, folder_name + '_video.avi')
    #path_video = os.path.join(absolute_path, folder_name + '_video2.avi')
    #path_video = os.path.join(absolute_path, folder_name + '_mixed.avi')
    #path_video = os.path.join(absolute_path, folder_name + '_mixed.mp4')
    
    path_audio = os.path.join(absolute_path, folder_name + '_audio.wav')
    return path_video, path_audio

# Method to get current date and time for storage purposes
def get_datetime():
	now = datetime.datetime.now()
	str_year = f'{now.year:04d}'
	str_month = f'{now.month:02d}'
	str_day = f'{now.day:02d}'
	str_hour = f'{now.hour:02d}'
	str_minute = f'{now.minute:02d}'
	str_second = f'{now.second:02d}'

	datetime_str = (str_year + "-" + str_month + "-" + str_day + "_" + str_hour + "." + str_minute + "." + str_second)
	return datetime_str

# Method to create the storage folder
def define_storage_folder(filename_common):
	
	# Get local path of the folder where the routine is stored
	local_path = os.getcwd()

	parent_dir = os.path.abspath(os.path.join(local_path, os.pardir))

	#folder_data = os.path.join(parent_dir, current_date_time)
	folder_data = os.path.join(parent_dir, filename_common)

	if os.path.isdir(folder_data) == False:
		os.mkdir(folder_data)

	return folder_data

# ===================================== MAIN =====================================
if __name__ == '__main__':

    isDebug = True

    desired_width = 1280
    desired_height = 720
    desiredFrameSize = (desired_width,desired_height) # standard definition = (640, 480) => fps = 6; (1280,720) => fps = 10 or 30
    logs = []

    #trigger = "0"
    isTrigger = None
    sleeping_time_fps = 30/100

    [path_video, path_audio] = get_filename_path()
    
    # Establish UDP connection with a server (CCS) by creating a socket
    receiver_address = ('localhost', 50212)
    receiver_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    receiver_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    receiver_socket.bind(receiver_address) # Bind the socket to the receiving port
    
    try:
		#print('Listening for UDP messages')
        _thread.start_new_thread(receiver_thread, ())
    except:
        print ("Error: unable to start receiver thread.")
        quit()
    
    start_AVplayer()
    #print('Frame rate = ' + str(video_thread.fps) + ' fps')

    if isDebug:
        # Debug
        time.sleep(5)
        # trigger = "1"
        isTrigger = True
    else:
        while isTrigger is None:
            #print_logs("Waiting for trigger...")
            time.sleep(0.001)
    
    current_datetime = get_datetime()
    filename_common = current_datetime
    folder_data = define_storage_folder(filename_common)

    #print_logs("Waiting for multimedia files to close...")
    # while video_thread.isSourceOpen is True or audio_thread.isSourceOpen is True:
    #     time.sleep(0.01)
    
    while isTrigger is not False:
         time.sleep(0.01)

    print_logs("Writing metadata to file...")
    metadata_file = open(filename_common + '_metadata.txt', 'w')
    for log in logs:
        metadata_file.write(log)
    
    metadata_file.close()
    os.replace(os.path.join(os.getcwd(), filename_common + '_metadata.txt'), os.path.join(folder_data, filename_common + '_metadata.txt'))