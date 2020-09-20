using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TallariumChallenges2020_OLD
{
    class Program
    {

        /*private static bool OLDSetProxyAutoDetect(bool bAutoDetect)
        {

            //create options pointer
            IntPtr optionsPtr = CreateProxyTypeOptionPtr(null);

            //create options list pointer
            IntPtr ipcoListPtr = CreateIPCOListPtr(optionsPtr);


            // and finally, call the API method!
            int returnvalue = InternetSetOption(
                IntPtr.Zero,
                InternetOption.INTERNET_OPTION_PER_CONNECTION_OPTION,
                ipcoListPtr,
                Marshal.SizeOf(typeof(InternetPerConnOptionList))) ? -1 : 0;

            if (returnvalue == 0)
            {  // get the error codes, they might be helpful
                returnvalue = Marshal.GetLastWin32Error();
            }

            // FREE the data
            FreeComMemory(optionsPtr, ipcoListPtr);
            //Marshal.FreeCoTaskMem(optionsPtr);
            //Marshal.FreeCoTaskMem(ipcoListPtr);

            if (returnvalue > 0)
            {  // throw the error codes, they might be helpful (Most likely not!)
                throw new Win32Exception(returnvalue);
            }

            //Refresh settings so new settings take effect immediately without having to restart programs
            InternetSetOption(IntPtr.Zero, InternetOption.INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, InternetOption.INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

            return (returnvalue < 0);
        }
        
         
         
         
         
         
         private static string OLDGetProxySetting()
        {
            //create options pointer
            IntPtr optionsPtr = CreateProxyTypeOptionPtr(null);

            //create options list pointer
            IntPtr ipcoListPtr = CreateIPCOListPtr(optionsPtr);

            //InternetPerConnOptionList ipcoList1 = Marshal.PtrToStructure<InternetPerConnOptionList>(ipcoListPtr);

            InternetConnectionOption options1 = Marshal.PtrToStructure<InternetConnectionOption>(optionsPtr);


            bool bQuerySuccessful = false; 

            // Allocate unmanaged memory for int 
            IntPtr iSizePtr1 = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(Int32)));
            IntPtr iSizePtr2 = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(Int32)));
            IntPtr iSizePtr3 = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(Int32)));

            //IntPtr ipcoListPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(InternetPerConnOptionList)));

            int size1 = Marshal.SizeOf(typeof(InternetPerConnOptionList));


            Marshal.WriteInt32(iSizePtr1, 0);

            //Debug.WriteLine("iSize before1: {0}b, sizeof internetConnectionList {1}b", Marshal.ReadInt32(iSizePtr1), Marshal.SizeOf(typeof(InternetPerConnOptionList)));
            

            /*bQuerySuccessful = InternetQueryOption(
                                        IntPtr.Zero,
                                        InternetOption.INTERNET_OPTION_PER_CONNECTION_OPTION,
                                        IntPtr.Zero,
                                        iSizePtr1
                                        );*/
             
            //Debug.WriteLine("iSize after1: {0}b", Marshal.ReadInt32(iSizePtr1)); 


        /*
            try
            {
                Debug.WriteLine("iSize before2: {0}b or {1}", Marshal.ReadInt32(iSizePtr2), iSizePtr2);

                bQuerySuccessful = InternetQueryOption(
                                        IntPtr.Zero,
                                        InternetOption.INTERNET_OPTION_PER_CONNECTION_OPTION,
                                        ipcoListPtr,
                                        iSizePtr2
                                        );

        Debug.WriteLine("iSize after2: {0}b or {1}", Marshal.ReadInt32(iSizePtr2), iSizePtr2); 

                Debug.WriteLine("Queryied Internet Options... Option {0} value {1}", options1.m_Option, options1.m_Value.m_Int);


                if (bQuerySuccessful)
                {
                    InternetPerConnOptionList ipcoList = Marshal.PtrToStructure<InternetPerConnOptionList>(ipcoListPtr);

        InternetConnectionOption options = Marshal.PtrToStructure<InternetConnectionOption>(optionsPtr);


        Console.WriteLine("Query Successful");
                    Console.WriteLine("- List pointer: {0}", ipcoListPtr);
                    Console.WriteLine("- List Pointer Option Count: {0}", ipcoList.dwOptionCount);
                    Console.WriteLine("- List Pointer Option {0}", options.m_Option);
                    Console.WriteLine("- List Pointer Int Value: {0}", options.m_Value.m_Int);
                    Console.WriteLine("- List Pointer String Pointer Value: {0}", options.m_Value.m_StringPtr);
                    Console.WriteLine("- List Pointer Filetime Value toString: {0}", options.m_Value.m_FileTime.ToString());

                }
                else
                {
                    Console.WriteLine("Err querying internet options: {0}", Marshal.GetLastWin32Error());
                }
            }
            catch (Win32Exception e)
{
    Console.WriteLine("Win32Exception while querying internet options: {0}", e.Message);

}
catch (Exception e)
{

    Console.WriteLine("General exception while querying internet options: {0} {1}", e.Source, e.Message);
}
finally
{
    // FREE the reserved unmanaged memory
    FreeComMemory(optionsPtr, ipcoListPtr, iSizePtr1, iSizePtr2, iSizePtr3);
}

return "";
        }

         
         
         
         
          //if bAutoDetect is set to null then a pointer with a blank value is created
        private static IntPtr CreateProxyTypeOptionPtr(Boolean? bAutoDetect)
        {

            // we'll use this array to store our options
            InternetConnectionOption[] options = new InternetConnectionOption[1];


            // our per connection flags get stored here...
            options[0].m_Option = PerConnOption.INTERNET_PER_CONN_FLAGS_UI;
            //options[1].m_Option = PerConnOption.INTERNET_PER_CONN_AUTODISCOVERY_FLAGS;

            if (bAutoDetect != null)
            {
                // enable or disable proxy auto detect?
                if (bAutoDetect == true)
                {
                    options[0].m_Value.m_Int = (int)PerConnFlags.PROXY_TYPE_AUTO_DETECT | (int)PerConnFlags.PROXY_TYPE_DIRECT;
                    //options[1].m_Value.m_Int = (int)AutoDetectFlags.AUTO_PROXY_FLAG_USER_SET;
                }
                else
                {
                    options[0].m_Value.m_Int = (int)PerConnFlags.PROXY_TYPE_DIRECT;
                    //options[1].m_Value.m_Int = (int)AutoDetectFlags.AUTO_PROXY_FLAG_USER_SET;
                }

            }
            else
            {
                options[0].m_Value.m_Int = (int)IntPtr.Zero;
                //options[1].m_Value.m_Int = (int)IntPtr.Zero;
            }


            int optSize = Marshal.SizeOf(typeof(InternetConnectionOption));

            // make a pointer out of all that ...
            IntPtr optionsPtr = Marshal.AllocCoTaskMem(optSize * options.Length);

            // copy the array over into that spot in memory ...
            // transfer managed memory to unmanaged memory
            for (int i = 0; i < options.Length; ++i)
            {
                IntPtr opt = new IntPtr((long)(optionsPtr.ToInt64() + (i * optSize)));
                Marshal.StructureToPtr(options[i], opt, false);
            }

            Debug.WriteLine("Created optionsPtr: {0}", optionsPtr.ToInt64());

            return optionsPtr;
        }

        private static IntPtr CreateIPCOListPtr(IntPtr optionsPtr)
        {

            // use this to store our proxy settings
            InternetPerConnOptionList list = new InternetPerConnOptionList();

            //?
            list.dwSize = Marshal.SizeOf(list);

            Debug.WriteLine("Marshal.SizeOf(list): {0}b", Marshal.SizeOf(list));


            //the proxy settings will be applied to system settings
            list.szConnection = IntPtr.Zero;

            list.dwOptionCount = 1;
            list.dwOptionError = 0;

            // assign options created to option list
            list.options = optionsPtr;

            // and then make a pointer out of the whole list
            IntPtr ipcoListPtr = Marshal.AllocCoTaskMem((int)list.dwSize);
            Marshal.StructureToPtr(list, ipcoListPtr, false);

            Debug.WriteLine("Created ipcoListPtr: {0}", ipcoListPtr.ToInt64());


            return ipcoListPtr;

        }

        /*private static string GetProxySetting2()
        {
            //https://stackoverflow.com/questions/15565997/internetqueryoption-internet-per-conn-option-and-the-internet-per-conn-flags-ui

            InternetPerConnOptionList List;
            // we'll use this array to store our options
            InternetConnectionOption[] options = new InternetConnectionOption[1];

            long nSize = Marshal.SizeOf(typeof(InternetPerConnOptionList));

            options[0].m_Option = PerConnOption.INTERNET_PER_CONN_FLAGS;

            List.dwSize = (int)nSize;
            List.szConnection = IntPtr.Zero;
            List.dwOptionCount = 1;
            List.dwOptionError = 0;

            options[0].m_Value.m_Int = 0;

            //List.options = options;

            //InternetQueryOption(NULL, INTERNET_OPTION_PER_CONNECTION_OPTION, &List, &nSize);

            return "";
        }*/

         
          

    }
}