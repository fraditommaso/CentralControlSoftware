// Author: Francesco Di Tommaso, PhD student

// UDP Socket

// =======================================================================================
// Packages
// =======================================================================================
var UDP = require('dgram');
//var buffer = require('buffer');

// =======================================================================================
// Properties
// =======================================================================================


// =======================================================================================
// Class definition
// =======================================================================================

module.exports = class UDPSocket
{
    constructor(_ip, _portReceiver, _portTransmitter)
    {
        this.ip = _ip;
        this.portReceiver = _portReceiver;
        this.portTransmitter = _portTransmitter;

        // Create UDP Socket for the client
        this.socket = UDP.createSocket('udp4');
    }

    // -----------------------------------------------------------------------------------
    // -- Methods --
    // -----------------------------------------------------------------------------------
    ConnectToServer()
    {
        // Bind the socket to a port
        try {
            this.socket.bind(this.portTransmitter, this.ip);
            //console.log('Binding the socket to the transmitter port of the local node (' + this.portTransmitter + ').');
        }
        catch (e) {
            console.log(e);
            this.socket.close();
        }

        // // Debug test to check if packet are sent over udp socket to CCS
        // let date_obj = new Date();
        // var data = '\n' + date_obj + '- Xsens DOT Server started.\n'
        // var message = Buffer.from(data);

        // try{
        //     this.socket.send(message, 0, message.length, this.portReceiver, this.ip, function(err){
        //         if(err) throw err
        //         //this.socket.close;
        //     });
        //     //console.log('UDP message sent to ' + this.ip + ':' + this.portReceiver + ' - ' + message);
        // } catch (e) {
        //     console.log(e)
        //     console.log('Unexpected error while establishing UDP connection.');
        // }
    }
    
    SendMessage(data)
    {
        var message = Buffer.from(data);

        try{
            this.socket.send(message, 0, message.length, this.portReceiver, this.ip, function(err){
                if(err) throw err
                //client.close;
            });
            //console.log('UDP message sent to ' + this.ip + ':' + this.portReceiver + " - " + message);
        } catch (e) {
            console.log(e)
            console.log('Unexpected error while establishing UDP connection.');
        }
    }

    StartListeningMessages()
    {
        try{
            this.socket.bind(this.portReceiver, this.ip);
            //console.log('Binding the socket to the receiver port of the local node (' + this.portReceiver + ').');
            
            this.socket.on('listening', function(){});
            //console.log('Xsens DOT Server ready to receive messages from ' + this.ip + ':' + this.portTransmitter)
        } catch{
            console.log(e)
            console.log('Unexpected error while establishing UDP connection.');
        }
    }

    ListenMessage()
    {
        //var message;
        try{
            this.socket.on('message', (message, senderInfo)=>{
                console.log('Message from ' + senderInfo.address + ':' + senderInfo.port + ' - ' + message); //this is correctly printed!!
                //return this.message.toString();
                //return callback(message);
                //return this.socket.message;
            });
            //return message; // goes to catch
        } catch {
            console.error();
            console.log('Unexpected error while receiving UDP message from Xsens DOT Server.');
        }
        //return this.socket.message.toString();
        //return this.message;
    }
}

// =======================================================================================
// Export the UDPSocket class
// =======================================================================================
//module.exports = UDPSocket;