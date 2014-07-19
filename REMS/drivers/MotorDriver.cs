using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Threading;
using System.IO.Ports;

namespace REMS.drivers
{
    class MotorDriver
    {
        private SerialPort serial = new SerialPort();
        private string recieved_data;
        private Boolean mWaitingForFinish = false;
        //private Boolean mWaitingForReply = false;
        private String mReply = "";
        //private Boolean _connected = false;
        private Boolean _wasHomed = false;

        public MotorDriver()
        {

        }

        public Boolean Connect(String aCOM)
        {
            try
            {
                //Sets up serial port
                serial.PortName = "COM" + aCOM;
                serial.BaudRate = 9600;
                serial.Handshake = System.IO.Ports.Handshake.None;
                serial.Parity = Parity.None;
                serial.DataBits = 8;
                serial.StopBits = StopBits.One;
                serial.ReadTimeout = 200;
                serial.WriteTimeout = 50;
                serial.Open();

                //Sets button State and Creates function call on data recieved
                serial.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(Recieve);
                return false;
            }
            catch
            {
                return true;
            }
        }

        public Boolean isConnected()
        {
            return serial.IsOpen;
        }

        public void Disconnect()
        {
            try // just in case serial port is not open could also be acheved using if(serial.IsOpen)
            {
                serial.Close();
            }
            catch
            {
            }
        }

        private delegate void DataReceivedDelegate(string text);
        private void Recieve(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            // Collecting the characters received to our 'buffer' (string).
            recieved_data = serial.ReadExisting();
            App.Current.Dispatcher.Invoke(DispatcherPriority.Send, new DataReceivedDelegate(WriteData), recieved_data);
        }

        private void WriteData(string text)
        {
            if (text == "^")
                mWaitingForFinish = false;

            //if (mWaitingForReply)
            //{
            //    mWaitingForReply = false;
            mReply = text;
            //}

            Console.WriteLine("Received: " + text);
        }

        public void WaitUntilFinished()
        {
            mWaitingForFinish = true;
            while (mWaitingForFinish) ;
        }

        public string ReadReply()
        {
            return mReply;
        }

        public void sendCommand(string aCommand)
        {
            mReply = "";
            SerialCmdSend(aCommand);
        }

        private void SerialCmdSend(string data)
        {
            if (serial.IsOpen)
            {
                try
                {
                    // Send the binary data out the port
                    byte[] hexstring = Encoding.ASCII.GetBytes(data);
                    //There is a intermitant problem that I came across
                    //If I write more than one byte in succesion without a 
                    //delay the PIC i'm communicating with will Crash
                    //I expect this id due to PC timing issues ad they are
                    //not directley connected to the COM port the solution
                    //Is a ver small 1 millisecound delay between chracters
                    foreach (byte hexval in hexstring)
                    {
                        byte[] _hexval = new byte[] { hexval }; // need to convert byte to byte[] to write
                        serial.Write(_hexval, 0, 1);
                        Thread.Sleep(1);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to SEND" + data);
                    Console.WriteLine(ex);
                }
            }
        }


        public void home()
        {
            _wasHomed = false;

            int lStatus = 0;
            sendCommand("F,C,I2M-0,R");
            WaitUntilFinished();

            sendCommand("F,C,(I3M-0,I1M-0,)R");
            WaitUntilFinished();

            sendCommand("N");
            Console.WriteLine("Update Home Position: " + (lStatus == 1 ? "Success" : "Failed"));

            _wasHomed = true;
        }

        public void move(int aXPos, int aYPos, int aZPos)
        {
            // if the motors have never been homed, do that now!
            if (!_wasHomed)
                this.home();

            // Since our motor step size is 0.005mm, we have to
            // multiply our steps by 200
            string lXPos = Convert.ToString(aXPos * 200);
            string lYPos = Convert.ToString(aYPos * 200);
            string lZPos = Convert.ToString(aZPos * 200);

            //int lStatus = 0;
            String lCommand = "F,C,(IA3M" + lXPos + ",IA1M" + lYPos + ",IA2M" + lZPos + ",)R";
            sendCommand(lCommand);

            WaitUntilFinished();
        }

        public void stop()
        {
            sendCommand("K");
        }

        public string getMotorStatus()
        {
            sendCommand("V");
            return ReadReply();
        }
    }
}

