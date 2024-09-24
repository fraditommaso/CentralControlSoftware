#!/usr/bin/python

# CODE TO READ GSR AND PPG VALUES FROM SHIMMER SENSORS

# v0: example code from shimmer adapted to python 3.8 version
# v1: write data to file
# v2: connect to more than one sensor
# v3: trigger signal received from UDP

# *	AUTHORS: 
# *   
#     Alessia Noccaro, PhD 
# *	NeXT: Neurophysiology and Neuroengineering of Human-Technology Interaction Research Unit
# *	Universita Campus Bio-Medico di Roma
# *	Via Alvaro del Portillo 21, 00128 Roma, Italy
# *	email: a.noccaro@unicampus.it

#     Francesco Di Tommaso, PhD Student
# *   Advanced Robotics and Human Centered Technologies, CREO Lab
# *   Universita Campus Bio-Medico di Roma
# *	Via Alvaro del Portillo 21, 00128 Roma, Italy
# *	email: f.ditommaso@unicampus.it


import sys, struct, serial
import datetime
import os.path
import socket
import _thread


isSynchDone = False
trigger = "0"

# ====================================== METHODS ======================================
# Method to get a filename to store data
def get_filename(subj_ID, COM_port):

   folder_data = 'SHIMMER_DATA'

   if os.path.isdir(folder_data) == False:
      os.mkdir(folder_data)

   now = datetime.datetime.now()
   str_year = f'{now.year:04d}'
   str_month = f'{now.month:02d}'
   str_day = f'{now.day:02d}'
   str_hour = f'{now.hour:02d}'
   str_minute = f'{now.minute:02d}'
   str_second = f'{now.second:02d}'

   datetime_str = (str_year + "-" + str_month + "-" + str_day + "_" + str_hour + "." + str_minute + "." + str_second)

   local_path = os.getcwd()
   #parent_dir = os.path.abspath(os.path.join(local_path, os.pardir))

   #filename = os.path.join(parent_dir, folder_data, ("subj_" + subj_ID + "_" + COM_port + "_" + datetime_str + ".txt"))
   filename = os.path.join(local_path, folder_data, ("subj_" + subj_ID + "_" + COM_port + "_" + datetime_str + ".txt"))
   #filename = os.path.join("c:", folder_data, ("subj_" + subj_ID + "_" + COM_port + "_" + datetime_str + ".txt") )
   return filename

# Method to get the number of connected sensors and COMs
def get_COM():
   Nsensors = len(sys.argv)-1
   print("Number of Shimmer3 sensors: ", Nsensors)

   COM_ports = []
   for isensor in range(Nsensors):
      COM_ports.append(sys.argv[1+isensor])
      print("Sensor ", isensor+1, " on port ", COM_ports[isensor])

   return COM_ports

# # Method to start a thread to listen from a server (CCS) and receive a trigger signal
# def receiver_thread():
#    global trigger
#    while True:
#       UDPdata, addr = receiver_socket.recvfrom(2)
#       # trigger = UDPdata.decode("utf-8")
#       trigger = repr(int.from_bytes(UDPdata[:1],"big"))
#       #trigger = UDPdata.decode("ASCII")
#       #print ("Received message inside thread:", trigger)
#       #print ("Received message inside thread:"+ trigger)

# Method to start a thread to listen from a server (CCS) and receive a trigger signal
def receiver_thread():
	global trigger
	while True:
		UDPdata, addr = receiver_socket.recvfrom(3)
		trigger = UDPdata.decode("utf-8")
		#print('Trigger = ' + trigger)

def wait_for_ack(isensor):
   ddata = ""
   ack = struct.pack('B', 0xff)
   while ddata != ack:
      ddata = ser[isensor].read(1)
      #print ("0x%02x" % ord(ddata[0]))
   return

# ====================================== MAIN ======================================
COM_ports = get_COM()
Nsensors = len(COM_ports)

# Establish UDP connection with a server (CCS) by creating a socket
receiver_address = ('localhost', 50122) 
receiver_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
receiver_socket.bind(receiver_address) # Bind the socket to the receiving port
print('starting up on {} port {}'.format(*receiver_address))

try:
   _thread.start_new_thread(receiver_thread, ())
except:
   print ("Error: unable to start receiver thread.")
   quit()

transmitter_address = ('localhost', 50124)
transmitter_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
print('sendind UDP messages to {}'.format(*transmitter_address) )

# #Debug - Francesco 24.01.22
# message = b"Shimmer is connected successfully"
# transmitter_socket.sendto(message, transmitter_address)
# # Debug
# print("UDP Packet storing message (", message, ") sent to ", transmitter_address)


# Connect to shimmer and record data
# subj_ID = input("Enter subject's ID: ")
subj_ID = '01' #change this to retrieve variable automatically

if len(sys.argv) < 2:
   print ("no device specified")
   print ("You need to specify the serial port of the device you wish to connect to")
   print ("example:")
   print ("   aAccel5Hz.py Com12")
   print ("or")
   print ("   aAccel5Hz.py /dev/rfcomm0")
else:

   # Open port
   if len(sys.argv) > 2:
      Nsensor=len(sys.argv)-1
      #print(Nsensor)
      ser=[]
      for isensor in range(Nsensor):
         com=sys.argv[1+isensor]
         #print (com)
         ser.append(serial.Serial(com, 115200))
         ser[isensor].flushInput()
         print ("port opening, done.")
   else:
      Nsensor=1
      ser=[]
      #print (sys.argv[1])
      com=sys.argv[1]
      ser.append(serial.Serial(com, 115200))
      ser[0].flushInput()
      print ("port opening, done.")

   storage_file = {}
   for isensor in range(Nsensors):
      sensor_name = 'shimmer_{}'.format(isensor+1)
      filename_data = get_filename(subj_ID, COM_ports[isensor])
      storage_file[sensor_name] = open(filename_data, "a")

   # send the | set sensors command | sampling rate response | inquiry command | data packet
   for isensor in range(Nsensor):
      s=ser[isensor]
      s.write(struct.pack('BBBB', 0x08 , 0x04, 0x01, 0x00))
      wait_for_ack(isensor)   
      print ("sensor setting, done.")

   # Enable the internal expansion board power
   for isensor in range(Nsensor):
      ser[isensor].write(struct.pack('BB', 0x5E, 0x01))
      wait_for_ack(isensor)
      print ("enable internal expansion board power, done.")


   # send the set sampling rate command

   '''
   sampling_freq = 32768 / clock_wait = X Hz
   '''
   sampling_freq = 50
   clock_wait = (2 << 14) / sampling_freq

   #ser.write(struct.pack('<BH', 0x05, clock_wait))
   #Alessia 30/7/2021
   for isensor in range(Nsensor):
      ser[isensor].write(struct.pack('<BH', 0x05, int(clock_wait)))
      wait_for_ack(isensor)

   # send start streaming command
   for isensor in range(Nsensor):
      ser[isensor].write(struct.pack('B', 0x07))
      wait_for_ack(isensor)
      print ("start command sending, done.")   

   # Send ACK message to server
   if(isSynchDone == False):
      synchDoneMessage = "sync done"
      transmitter_socket.sendto(synchDoneMessage.encode(), transmitter_address)
      isSynchDone = True

   # read incoming data
   ddata={}
   data={}
   for isensor in range(Nsensor):   
      ddata[isensor]=""
   numbytes = 0
   framesize = 8 # 1byte packet type + 3byte timestamp + 2 byte GSR + 2 byte PPG(Int A13)
   
   # global trigger
   # trigger=repr(0)
   #print ("Packet Type\tTimestamp\tGSR\tPPG")
   try:
      while True:
         for isensor in range(Nsensor):
            sensor_name = 'shimmer_{}'.format(isensor+1)
            #ddata=""  
            #data=[]
            numbytes = 0
            while numbytes < framesize:
               #ddata += ser.read(framesize)
               #Alessia 30/7/2021
               #ddata[isensor] += str(ser[isensor].read(framesize))
               ddata[isensor] = ser[isensor].read(framesize)
               numbytes = len(ddata[isensor])            
            data[isensor] = ddata[isensor][0:framesize]
            ddata[isensor] = ddata[isensor][framesize:]
            numbytes = len(ddata[isensor])

            # read basic packet information
            #(packettype) = struct.unpack('B', data[0:1])
            #Alessia 30/7/2021
            (packettype) = struct.unpack('B', (data[isensor][0:1]))
             
            #(timestamp0, timestamp1, timestamp2) = struct.unpack('BBB', data[1:4])
            #Alessia 30/7/2021
            (timestamp0, timestamp1, timestamp2) = struct.unpack('BBB', data[isensor][1:4])

            # read packet payload
            #(PPG_raw, GSR_raw) = struct.unpack('HH', data[4:framesize])
            #Alessia 30/7/2021
            (PPG_raw, GSR_raw) = struct.unpack('HH', data[isensor][4:framesize])

            # get current GSR range resistor value
            Range = ((GSR_raw >> 14) & 0xff)  # upper two bits
            if(Range == 0):
               Rf = 40.2   # kohm
            elif(Range == 1):
               Rf = 287.0  # kohm
            elif(Range == 2):
               Rf = 1000.0 # kohm
            elif(Range == 3):
               Rf = 3300.0 # kohm

            # convert GSR to kohm value
            gsr_to_volts = (GSR_raw & 0x3fff) * (3.0/4095.0)
            GSR_ohm = Rf/( (gsr_to_volts /0.5) - 1.0)

            # convert PPG to milliVolt value
            PPG_mv = PPG_raw * (3000.0/4095.0)

            timestamp = timestamp0 + timestamp1*256 + timestamp2*65536
            #print ("0x%02x\t\t%5d,\t%4d,\t%4d" % (packettype[0], timestamp, GSR_ohm, PPG_mv))
            #===================================================
            #write data on file 
            print("%u\t%s\t0x%02x\t%5d\t%4d\t%4d" %(isensor+1,trigger, packettype[0],timestamp, GSR_ohm, PPG_mv), file=storage_file[sensor_name])
            #print("%u\t%s\t0x%02x\t%5d\t%4d\t%4d" %(isensor+1,trigger, packettype[0],timestamp/(1<<9), GSR_ohm, PPG_mv), file=storage_file[sensor_name])
            #===================================================

            if(trigger == "1"):
               # only 1 sensor
               udpPacket = str(isensor+1) + '\t' + trigger + '\t' + str(packettype[0]) + '\t' + str(timestamp) + '\t' + str(GSR_ohm) + '\t' + str(PPG_mv) + '\n'
               transmitter_socket.sendto(udpPacket.encode(), transmitter_address)



   except KeyboardInterrupt:
      #send stop streaming command
      for isensor in range(Nsensor):
         ser[isensor].write(struct.pack('B', 0x20))
         print ("stop command sent, waiting for ACK_COMMAND")
         wait_for_ack(isensor)
         print ("ACK_COMMAND received.")

      #close serial port
      for isensor in range(Nsensor):
         ser[isensor].close()
         print ("All done")
         #close file
         sensor_name = 'shimmer_{}'.format(isensor+1)
         storage_file[sensor_name].close()
         #file.close()