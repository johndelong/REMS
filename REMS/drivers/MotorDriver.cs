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
        private Boolean mStopped = false;
        private String mReply = "";
        private Boolean mWasHomed = false;

        public MotorDriver()
        {
            //mIsOnlineTimer = new DispatcherTimer();
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

                return true;
            }
            catch
            {
                return false;
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

            mReply = text;

            //Console.WriteLine("Received: " + text);
        }

        public void WaitUntilFinished()
        {
            mWaitingForFinish = true;
            while (mWaitingForFinish && isOnline() && !mStopped) ;
        }

        public Boolean isMoving
        {
            get { return mWaitingForFinish; }
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

        private void homeMotors()
        {
            mWasHomed = false;
            if (!mStopped)
            {
                sendCommand("F,C,I2M-0,R");
                WaitUntilFinished();
            }

            if (!mStopped)
            {

                sendCommand("F,C,(I3M-0,I1M-0,)R");
                WaitUntilFinished();

                sendCommand("N");

                mWasHomed = true;
            }
        }

        public Boolean move(int aXPos, int aYPos, int aZPos)
        {
            // if want to move the motors, we are going to assume that
            // the motors shouldn't be stopped anymore
            mStopped = false;

            // if the motors have never been homed, do that now!
            if (!mWasHomed || (aXPos == 0 && aYPos == 0 && aZPos == 0))
                this.homeMotors();

            if (!mStopped)
            {
                // Since our motor step size is 0.005mm, we have to
                // multiply our steps by 200 to get 1mm increments
                string lXPos = Convert.ToString(aXPos * 200);
                string lYPos = Convert.ToString(aYPos * 200);
                string lZPos = Convert.ToString(aZPos * 200);


                String lCommand = "F,C,(IA3M" + lXPos + ",IA1M" + lYPos + ",IA2M" + lZPos + ",)R";
                sendCommand(lCommand);

                WaitUntilFinished();
            }

            return !mStopped;
        }

        public void stop()
        {
            // Don't need to wait for a response anymore
            mWaitingForFinish = false;
            mStopped = true;
            sendCommand("D");
        }

        /*private void motorStatus_tick(object sender, EventArgs e)
        {
            getMotorStatus();
        }*/

        /*private void getMotorStatus()
        {
            Task.Factory.StartNew(() =>
            {
                sendCommand("V");

                Thread.Sleep(500);

                Console.WriteLine((mReply == "") ? "Offline" : "Online");
                mMotorsOnline = (mReply != "") ? true : false;
            });
        }*/

        public Boolean isOnline()
        {
            return serial.IsOpen;
        }
    }
}

