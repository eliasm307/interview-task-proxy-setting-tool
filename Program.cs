using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

//References
//https://stackoverflow.com/questions/43817351/c-sharp-proxy-authentication-using-wininet
//https://github.com/chentiangemalc/ProxyUtils


/*
 * by Elias Mangoro 
 * 20/09/2020
 * 
 * Based on information from:
 * 
 *  - Mircosoft documentation on WinInet.H
 *  - c# proxy authentication using wininet StackOverflow post: https://stackoverflow.com/questions/43817351/c-sharp-proxy-authentication-using-wininet
 *  - Malcolm McCaffery ProxyUtils Github Repo: https://github.com/chentiangemalc/ProxyUtils
 * 
 */

namespace TallariumChallenges2020
{
    class Program
    {

        private const string sCONSOLE_SEPARATOR = "----------------------------------------------------------------";
        private static int m_iLastProxySetting = (int)ProxySettingState.UNDEFINED;   //Last successfully read proxy setting based on "PerConFlags" enum values ie -1 = undefined, 1 = PROXY_TYPE_DIRECT (Auto detect off), 9 = PROXY_TYPE_DIRECT + PROXY_TYPE_AUTO_DETECT (Auto detect on)
        private static IntPtr sizePtr, optionsPtr, ipcoListPtr;
        private static int optSize, ipcoListSize;

        private enum ProxySettingState : int
        {
            UNDEFINED = -1,
            OFF = 1,
            ON = 9
        }
         
        // Relevant option flags for InternetSetOption to set appropriate settings and refresh OS after
        [Flags]
        private enum InternetOption : uint
        {
            INTERNET_OPTION_REFRESH = 0x00000025,               //Force OS to refresh internet settings
            INTERNET_OPTION_SETTINGS_CHANGED = 0x00000027,      //Notify OS that internet settings were changed ie dirty
            INTERNET_OPTION_PER_CONNECTION_OPTION = 0x0000004B  //Option to set internet settings based on a INTERNET_PER_CONN_OPTION_LIST structure 
        }
         
        // Relevant Option used in INTERNET_PER_CONN_OPTON struct 
        [Flags]
        private enum PerConnOption : int
        {
            INTERNET_PER_CONN_FLAGS = 1,            // Sets or retrieves the connection type. The Value member will contain one or more of the values from PerConnFlags  
            INTERNET_PER_CONN_AUTODISCOVERY_FLAGS = 5, // Sets or AutoDiscovery Flags
            INTERNET_PER_CONN_FLAGS_UI = 10
        }

        // Relevant values for option INTERNET_PER_CONN_FLAGS for INTERNET_PER_CONN_OPTIONA structure to set connection type settings
        [Flags]
        private enum PerConnFlags : int
        {
            PROXY_TYPE_DIRECT = 0x00000001,         // direct to net 
            PROXY_TYPE_AUTO_DETECT = 0x00000008     // use autoproxy detection
        }


        // Relevant values for option INTERNET_PER_CONN_AUTODISCOVERY_FLAGS for INTERNET_PER_CONN_OPTIONA structure to set automatic discovery settings, does this need to be used if PROXY_TYPE_AUTO_DETECT is set
        [Flags]
        private enum AutoDetectFlags : int
        {
            AUTO_PROXY_FLAG_USER_SET = 0x00000001,
            AUTO_PROXY_FLAG_ALWAYS_DETECT = 0x00000002
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct InternetPerConnOptionList : IDisposable
        {
            public int dwSize;              // size of INTERNET_PER_CONN_OPTION_LIST struct
            public IntPtr szConnection;     // connection name to set/query options
            public int dwOptionCount;       // number of options to set/query
            public int dwOptionError;       // on error, which option failed
            public IntPtr options;

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            // The bulk of the clean-up code is implemented in Dispose(bool)
            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (szConnection != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(szConnection);
                        szConnection = IntPtr.Zero;
                    }

                    if (options != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(options);
                        szConnection = IntPtr.Zero;

                    }
                }
            } 
        };


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct InternetConnectionOption
        {
            static readonly int Size = Marshal.SizeOf(typeof(InternetConnectionOption));    //size allocated in memory
            public PerConnOption m_Option;  //flag of the value to be set
            public InternetConnectionOptionValue m_Value;  //value to be set to

            // Nested Types for option values
            [StructLayout(LayoutKind.Explicit)]
            public struct InternetConnectionOptionValue : IDisposable
            {
                // Fields
                [FieldOffset(0)]
                public System.Runtime.InteropServices.ComTypes.FILETIME m_FileTime;
                [FieldOffset(0)]
                public int m_Int;
                [FieldOffset(0)]
                public IntPtr m_StringPtr;

                public void Dispose()
                {
                    Dispose(true);
                    GC.SuppressFinalize(this);

                }

                // The bulk of the clean-up code is implemented in Dispose(bool)
                private void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        if (m_StringPtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(m_StringPtr);
                            m_StringPtr = IntPtr.Zero;
                        }
                    }
                }
            }
        }
          
        //Import InternetSetOption method
        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InternetSetOption(
            IntPtr hInternet,
            InternetOption dwOption,
            IntPtr lpBuffer,
            int lpdwBufferLength);
         
        //Import InternetQueryOption method
        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InternetQueryOption(
            IntPtr hInternet,
            InternetOption dwOption,
            IntPtr lpBuffer,
            IntPtr lpdwBufferLength);
        
        public static bool SetProxySetting(bool EnableAutoDetect)
        {
            Debug.WriteLine(sCONSOLE_SEPARATOR);
            Debug.WriteLine("SetProxySetting method start");

            // use this to store proxy settings
            InternetPerConnOptionList list = new InternetPerConnOptionList(); 

            // Minimum of 1 setting needs to be changed
            int optionCount = 1;
            int returnvalue; 

            // To enable auto-detect proxy setting, the INTERNET_PER_CONN_AUTODISCOVERY_FLAGS setting also need to be set
            if (EnableAutoDetect) optionCount++; 

            // we'll use this array to store our options
            InternetConnectionOption[] options = new InternetConnectionOption[optionCount];
              
            // our per connection flags get stored here...
            options[0].m_Option = PerConnOption.INTERNET_PER_CONN_FLAGS; //opted to use INTERNET_PER_CONN_FLAGS over INTERNET_PER_CONN_FLAGS_UI for greater compatibility

            //base flag required
            options[0].m_Value.m_Int = (int)PerConnFlags.PROXY_TYPE_DIRECT;
             
            if (EnableAutoDetect)
            {
                //addd auto-detect to connection type flag
                options[0].m_Value.m_Int = (int)PerConnFlags.PROXY_TYPE_AUTO_DETECT | (int)options[0].m_Value.m_Int;

                //additional setting for auto-detect, AUTO_PROXY_FLAG_ALWAYS_DETECT is for "Always automatically detect settings"
                options[1].m_Option = PerConnOption.INTERNET_PER_CONN_AUTODISCOVERY_FLAGS;
                options[1].m_Value.m_Int = (int)AutoDetectFlags.AUTO_PROXY_FLAG_ALWAYS_DETECT; 
            }

            // Get memory size of option list struct
            list.dwSize = Marshal.SizeOf(list);

            // the proxy will apply to default system settings
            list.szConnection = IntPtr.Zero;

            // set other details of option list struct
            list.dwOptionCount = options.Length;
            list.dwOptionError = 0; 

            // copy the array of internet options into reserved spot in unmanaged memory 
            for (int i = 0; i < options.Length; ++i)
            {
                IntPtr opt = new IntPtr((long)(optionsPtr.ToInt64() + (i * optSize)));
                Marshal.StructureToPtr(options[i], opt, false);
            }

            // Give pointer to internet options in unmanaged memory to option list
            list.options = optionsPtr;

            // Copy list to allocated unmanaged memory 
            Marshal.StructureToPtr(list, ipcoListPtr, false);

            // call the API method to apply settings and get result status as integer
            returnvalue = InternetSetOption(
                                    IntPtr.Zero,
                                    InternetOption.INTERNET_OPTION_PER_CONNECTION_OPTION,
                                    ipcoListPtr, 
                                    list.dwSize
                                    ) ? -1 : 0;

            //if API call was unsuccessful
            if (returnvalue == 0)
            {  // get the error codes, they might be helpful
                returnvalue = Marshal.GetLastWin32Error();
            }
              
            //If there were errors then show them on console
            if (returnvalue > 0)
            {   // throw the error codes, they might be helpful (Most likely not!)
                //throw new Win32Exception(returnvalue);
                Console.WriteLine("{0, 10} - Win32 Error while setting proxy options: {1}","ERROR", returnvalue);
            }
              
            //Notify OS of settings changes and force refresh so new settings take effect immediately
            InternetSetOption(IntPtr.Zero, InternetOption.INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, InternetOption.INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
             
            return (returnvalue < 0);
        }

        private static int GetProxySetting()
        {
            Debug.WriteLine(sCONSOLE_SEPARATOR);
            Debug.WriteLine("GetProxySetting method start");

            //default result is undefined
            int iResult = (int)ProxySettingState.UNDEFINED;

            //return value variable
            int returnvalue;

            // use this to store our proxy settings
            InternetPerConnOptionList list = new InternetPerConnOptionList();

            // we'll use this array to store our options, size is always 2 to accomodate required settings
            InternetConnectionOption[] options = new InternetConnectionOption[2];

            // our per connection flags get stored here...
            options[0].m_Option = PerConnOption.INTERNET_PER_CONN_FLAGS;  //opted to use INTERNET_PER_CONN_FLAGS over INTERNET_PER_CONN_FLAGS_UI for greater compatibility
            options[1].m_Option = PerConnOption.INTERNET_PER_CONN_AUTODISCOVERY_FLAGS; //Seems to require to be included to work in the case where auto detect is being used

            // Get memory size of list
            list.dwSize = Marshal.SizeOf(list);

            // read proxy settings for default global settings
            list.szConnection = IntPtr.Zero;

            // set other details of option list struct
            list.dwOptionCount = options.Length;
            list.dwOptionError = 0;

            // copy the array over into the reserved spot in unmanaged memory
            for (int i = 0; i < options.Length; ++i)
            {
                IntPtr opt = new IntPtr((long)(optionsPtr.ToInt64() + (i * optSize)));
                Marshal.StructureToPtr(options[i], opt, false);
            }

            // Give pointer to internet options in unmanaged memory to option list
            list.options = optionsPtr;

            // Copy list to Allocated unmanaged memory for option list struct and get pointer to memory location 
            Marshal.StructureToPtr(list, ipcoListPtr, false);

            //For monitoring and debugging
            Debug.WriteLine("iSize before1: {0}kb", Marshal.ReadInt32(sizePtr) / 1024);

            // call the API method to query settings on a null buffer pointer to get the required memory size for the result
            InternetQueryOption(
                        IntPtr.Zero,
                        InternetOption.INTERNET_OPTION_PER_CONNECTION_OPTION,
                        IntPtr.Zero,
                        sizePtr
                        );

            //For monitoring and debugging
            Debug.WriteLine("iSize after1 : {0}kb", Marshal.ReadInt32(sizePtr) / 1024);

            //For monitoring and debugging
            Debug.WriteLine("iSize before2: {0}kb", Marshal.ReadInt32(sizePtr) / 1024);

            // call the API method to query settings using the configured buffer with an appropriate size to get the the actual settings, and get result status as integer
            returnvalue = InternetQueryOption(
                                    IntPtr.Zero,
                                    InternetOption.INTERNET_OPTION_PER_CONNECTION_OPTION,
                                    ipcoListPtr,
                                    sizePtr
                                    ) ? -1 : 0;

            //For monitoring and debugging
            Debug.WriteLine("iSize after2 : {0}kb", Marshal.ReadInt32(sizePtr) / 1024);

            //if API call was unsuccessful
            if (returnvalue == 0)
            {  // get the error codes, they might be helpful
                returnvalue = Marshal.GetLastWin32Error();
            }
            else
            {
                Debug.WriteLine("Query Successful");

                //Copy structures in unmanaged memory to existing variables in managed memory
                list = Marshal.PtrToStructure<InternetPerConnOptionList>(ipcoListPtr);
                options[0] = Marshal.PtrToStructure<InternetConnectionOption>(optionsPtr);

                //For monitoring and debugging
                Debug.WriteLine("- List pointer: {0}", ipcoListPtr);
                Debug.WriteLine("- List Option Count: {0}", list.dwOptionCount);
                Debug.WriteLine("- List size: {0}", list.dwSize);
                Debug.WriteLine("- List Pointer Option {0}", options[0].m_Option);
                Debug.WriteLine("- List Pointer Int Value: {0}", options[0].m_Value.m_Int);
                Debug.WriteLine("- List Pointer Value Flags: {0:G}", (PerConnFlags)options[0].m_Value.m_Int);

                //update last read proxy setting 
                iResult = options[0].m_Value.m_Int;

            }

            if (returnvalue > 0)
            {  // show the error codes, they might be helpful
                //throw new Win32Exception(returnvalue);
                Console.WriteLine("Win32 Error while reading proxy options: {0}", returnvalue);
            }

            return iResult;
        }

        private static void AllocateMemory()
        { 
            Debug.WriteLine(sCONSOLE_SEPARATOR);
            Debug.WriteLine("AllocateMemory method start");

            // memory size for single internet option struct
            optSize = Marshal.SizeOf(typeof(InternetConnectionOption));

            // memory size for single internet option list struct
            ipcoListSize = Marshal.SizeOf(typeof(InternetPerConnOptionList));
             
            sizePtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(Int32)));

            // Allocate memory for required internet option structs (2 at most for this application) and get pointer to memory location
            optionsPtr = Marshal.AllocCoTaskMem(optSize * 2);

            // Allocate memory for option list struct and get pointer to memory location
            ipcoListPtr = Marshal.AllocCoTaskMem(ipcoListSize);

        }

        private static void FreeMemory()
        {
            Debug.WriteLine(sCONSOLE_SEPARATOR);
            Debug.WriteLine("FreeMemory method start");
            FreePointerMemory(sizePtr, optionsPtr, ipcoListPtr);

        }
         
        private static void FreePointerMemory(params IntPtr[] paramArr)
        {
            Debug.WriteLine(sCONSOLE_SEPARATOR);
            Debug.WriteLine("FreePointerMemory method start");

            Debug.WriteLine("Freeing Memory from {0} pointers...", paramArr.Length);

            for (int i = 0; i < paramArr.Length; i++)
            {
                //Release memory
                Marshal.FreeCoTaskMem(paramArr[i]);
                //Marshal.FreeHGlobal(paramArr[i]);

                //Set pointer to 'null'
                paramArr[i] = IntPtr.Zero;

            }

        }

        //Entry point for console app
        static void Main(string[] args)
        {
            string sInput;
            string sMessage;

            //allocate required memory that will be reused
            AllocateMemory();

            //Read initial proxy setting 
            m_iLastProxySetting = GetProxySetting();

            Console.Write("'Automatically detect proxy settings' Tool");
            Console.Write("By Elias Mangoro");

            do
            {
                //console Header with seperator and time stamp
                Console.WriteLine(sCONSOLE_SEPARATOR);
                Console.WriteLine(DateTime.Now);

                Console.WriteLine("{0, 10} - Current Proxy Setting for 'Automatically detect proxy settings': {1:G}", "STATUS", (ProxySettingState)m_iLastProxySetting);

                Console.WriteLine("\nPlease enter requested setting for 'Automatically detect proxy settings' ('on' or 'off'),\nor enter 'exit' to end program");

                Console.Write("{0, 10} - Enter 'on', 'off', or 'exit': ", "INPUT");
                sInput = Console.ReadLine();

                //make input case insensitive and ignore white spaces
                sInput = sInput.ToUpper().Trim();

                sMessage = "";

                if (sInput == "ON")
                {
                    if ((int)m_iLastProxySetting == (int)PerConnFlags.PROXY_TYPE_DIRECT + (int)PerConnFlags.PROXY_TYPE_AUTO_DETECT)
                    {
                        sMessage = "'Automatically detect proxy settings' was already turned ON, no changes made";
                    }
                    else if (SetProxySetting(true))
                    {
                        sMessage = "'Automatically detect proxy settings' turned ON successfully";

                        //refresh last read proxy setting
                        m_iLastProxySetting = GetProxySetting();
                    }
                }
                else if (sInput == "OFF")
                {
                    if ((int)m_iLastProxySetting == (int)PerConnFlags.PROXY_TYPE_DIRECT)
                    {
                        sMessage = "'Automatically detect proxy settings' was already turned OFF, no changes made";
                    }
                    else if (SetProxySetting(false))
                    {
                        sMessage = "'Automatically detect proxy settings' turned OFF successfully";

                        //refresh last read proxy setting
                        m_iLastProxySetting = GetProxySetting();
                    }
                }
                else if (sInput == "EXIT")
                {
                    sMessage = "Exiting program...";
                }
                else if (sInput != "")
                {
                    sMessage = "Invalid input, please try again";
                }

                Console.WriteLine("{0, 10} - {1}", "RESULT", sMessage);

            } while (sInput != "EXIT");

            //free allocated memory
            FreeMemory();

        }

    }

}
   