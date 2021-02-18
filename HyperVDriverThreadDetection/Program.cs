using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HyperVDriverThreadDetection
{
    class Program
    {
        /// <summary>
        /// Gets the number of logical processors on the system.
        /// </summary>
        /// <returns>The number of logical processors on the system.</returns>
        static int GetLogicalProcessorCount()
        {
            uint bufferLength = 0;
            IntPtr buffer = IntPtr.Zero;
            // fuse pattern to prevent looping on endless GetLogicalProcessorInformationEx calls if it always returns ERROR_INSUFFICIENT_BUFFER
            const int BLOWN = 100;
            int fuse = 0;
            while (fuse++ != BLOWN)
            {
                // attempt to get processor information
                if (!NativeMethods.GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, buffer, ref bufferLength))
                {
                    int lastError = Marshal.GetLastWin32Error();
                    if (lastError == NativeConsts.ERROR_INSUFFICIENT_BUFFER)
                    {
                        // buffer wasn't big enough. reallocate and try again.
                        if (buffer != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(buffer);
                        }
                        buffer = Marshal.AllocHGlobal((int)bufferLength);
                        continue;
                    }

                    // some other error occurred.
                    Console.WriteLine("[X] GetLogicalProcessorInformationEx call failed with error 0x{0:x}", lastError);
                    return -1;
                }

                try
                {
                    // check that we got a sensible amount of data back
                    if (bufferLength < Marshal.SizeOf<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>())
                    {
                        Console.WriteLine("[X] GetLogicalProcessorInformationEx call returned insufficient data.");
                        return -1;
                    }

                    // call was successful. walk through the data.
                    int logicalProcessorCount = 0;
                    int offset = 0;
                    // loop through all the logical CPU info and figure out how many CPUs we have.
                    while (offset < bufferLength - Marshal.SizeOf<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>())
                    {
                        var procInfo = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(buffer + offset);
                        if (procInfo.Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore)
                        {
                            // Flags is 0 if the processor has one logical core, or non-zero (LTP_PC_SMT) if SMT is enabled (practically this means 2 cores)
                            logicalProcessorCount += procInfo.Processor.Flags == 0 ? 1 : 2;
                        }
                        offset += (int)procInfo.Size;
                    }
                    return logicalProcessorCount;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            // we only reach here if the fuse blows
            Console.WriteLine("[X] GetLogicalProcessorInformationEx did not return correct data after {0} attempts.", fuse);
            return -1;
        }


        static SYSTEM_THREAD_INFORMATION[] GetSystemThreadInformation()
        {
            uint bufferLength = 0;
            IntPtr buffer = IntPtr.Zero;
            // fuse pattern to prevent looping on endless NtQuerySystemInformation calls if it always returns ERROR_INSUFFICIENT_BUFFER
            const int BLOWN = 100;
            int fuse = 0;
            while (fuse++ != BLOWN)
            {
                // attempt to get process/thread information
                uint status = NativeMethods.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessInformation, buffer, bufferLength, out bufferLength);
                if (status == NativeConsts.STATUS_INFO_LENGTH_MISMATCH)
                {
                    // buffer wasn't big enough. reallocate and try again.
                    if (buffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                    buffer = Marshal.AllocHGlobal((int)bufferLength);
                    continue;
                }
                else if (status != NativeConsts.STATUS_SUCCESS)
                {
                    // some other error occurred.
                    Console.WriteLine("[X] NtQuerySystemInformation call failed with status 0x{0:x}", status);
                    return null;
                }

                try
                {
                    // check that we got a sensible amount of data back
                    if (bufferLength < Marshal.SizeOf<SYSTEM_PROCESS_INFORMATION>() + Marshal.SizeOf<SYSTEM_THREAD_INFORMATION>())
                    {
                        Console.WriteLine("[X] NtQuerySystemInformation call returned insufficient data. This occurs when the current process is running at a low integrity level.");
                        return null;
                    }

                    // call was successful. walk through the data.
                    var threads = new List<SYSTEM_THREAD_INFORMATION>();
                    int offset = 0;
                    // loop through all the logical CPU info and figure out how many CPUs we have.
                    while (offset < bufferLength - Marshal.SizeOf<SYSTEM_THREAD_INFORMATION>())
                    {
                        var processInfo = Marshal.PtrToStructure<SYSTEM_PROCESS_INFORMATION>(buffer + offset);
                        int nextProcessOffset = offset + (int)processInfo.NextEntryOffset;
                        offset += Marshal.SizeOf<SYSTEM_PROCESS_INFORMATION>();

                        // enumerate the threads for this process
                        for (int t = 0; t < processInfo.NumberOfThreads; t++)
                        {
                            var threadInfo = Marshal.PtrToStructure<SYSTEM_THREAD_INFORMATION>(buffer + offset);
                            offset += Marshal.SizeOf<SYSTEM_THREAD_INFORMATION>();
                            threads.Add(threadInfo);
                        }

                        if (processInfo.NextEntryOffset == 0)
                        {
                            // this is the end of the data
                            break;
                        }
                        offset = nextProcessOffset;
                    }

                    return threads.ToArray();
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            // we only reach here if the fuse blows
            Console.WriteLine("[X] NtQuerySystemInformation did not return correct data after {0} attempts.", fuse);
            return null;
        }

#if DEBUG
        static RTL_PROCESS_MODULE_INFORMATION[] GetSystemModules()
        {
            uint bufferLength = 0;
            IntPtr buffer = IntPtr.Zero;
            // fuse pattern to prevent looping on endless NtQuerySystemInformation calls if it always returns ERROR_INSUFFICIENT_BUFFER
            const int BLOWN = 100;
            int fuse = 0;
            while (fuse++ != BLOWN)
            {
                // attempt to get process/thread information
                uint status = NativeMethods.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemModuleInformation, buffer, bufferLength, out bufferLength);
                if (status == NativeConsts.STATUS_INFO_LENGTH_MISMATCH)
                {
                    // buffer wasn't big enough. reallocate and try again.
                    if (buffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                    buffer = Marshal.AllocHGlobal((int)bufferLength);
                    continue;
                }
                else if (status != NativeConsts.STATUS_SUCCESS)
                {
                    // some other error occurred.
                    Console.WriteLine("[X] NtQuerySystemInformation call failed with status 0x{0:x}", status);
                    return null;
                }

                try
                {
                    // check that we got a sensible amount of data back
                    if (bufferLength < IntPtr.Size + Marshal.SizeOf<RTL_PROCESS_MODULE_INFORMATION>())
                    {
                        Console.WriteLine("[X] NtQuerySystemInformation call returned insufficient data.");
                        return null;
                    }

                    // call was successful. read the length field that prefixes the array.
                    int moduleCount = Marshal.ReadInt32(buffer);
                    if (moduleCount < 0)
                    {
                        Console.WriteLine("[X] NtQuerySystemInformation call returned an invalid module count.");
                        return null;
                    }

                    // check that the buffer size can actually fit that many modules.
                    long expectedLength = IntPtr.Size + (Marshal.SizeOf<RTL_PROCESS_MODULE_INFORMATION>() * moduleCount);
                    if (expectedLength > bufferLength)
                    {
                        Console.WriteLine("[X] NtQuerySystemInformation call returned insufficient data for the reported number of modules.");
                        return null;
                    }

                    // call was successful. walk through the data.
                    var modules = new List<RTL_PROCESS_MODULE_INFORMATION>();
                    int offset = IntPtr.Size;
                    int index = 0;
                    // loop through all the module info and add it to the threa
                    while ((offset < bufferLength - Marshal.SizeOf<RTL_PROCESS_MODULE_INFORMATION>()) && (index++ < moduleCount))
                    {
                        var moduleInfo = Marshal.PtrToStructure<RTL_PROCESS_MODULE_INFORMATION>(buffer + offset);
                        modules.Add(moduleInfo);
                        offset += Marshal.SizeOf<RTL_PROCESS_MODULE_INFORMATION>();
                    }

                    return modules.ToArray();
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            // we only reach here if the fuse blows
            Console.WriteLine("[X] NtQuerySystemInformation did not return correct data after {0} attempts.", fuse);
            return null;
        }
#endif

        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("HyperV VMBUS Driver Thread VM Detection");
            Console.WriteLine();

#if DEBUG
            // for debug builds, query module information so we can show which drivers we've found threads for.
            Console.WriteLine("[*] [DEBUG] Querying module information...");
            var modules = GetSystemModules();
            if (modules == null)
            {
                Console.WriteLine("[*] [DEBUG] Failed to get module information. Continuing anyway.");
            }
            else
            {
                Console.WriteLine("[*] [DEBUG] {0} modules found.", modules.Length);
            }
#endif

            Console.WriteLine("[*] Querying process information...");
            var threads = GetSystemThreadInformation();
            if ((threads == null) || (threads.Length == 0))
            {
                Console.WriteLine("[X] Failed to get thread information. This usually occurs for low integrity processes.");
                return;
            }
            Console.WriteLine("[*] {0} total threads found.", threads.Length);

            var systemThreads = threads.Where(t => t.ClientIdUniqueProcess == new IntPtr(4));
            Console.WriteLine("[*] {0} system threads found.", systemThreads.Count());

            if (systemThreads.Count() == 0)
            {
                Console.WriteLine("[X] No system threads found. This usually occurs for low integrity processes.");
                return;
            }

            Console.WriteLine("[*] Grouping threads by start address...");
            var threadsByStartAddress = new Dictionary<Int64, List<SYSTEM_THREAD_INFORMATION>>();
            foreach (SYSTEM_THREAD_INFORMATION thread in systemThreads)
            {
                Int64 startAddr = thread.StartAddress.ToInt64();
                if (!threadsByStartAddress.ContainsKey(startAddr))
                {
                    threadsByStartAddress.Add(startAddr, new List<SYSTEM_THREAD_INFORMATION>());
                }
                threadsByStartAddress[startAddr].Add(thread);
            }

            int cpuCount = GetLogicalProcessorCount();
            if (cpuCount < 0)
            {
                Console.WriteLine("[X] Failed to get the number of logical processors. Falling back to .NET method.");
                cpuCount = Environment.ProcessorCount;
            }
            Console.WriteLine("[*] {0} vCPUs detected.", cpuCount);

            if (cpuCount < 3)
            {
                Console.WriteLine("[?] Warning: this system has a small number of logical processors. This check may produce false positives.");
            }

            Console.WriteLine("[*] Looking for candidate thread groups...");
            foreach (var kvp in threadsByStartAddress)
            {
                var startAddr = kvp.Key;
                var threadsAtThisAddr = kvp.Value;
                // to make the scaling slightly more flexible to future changes, this heuristic looks for any thread group that has exactly 2x, 3x, 4x, etc. as many threads as there are CPUs.
                if (threadsAtThisAddr.Count > cpuCount && threadsAtThisAddr.Count % cpuCount == 0)
                {
                    Console.WriteLine("[?] Potential target with {0} threads at address {1:X16}", threadsAtThisAddr.Count, startAddr);
#if DEBUG
                    // in debug builds, show module information when we have it.
                    if (modules != null)
                    {
                        // find a module that corresponds to the thread start address
                        // if not found, return value will be a struct whose ImageBase pointer is zero
                        var module = modules.FirstOrDefault(m => startAddr >= m.ImageBase.ToInt64() &&
                                                startAddr < (m.ImageBase.ToInt64() + m.ImageSize));
                        // did we find one?
                        if (module.ImageBase != IntPtr.Zero)
                        {
                            Int64 offset = startAddr - module.ImageBase.ToInt64();
                            Console.WriteLine("[^] Module for {0:X16} is {1}+0x{2:x}", startAddr, module.FullPathName, offset);
                        }
                    }
#endif

                    Console.WriteLine("[*] Checking nearby threads...");
                    var nearbyThreads = systemThreads.Where(t =>
                        t.StartAddress.ToInt64() < startAddr &&
                        startAddr - t.StartAddress.ToInt64() < 0x2000);
                    Console.WriteLine("[*] {0} nearby threads found.", nearbyThreads.Count());
                    if (nearbyThreads.Count() == 2)
                    {
                        Console.WriteLine("[!] Hyper-V VMBUS driver detected!");
                        return;
                    }
                }
            }

            Console.WriteLine("[*] No Hyper-V detected.");
            Console.ReadLine();
        }
    }
}
