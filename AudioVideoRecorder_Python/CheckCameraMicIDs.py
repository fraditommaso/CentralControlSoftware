import sys
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

import pyaudio


## TEST VIDEO 1 (it works)
# for x in range(2, 800):
#     print('Analyzing index ' + str(x))
#     camera_idx = x
#     try:
#         cap = cv2.VideoCapture(camera_idx)
#     except:
#         print('Error')

#     ret, frame = cap.read()
    
#     if(ret==True):
#         #cv2.imshow('Video', frame)
#         print('Camera found at index = ' + str(x))
#     else:
#         print('Camera not found at index = ' + str(x))
#         continue

#     if cv2.waitKey(1) & 0xFF == ord('q'):
#         exit()
#         break

#     cap.release()
#     cv2.destroyAllWindows


## TEST VIDEO 2 (this works: checks the device at specified index)
# camera_idx = 0 #701
# cap = cv2.VideoCapture(camera_idx)
# # ret, frame = cap.read()
# # print("Camera index = " + str(camera_idx) + " " + str(ret))

# while True:
#     ret, frame = cap.read()
#     cv2.imshow('Video', frame)


# TEST VIDEO 3 (this works)
# Index 0 = internal camera; index 1 = blocks the script (?); from index 2 it works and finds two cameras 
# at indices 700 (should be the internal camera again) and 701
source = 0
while source < 201:
    print('Checking camera at index ' + str(source))
    cap = cv2.VideoCapture(source) 
    if cap is None or not cap.isOpened():
        print('Warning: unable to open video source: ', source)
    else:
        print('\nFound active camera at index ' + str(source))
    source += 1


# ## TEST AUDIO 1 (this works perfectly to check the device's index)
# p = pyaudio.PyAudio()
# info = p.get_host_api_info_by_index(0)
# numdevices = info.get('deviceCount')
# for i in range(0, numdevices):
#    if (p.get_device_info_by_host_api_device_index(0, i).get('maxInputChannels')) > 0:
#        print("Input Device id ", i, " - ", p.get_device_info_by_host_api_device_index(0, i))


## TEST AUDIO 2 (record system audio)
p = pyaudio.PyAudio()
for i in range(0, p.get_device_count()):
    info = p.get_device_info_by_index(i)
    print ( str(info["index"]) +  ": \t %s \n \t %s \n" % (info["name"], p.get_host_api_info_by_index(info["hostApi"])["name"]))
    pass