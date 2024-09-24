
# OPEN SOURCE API TO RECORD AUDIO AND VIDEO
# https://github.com/JRodrigoF/AVrecordeR

## Description
## VideoRecorder and AudioRecorder are two classes based on openCV and pyaudio, respectively. 
## By using multithreading these two classes allow to record simultaneously video and audio.
## ffmpeg is used for muxing the two signals
## A timer loop is used to control the frame rate of the video recording. This timer as well as
## the final encoding rate can be adjusted according to camera capabilities

## Usage
## numpy, PyAudio and Wave need to be installed
## install openCV, make sure the file cv2.pyd is located in the same folder as the other libraries
## install ffmpeg and make sure the ffmpeg .exe is in the working directory
##
## start_AVrecording(filename) # function to start the recording
## stop_AVrecording(filename)  # "" ... to stop it


## Updates 
#	Francesco Di Tommaso, PhD Student
#		Advanced Robotics and Human Centered Technologies, CREO Lab
#		Universita Campus Bio-Medico di Roma
#		Via Alvaro del Portillo 21, 00128 Roma, Italy
# 		email: f.ditommaso@unicampus.it

# v01 (17.03.22)
#	- Major modifications
#	- File storage
#	- UDP Socket to receive trigger

# v02
#	- Minor modifications

# v03
#	- Implemented trigger
#	- Code properly working

# v04
#	- Attempted to modify the capturing process of frames with time_between_two_frames, but didn't work as good
#	as with time.sleep.

# v05
# - Changed file_manager to save media files in a parent folder
# - Start py file with argument (filename)

# v06
#	- Improved audio/video merging for better quality

# v07
#	- Added datetime timestamp (to ms) to screen to check synchronization

from fileinput import filename
from operator import indexOf
import sys
from xml.dom import NoModificationAllowedErr
import cv2
import pyaudio
import wave
import threading
import time
import subprocess
import os

import datetime
import socket
import _thread

import ffmpeg


# ===================================== CLASSES =====================================
# Class to manage the video source
class VideoRecorder():
	
	# Video class based on openCV 
	def __init__(self):
		
		#Debug
		print_logs("Setting up camera...")

		self.open = True
		self.device_index = cameraID
		self.fps = desired_fps #6  # fps should be the minimum constant rate at which the camera can
		#self.fourcc = "MJPG"       # capture images (with no decrease in speed over time; testing is required)
		self.fourcc = 'XVID'	#this works
		self.frameSize = desiredFrameSize # video formats and sizes also depend and vary according to the camera used
		self.video_filename = track_video_filename + output_format_video
		self.video_cap = cv2.VideoCapture(self.device_index, cv2.CAP_DSHOW) #cv2.CAP_DSHOW to speed up camera opening
		
		#new
		self.video_cap.set(cv2.CAP_PROP_FPS, self.fps)

		self.video_writer = cv2.VideoWriter_fourcc(*self.fourcc)
		self.video_out = cv2.VideoWriter(self.video_filename, self.video_writer, self.fps, self.frameSize)
		self.frame_counts = 0
		self.stop_time_video = None
		
		self.video_cap.set(cv2.CAP_PROP_FRAME_WIDTH,desired_width)
		self.video_cap.set(cv2.CAP_PROP_FRAME_HEIGHT,desired_height)

	# Video starts being recorded 
	def record(self):

		isRecordingStarted = False
		while(self.open==True):

			try:
				ret, video_frame = self.video_cap.read()

				# Show frame
				if ret == True:
					if isTrigger is True:
						color = (255, 255, 255)
					else:
						color = (0, 0, 255)
					cv2.putText(video_frame, datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3], (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 0.45, color, 1, cv2.LINE_AA)
					
					cv2.imshow('video_frame', video_frame)

					if cv2.waitKey(1) & 0xFF == ord('q'):
						exit()
						break

					# # Record frame
					# if trigger == "1":
					# 	if isRecordingStarted == False:
					# 		isRecordingStarted = True
					# 		self.start_time_video = time.time()
					# 		print_logs("Started recording video at " + datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3])

					# 	try:
					# 		self.video_out.write(video_frame)
					# 		self.frame_counts += 1
					# 	except:
					# 		print_logs("Cannot write frame to video. Exit the process")
					# elif trigger == "0":
					# 	if isRecordingStarted == False:
					# 		#time.sleep(0.01)
					# 		continue
					# 	elif isRecordingStarted == True:
					# 		self.stop()
					# 		#print_logs("Stopped recording video at " + datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f'))

					if isTrigger is True:
						if isRecordingStarted is False:
							isRecordingStarted = True
							self.start_time_video = time.time()
							print_logs("Started recording video at " + datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3])

						try:
							self.video_out.write(video_frame)
							self.frame_counts += 1
						except:
							print_logs("Cannot write frame to video. Exit the process")
					elif isTrigger is False:
							self.stop()
					elif isTrigger is None:
						continue
				else:
					break
			except:
				print_logs ("An exception has occurred while grabbing a frame. Ending the recording.")
				break
		
	# Finishes the video recording therefore the thread too
	def stop(self):
		
		if self.open==True:
			self.stop_time_video = time.time()

			self.open=False
			self.video_out.release()
			print_logs("Stopped recording video at " + datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3])
			self.video_cap.release()
			cv2.destroyAllWindows()
			
		else: 
			pass

	# Launches the video recording function using a thread			
	def start(self):
		video_thread = threading.Thread(target=self.record)
		video_thread.start()
		

# Class to manage the audio source
class AudioRecorder():
	
	# Audio class based on pyAudio and Wave
	def __init__(self):
		
		#Debug
		print_logs("Setting up microphone...")

		self.open = True
		self.rate = 44100
		self.frames_per_buffer = 1024
		self.channels = 2
		self.format = pyaudio.paInt16
		self.audio_filename = track_audio_filename + output_format_audio  #self.audio_filename = "temp_audio.wav" 
		self.audio = pyaudio.PyAudio()
		self.stream = self.audio.open(format=self.format,
									  channels=self.channels,
									  rate=self.rate,
									  input=True,
									  frames_per_buffer = self.frames_per_buffer,
									  input_device_index=microphoneID)
		self.audio_frames = []
		self.frame_counts = 0
		self.stop_time_audio = None

	# Audio starts being recorded
	def record(self):
		self.stream.start_stream()

		isRecordingStarted = False

		# Previous version that works
		while(self.open == True):
			if triggerAudio == "1":

				if isRecordingStarted == False:
					isRecordingStarted = True
					self.start_time_audio = time.time()
					print_logs("Started recording audio at " + datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3])

				try:
					data = self.stream.read(self.frames_per_buffer)
					self.audio_frames.append(data)
					self.frame_counts += 1
				except:
					print_logs("Unable to write audio frame.")

				if self.open==False:
					break
			elif triggerAudio == "0":
				if isRecordingStarted == False:
					continue
				elif isRecordingStarted == True:
					self.stop()

		# New version does not work continuously (gaps in audio track)
		# while(self.open == True):
		# 	if isTrigger is True: #trigger == "1":

		# 		if isRecordingStarted == False:
		# 			isRecordingStarted = True
		# 			self.start_time_audio = time.time()
		# 			print_logs("Started recording audio at " + datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3])

		# 		data = self.stream.read(self.frames_per_buffer)
		# 		self.audio_frames.append(data)
		# 		self.frame_counts += 1

		# 	elif isTrigger is False:
		# 		self.stop()
		# 	elif isTrigger is None:
		# 		continue
		
	# Finishes the audio recording therefore the thread too    
	def stop(self):
	   
		if self.open==True:
			try:
				self.open = False
				self.stream.stop_stream()
				self.stream.close()
				self.stop_time_audio = time.time()
				print_logs("Stopped recording audio at " + datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3])
				self.audio.terminate()
				
				waveFile = wave.open(self.audio_filename, 'wb')
				waveFile.setnchannels(self.channels)
				waveFile.setsampwidth(self.audio.get_sample_size(self.format))
				waveFile.setframerate(self.rate)
				waveFile.writeframes(b''.join(self.audio_frames))
				waveFile.close()
			except:
				print_logs("Unable to write audio file.")

			
		pass
	
	# Launches the audio recording function using a thread
	def start(self):
		audio_thread = threading.Thread(target=self.record)
		audio_thread.start()

# ===================================== METHODS =====================================
# Method to start recording from audio and video sources
def start_AVrecording(audiovideo_filename):

	global video_thread
	global audio_thread
	
	video_thread = VideoRecorder()
	audio_thread = AudioRecorder()

	audio_thread.start()
	video_thread.start()

	return audiovideo_filename

# Method to start recording from video source
def start_video_recording(filename):
				
	global video_thread
	
	video_thread = VideoRecorder()
	video_thread.start()

	return filename

# Method to start recording from audio source
def start_audio_recording(filename):
				
	global audio_thread
	
	audio_thread = AudioRecorder()
	audio_thread.start()

	return filename

# Method to stop recording from audio and video sources
def stop_AVrecording(audiovideo_filename):

	global isVideoReEncoded

	# Stop recording cannot be managed here because this method runs in the main thread which can only perform one operation at a time, 
	# hence they will be performed in sequence and the stop recording command will be inevitably received at different times.
	# video_thread.stop()
	# audio_thread.stop()

	# If audio and video thread are stopped internally, main thread must block until they are both stopped
	while audio_thread.stop_time_audio is None or video_thread.stop_time_video is None:
		time.sleep(0.01)
	
	# Recap audio and video properties
	audio_length = audio_thread.stop_time_audio - audio_thread.start_time_audio
	audio_fps = audio_thread.frame_counts / audio_length
	print_logs("\n")
	print_logs("Audio Properties")
	print_logs("Total frames: " + str(audio_thread.frame_counts))
	print_logs("Elapsed time: " + str(audio_thread.stop_time_audio) + " - " + str(audio_thread.start_time_audio) + " = " + str(audio_length))
	print_logs("Recorded fps: " + str(audio_fps) + "\n")

	elapsed_time = video_thread.stop_time_video - video_thread.start_time_video
	recorded_fps = video_thread.frame_counts / elapsed_time
	print_logs("Video Properties")
	print_logs ("Total frames: " + str(video_thread.frame_counts))
	print_logs ("Elapsed time: " + str(video_thread.stop_time_video) + " - " + str(video_thread.start_time_video) + " = " + str(elapsed_time))
	print_logs ("Recorded fps: " + str(recorded_fps) + "\n")

	# # This works
	# # Merging audio and video signal
	# if abs(recorded_fps - desired_fps) >= 0.3:    # If the fps rate was higher/lower than expected, re-encode it to the expected

	# 	isVideoReEncoded = True			
	# 	print ("Re-encoding...")
	# 	#cmd = "ffmpeg -r " + str(recorded_fps) + " -i temp_video.avi -pix_fmt yuv420p -r 6 temp_video2.avi"
	# 	cmd = "ffmpeg -r " + str(recorded_fps) + " -i " + track_video_filename + ".avi" + " -pix_fmt yuv420p -r " + str(desired_fps) + " " + track_video_filename + "2.avi"
	# 	#subprocess.call(cmd, shell=True)
	# 	subprocess.call(cmd, shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT) #abort printing on console
	
	# 	print ("Muxing...")
	# 	#cmd = "ffmpeg -ac 2 -channel_layout stereo -i temp_audio.wav -i temp_video2.avi -pix_fmt yuv420p " + audiovideo_filename + ".avi"
	# 	cmd = "ffmpeg -ac 2 -channel_layout stereo -i " + track_audio_filename + ".wav" + " -i " + track_video_filename + "2.avi" + " -pix_fmt yuv420p " + audiovideo_filename + ".avi"
	# 	#subprocess.call(cmd, shell=True)
	# 	subprocess.call(cmd, shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT) #abort printing on console
	
	# else:
		
	# 	print("Merging audio and original video...")
	# 	#print ("Normal recording\nMuxing")
	# 	#cmd = "ffmpeg -ac 2 -channel_layout stereo -i temp_audio.wav -i temp_video.avi -pix_fmt yuv420p " + audiovideo_filename + ".avi"
		
	# 	# Original: it works but the resolution is lower
	# 	# cmd = "ffmpeg -ac 2 -channel_layout stereo -i " + track_audio_filename + ".wav" + " -i " + track_video_filename + ".avi" + " -pix_fmt yuv420p " + audiovideo_filename + ".avi"
		
	# 	while os.path.exists(os.path.join(os.getcwd(), track_video_filename + '.avi')) == False or os.path.exists(os.path.join(os.getcwd(), track_audio_filename + '.wav')) == False:
	# 		print("Waiting for audio and video files to be created.\n")
	# 		time.sleep(1)
		
	# 	# #subprocess.call(cmd, shell=True)
	# 	 #subprocess.call(cmd, shell=True, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT) #abort printing on console

	# 	# This works
	# 	input_video = ffmpeg.input(os.path.join(os.getcwd(), track_video_filename + '.avi'))
	# 	input_audio = ffmpeg.input(os.path.join(os.getcwd(), track_audio_filename + '.wav'))
	# 	output_media = os.path.join(os.getcwd(), audiovideo_filename + '.avi')
	# 	ffmpeg.concat(input_video, input_audio, v=1, a=1).output(output_media).run()
		
	# Test
	# print('Waiting to merge audio and video...')
	# time.sleep(4)
	# print('Merging audio and video...')
	# video_stream = ffmpeg.input(video_thread.video_filename)
	# audio_stream = ffmpeg.input(audio_thread.audio_filename)
	# ffmpeg.output(audio_stream, video_stream, audiovideo_filename + output_format_mixed).run(overwrite_output=True)

	while os.path.exists(os.path.join(os.getcwd(), track_video_filename + '.avi')) == False or os.path.exists(os.path.join(os.getcwd(), track_audio_filename + '.wav')) == False:
		print_logs("Waiting for audio and video files to be created.\n")
		time.sleep(1)

	try:
		video_stream = ffmpeg.input(video_thread.video_filename)
		audio_stream = ffmpeg.input(audio_thread.audio_filename)
		print_logs('Merging audio and video...')
		ffmpeg.output(audio_stream, video_stream, audiovideo_filename + output_format_mixed).run(overwrite_output=True)
	except:
		print_logs("Unable to merge audio and video tracks.")


# Method to manage files storage
def file_manager(folder_data, track_video_filename, track_audio_filename, audiovideo_filename):

	# Get local path of the folder where the routine is stored
	local_path = os.getcwd()

	# media_folder = 'Media'
	# if os.path.isdir(media_folder) == False:
	# 	os.mkdir(media_folder)


	# folder_data = os.path.join(media_folder, current_date_time)

	# if os.path.isdir(folder_data) == False:
	# 	os.mkdir(folder_data)

	relative_video_filename = os.path.join(local_path, track_video_filename + output_format_video)
	absolute_video_filename = os.path.join(folder_data, track_video_filename + output_format_video)

	relative_audio_filename = os.path.join(local_path, track_audio_filename + output_format_audio)
	absolute_audio_filename = os.path.join(folder_data, track_audio_filename + output_format_audio)	

	relative_mixed_filename = os.path.join(local_path, audiovideo_filename + output_format_mixed)
	absolute_mixed_filename = os.path.join(folder_data, audiovideo_filename + output_format_mixed)		

	os.replace(relative_video_filename, absolute_video_filename)
	os.replace(relative_audio_filename, absolute_audio_filename)
	os.replace(relative_mixed_filename, absolute_mixed_filename)

	if isVideoReEncoded:
		relative_video2_filename = os.path.join(local_path, track_video_filename + '2' + output_format_mixed)
		absolute_video2_filename = os.path.join(folder_data, track_video_filename + '2' + output_format_mixed)
		os.replace(relative_video2_filename, absolute_video2_filename)

def file_manager_audio_and_video(folder_data, track_video_filename, track_audio_filename):

	local_path = os.getcwd()

	# media_folder = 'Media'
	# if os.path.isdir(media_folder) == False:
	# 	os.mkdir(media_folder)


	# folder_data = os.path.join(media_folder, current_date_time)

	# if os.path.isdir(folder_data) == False:
	# 	os.mkdir(folder_data)

	relative_video_filename = os.path.join(local_path, track_video_filename + output_format_video)
	absolute_video_filename = os.path.join(folder_data, track_video_filename + output_format_video)

	relative_audio_filename = os.path.join(local_path, track_audio_filename + output_format_audio)
	absolute_audio_filename = os.path.join(folder_data, track_audio_filename + output_format_audio)		

	os.replace(relative_video_filename, absolute_video_filename)
	os.replace(relative_audio_filename, absolute_audio_filename)

	if isVideoReEncoded:
		relative_video2_filename = os.path.join(local_path, track_video_filename + '2' + output_format_mixed)
		absolute_video2_filename = os.path.join(folder_data, track_video_filename + '2' + output_format_mixed)
		os.replace(relative_video2_filename, absolute_video2_filename)

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

# Method to start a thread to listen from a server (CCS) and receive a trigger signal
def receiver_thread():
	global isTrigger

	global triggerAudio
	while True:
		data, addr = receiver_socket.recvfrom(3)
		trigger = data.decode("utf-8")
		#print('Trigger = ' + trigger)
		print_logs('Trigger = ' + trigger)
		triggerAudio = trigger

		if trigger == "1":
			isTrigger = True
		elif trigger == "0":
			isTrigger = False
		else:
			isTrigger = []

def print_logs(message):
	log = datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S.%f')[:-3] + " - " + message + "\n"
	logs.append(log)
	print(log)

# ===================================== MAIN =====================================
if __name__== "__main__":

	isDebug = False
	logs = []

	# Input Arguments
	current_date_time = get_datetime()
	if len(sys.argv) > 1:
		filename_common = sys.argv[1]
		print_logs("AudioVideoRecorder routine started with filename " + filename_common)
		folder_data = define_storage_folder("Data")
	else:
		filename_common = current_date_time
		print_logs("AudioVideoRecorder routine started without filename. Setting to " + filename_common)
		folder_data = define_storage_folder(filename_common)

	# Global variables (with filename as input argument)
	track_video_filename = filename_common + '_video'
	track_audio_filename = filename_common + '_audio'
	audiovideo_filename = filename_common + '_mixed'
	
	receiverPort = 50212

	# Get current microphone index
	print_logs("Getting microphone index...")
	mic_idx = -1
	#mic_name = "HD Pro Webcam"
	#mic_name = "C922 Pro Stream"
	mic_name = "Logitech BRIO"
	p = pyaudio.PyAudio()
	for i in range(0, p.get_device_count()):
		info = p.get_device_info_by_index(i)
		print ( str(info["index"]) +  ": \t %s \n \t %s \n" % (info["name"], p.get_host_api_info_by_index(info["hostApi"])["name"]))
		if mic_name in info["name"]: #info["name"].cont == mic_name:
			mic_idx = info["index"]
			print_logs("Found " + mic_name + " at index " + str(mic_idx) + "\n")
			break
		pass

	if mic_idx == -1:
		print_logs("WARNING: Could not find " + mic_name + ". Setting microphone to default device.")
		mic_idx = 0
	
	cameraID = 0 # 0 = internal camera (or for workstation); 1 = external camera; 701 = external camera Logitech 920
	microphoneID = mic_idx # 0 = internal microphone; 1 = external microphone Logitech 920
	desired_fps = 10 # original value: 6; webcam can capture at: 30; logic saturates fps at: 10
	desired_width = 1280
	desired_height = 720
	desiredFrameSize = (desired_width,desired_height) # standard definition = (640, 480) => fps = 6; (1280,720) => fps = 10 or 30
	output_format_audio = '.wav'
	output_format_video = '.avi'
	output_format_mixed = '.mp4'

	# if(desired_fps == 6):
	# 	sleeping_time_fps = 15/100
	# elif(desired_fps == 10):
	# 	sleeping_time_fps = 10/100
	# else:
	# 	sleeping_time_fps = 7/100

	#trigger = "0"
	triggerAudio = "0"
	isTrigger = []
	isVideoReEncoded = False

	# Establish UDP connection with a server (CCS) by creating a socket
	receiver_address = ('localhost', receiverPort)
	receiver_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
	receiver_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
	receiver_socket.bind(receiver_address) # Bind the socket to the receiving port

	try:
		#print('Listening for UDP messages')
		_thread.start_new_thread(receiver_thread, ())
	except:
		print ("Error: unable to start receiver thread.")
		quit()
	
	# Manage recording
	start_AVrecording(audiovideo_filename)  
	
	if isDebug:
		# Debug to check script without udp trigger from ccs
		# print('starting camera and microphone...')
		time.sleep(3)
		# print('start recording...')
		trigger = "1"
		triggerAudio = trigger
		isTrigger = True
		time.sleep(120)
		trigger = "0"
		triggerAudio = trigger
		isTrigger = False
	else:
		# This works with UDP trigger 
		while(isTrigger is None or isTrigger is False):
			#print('Waiting for trigger to start...')
			time.sleep(0.001)

		while(isTrigger is True):
			#print('Waiting for trigger to stop...')
			time.sleep(0.001)
	
	# # # This works with UDP trigger
	# # while(trigger == "0"):
	# # 	#print('Waiting for trigger to start...')
	# # 	time.sleep(0.001)

	# # while(trigger == "1"):
	# # 	#print('Waiting for trigger to stop...')
	# # 	time.sleep(0.001)

	#print('Stop recording...')
	stop_AVrecording(audiovideo_filename)

	# Wait to move files in their folder
	#time.sleep(10)
	
	print_logs("Audio and video recordings saved successfully.")
	print_logs("Checking if mixed file has been created...")
	start_time_check = time.time()
	try:
		while True:
			if os.path.exists(os.path.join(os.getcwd(), audiovideo_filename + output_format_mixed)):
				print_logs("Mixed file created, moving files...")
				file_manager(folder_data, track_video_filename, track_audio_filename, audiovideo_filename)
				break
			
			if time.time()-start_time_check > 30:
				print_logs("Elapsed time has reached the max value of 30s.")
				print_logs("Moving audio and video track...")
				file_manager_audio_and_video(folder_data, track_video_filename, track_audio_filename)
				break
	except:
		print_logs("Error while checking if mixed file has been created. Shutting down the routine.")
			
	print_logs("Saving metadata to txt file...")
	metadata_file = open(filename_common + '_metadata.txt', 'w')
	for log in logs:
		metadata_file.write(log)
	metadata_file.close()
	os.replace(os.path.join(os.getcwd(), filename_common + '_metadata.txt'), os.path.join(folder_data, filename_common + '_metadata.txt'))
	print_logs("Metadata saved successfully.")