using System;
using System.Runtime.InteropServices;
//using System.Windows.Forms;

public class ClsVxmDriver
{
	/*******************************************************************************************************
	 *** The below code must have 	
	 *** [DllImport("kernel32.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)] 
	 *** Kind of layout On the line prior for C# to call the function
	 ***
	 *******************************************************************************************************
	*/

	//Use loadlibrary and getprocaddress to see if the driver functions exist
	[DllImport("kernel32.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int LoadLibrary(string lpLibFileName);

	[DllImport("kernel32.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int GetProcAddress(int hModule, string lpProcName);

	[DllImport("kernel32.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int CallWindowProc(int lpPrevWndFunc, int hWnd, int Msg, int wParam, int lParam);

	[DllImport("kernel32.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern bool GetModuleHandleExA(int dwFlags, string ModuleName, IntPtr phModule);

	[DllImport("kernel32.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern bool FreeLibrary(int hLibModule);

	[DllImport("kernel32.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int CopyMemory(object lpDest, object lpSource, int cBytes);

	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int ShowTerminalSimple(int ParentHwnd);	// Show driver debug form 
	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int HideTerminalSimple();	// Hide driver debug form 
	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int OpenPort(int ComPortNumber, int ComPortBaudRate);		//Open Port
	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int IsPortOpen();		//Is Port Open
	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int ClosePort();		//Close Port
	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int DriverSendToPort(string CommandOut);		//Send Commands to Vxm
	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver

	private static extern string ReadFromPort();		//Read replies from Vxm
	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver

	private static extern int CountCharsAtPort();		//Count number of chars at port waiting to be read
	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int SearchForChars(string CharsToFind);		//Search for chars or string at the port
	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int ClearPort();		//Clear the Port
	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int RemoveFromPort(string StringToRemove);	//Remove certain chars from port
	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern string GetMotorPosition(int MotorNumber);		//Get motor position
	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int WaitForChar(string CharToWaitFor, int TimeOutTime);		//Wait for char
	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern int WaitForCharWithMotorPosition(string CharToWaitFor, int MotorNumber, int ReportToWindowHwnd, int TimeOutTime);		//Wait for char, and report motor position while waiting
	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern void ResetDriverFunctions();		//Reset Driver Functions

	[DllImport("VxmDriver.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]		//Import the driver
	private static extern long GetThreadCountAmt();		//Reset Driver Functions
	
	//Handles for driver functions
	static private int hLib;                        //Driver
	static private int hShowTerminal;               //ShowTerminal
	static private int hHideTerminal;               //HideTerminal
	static private int hPortOpen;                   //Port Open
	static private int hPortClose;                  //Port Close
	static private int hIsPortOpen;                 //Is Port Open
	static private int hSendToPort;                 //Send to Port
	static private int hReadFromPort;               //Read from Port
	static private int hCountChars;                 //Count Chars at Port
	static private int hSearchForChars;             //Search for Chars
	static private int hClearPort;                  //Clear Port
	static private int hRemoveFromPort;             //Remove Chars from Port
	static private int hGetMotorPosition;           //Get Motor Position
	static private int hWaitForChar;                //Wait for Char
	static private int hWaitForCharMotorPosition;   //Wait for Char and Get Motor Position while waiting
	static private int hResetDriverFunctions;       //Reset Driver Functions for recovery

	/*** Note to self: All manipulations must come before the return command or they will not be run ***/

	//Load the Driver
	public int LoadDriver(string PathToDll)
	{
		hLib = LoadLibrary(PathToDll);
		switch(hLib)
		{
			case 0:
			{
				return 0;	//Call failed
			}
			default:	//default = case else statement from vb
			{
				/**** C# cannot use the handle passed to function to call it so these values
				 **** are to be used just to verify that the memory location exists
				*/

				hShowTerminal = GetProcAddress(hLib, "ShowTerminalSimple");			//Show Terminal
				hHideTerminal = GetProcAddress(hLib, "HideTerminalSimple");			//Hide Terminal
				hPortOpen = GetProcAddress(hLib, "OpenPort");			//Port Open
				hPortClose = GetProcAddress(hLib, "ClosePort");			//Port Close
				hIsPortOpen = GetProcAddress(hLib, "IsPortOpen");			//Is Port Open?
				hSendToPort = GetProcAddress(hLib, "DriverSendToPort");			//Send Commands
				hReadFromPort = GetProcAddress(hLib, "ReadFromPort");			//Read replies
				hCountChars = GetProcAddress(hLib, "CountCharsAtPort");			//Count chars
				hSearchForChars = GetProcAddress(hLib, "SearchForChars");			//Search for chars
				hClearPort = GetProcAddress(hLib, "ClearPort");			//Clear Port
				hRemoveFromPort = GetProcAddress(hLib, "RemoveFromPort");			//Remove from port
				hGetMotorPosition = GetProcAddress(hLib, "GetMotorPosition");			//Get Motor Position
				hWaitForChar = GetProcAddress(hLib, "WaitForChar");			//Wait for char
				hWaitForCharMotorPosition = GetProcAddress(hLib, "WaitForCharWithMotorPosition");	//Wait for char and report position back
				hResetDriverFunctions = GetProcAddress(hLib, "ResetDriverFunctions");			//Reset Driver Functions				
//				FreeLibrary(hLib);		//Release the driver so next calls dont increment reference thread number
				return hLib;	//Return the handle to the driver
			}
		}
	}

	//Release the Driver
	public int ReleaseDriver()
	{
//		MessageBox.Show(System.Convert.ToString(GetThreadCountAmt()));
		if (hPortClose != 0)	//If function exists
		{
			ClosePort();	//Close the port
		}
		if (hHideTerminal != 0)		//If function exists
		{
			HideTerminalSimple();		//Hide the terminal
		}
		if (hLib != 0)		//If function exists
		{
			long NumThreads;
			NumThreads= GetThreadCountAmt();
			for (int i = 1; i <= NumThreads; i++)
			FreeLibrary(hLib);		//Release the driver //Still unknown why if called both in closing and closed routines, fixes crash error so call it twice
		}
		return 1;
	}

	//Show - Hide Debug Terminal
	public int DriverTerminalShowState(int StateToShow, int ParentHwnd)
	{
		if (StateToShow == 0)		//If told to hide
		{
			if (hHideTerminal != 0)		//If function exists
			{
				return HideTerminalSimple();		//Hide Terminal
			}
			else		//Failed function call
			{
				return 0;
			}
		}
		else if (StateToShow == 1)		//If told to show
		{
			if (hShowTerminal != 0)		//If function exists
			{
				return ShowTerminalSimple(ParentHwnd);		//Show Terminal
			}
			else		//Failed function call
			{
				return 0;
			}
		}
		else		//Failed function call
		{
			return 0;
		}
	}

	//Open the port
	public int PortOpen(int PortNumber, int BaudRate)
	{
		if (hPortOpen !=0)		//If function exists
		{
			if (BaudRate == 9600)		//If Valid Baudrate
			{
				if (PortNumber < 1)		//Invalid port
				{
					return 0;
				}
				else if (PortNumber > 255)	//Invalid port
				{
					return 0;
				}
				else	//Valid Port and Baudrate
				{
					return OpenPort(PortNumber, BaudRate);	//Call the function
				}
			}
			else if (BaudRate == 19200)		//If function exists
			{
				if (PortNumber < 1)		//Invalid port
				{
					return 0;
				}
				else if (PortNumber > 255)		//Invalid port
				{
					return 0;
				}
				else		//Valid Port and Baudrate
				{
					return OpenPort(PortNumber, BaudRate);		//Call the function
				}
			}
			else if (BaudRate == 38400)		//If function exists
			{
				if (PortNumber < 1)		//Invalid port
				{
					return 0;
				}
				else if (PortNumber > 255)		//Invalid port
				{
					return 0;
				}
				else		//Valid Port and Baudrate
				{
					return OpenPort(PortNumber, BaudRate);		//Call the function
				}
			}
			else	//Invalid BaudRate
			{
				return 0;
			}
		}
		else		//Function does not exist
		{
			return 0;
		}
	}

	//Close the port
	public int PortClose()
	{
		if (hPortClose != 0)		//If function exists
		{
			return ClosePort();
		}
		else		//If function doesnt exist
		{
			return 0;
		}
	}
	
	//Is the port open?
	public int PortIsOpen()
	{
		if (hIsPortOpen !=0)	//If function exists
		{
			return IsPortOpen();
		}
		else
		{
			return 0;		//If function doesnt exist
		}
	}

	//Send commands to port
	public int PortSendCommands(string CommandToSend)
	{
		if (hSendToPort != 0)	//If function exists
		{
			return DriverSendToPort(CommandToSend);		//Send commands
		}
		else
		{
			return 0;		//If function doesnt exist
		}
	}

	//Read replies from the port
	public string PortReadReply()
	{
		if (hReadFromPort != 0)	//If function exists
		{
			return ReadFromPort();		//Read replies
		}
		else
		{
			return "";		//If function doesnt exist
		}
	}

	//Count Port Chars
	public int PortCountChars()
	{
		if (hCountChars != 0)	//If function exists
		{
			return CountCharsAtPort();		//Count chars
		}
		else
		{
			return 0;		//If function doesnt exist
		}
	}

	//Search for chars
	public int PortSearchForChars(string CharsToFind)
	{
		if (hSearchForChars != 0)	//If function exists
		{
			return SearchForChars(CharsToFind);		//Search for chars
		}
		else
		{
			return 0;		//If function doesnt exist
		}
	}

	//Clear the port
	public int PortClear()
	{
		if (hClearPort != 0)	//If function exists
		{
			return ClearPort();		//Clear the port
		}
		else
		{
			return 0;		//If function doesnt exist
		}
	}

	//Remove chars
	public int PortRemoveChars(string StringToRemove)
	{
		if (hRemoveFromPort != 0)	//If function exists
		{
			return RemoveFromPort(StringToRemove);		//Remove the characters
		}
		else
		{
			return 0;		//If function doesnt exist
		}
	}

	//Motor Position
	public string MotorPosition(int MotorNumber)
	{
		if (hGetMotorPosition != 0)	//If function exists
		{
			return GetMotorPosition(MotorNumber);		//Get the position
		}
		else
		{
			return "";		//If function doesnt exist
		}
	}

	//Wait for char
	public int PortWaitForChar(string CharToWaitFor, int TimeOutTime)
	{
		if (hWaitForChar != 0)	//If function exists
		{
			if (TimeOutTime != 0)		//If wait x milliseconds
			{
				return WaitForChar(CharToWaitFor, TimeOutTime);		//Wait till char or timeout happens
			}
			else
			{
				return WaitForChar(CharToWaitFor, 0);		//Wait for char or loss of communication
			}
		}
		else
		{
			return 0;		//If function doesnt exist
		}
	}

	//Wait for char and return motor position
	public int PortWaitForCharWithMotorPosition(string CharToWaitFor, int MotorNumber, int ReportToWindowHwnd, int TimeOutTime)
	{
		if (hWaitForCharMotorPosition != 0)	//If function exists
		{
			if (TimeOutTime != 0)		//If wait x milliseconds
			{
				return WaitForCharWithMotorPosition(CharToWaitFor, MotorNumber, ReportToWindowHwnd, TimeOutTime);		//Wait till char or timeout happens
			}
			else
			{
				return WaitForCharWithMotorPosition(CharToWaitFor, MotorNumber, ReportToWindowHwnd, 0);		//Wait till char or loss of communication
			}
		}
		else
		{
			return 0;		//If function doesnt exist
		}
	}

	//Reset Driver functions
	public void DriverResetFunctions()
	{
		if (hResetDriverFunctions != 0)	//If function exists
		{
			ResetDriverFunctions();		//Reset the driver functions
		}
		else
		{
			//If function doesnt exist
		}
	}


}
