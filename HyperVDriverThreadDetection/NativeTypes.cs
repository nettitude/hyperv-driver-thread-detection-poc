using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HyperVDriverThreadDetection
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        [MarshalAs(UnmanagedType.LPWStr)] public string Buffer;

        public UNICODE_STRING(string str)
        {
            Buffer = str;
            Length = (ushort)Encoding.Unicode.GetByteCount(str);
            MaximumLength = Length;
        }
    }

    // ref: https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ex/sysinfo/thread.htm
    [StructLayout(LayoutKind.Sequential, Size = 0x50)]
    struct SYSTEM_THREAD_INFORMATION
    {
        public Int64 KernelTime;
        public Int64 UserTime;
        public Int64 CreateTime;
        public UInt32 WaitTime;
        public IntPtr StartAddress;
        public IntPtr ClientIdUniqueProcess;
        public IntPtr ClientIdUniqueThread;
        public Int32 Priority;
        public Int32 BasePriority;
        public UInt32 ContextSwitches;
        public UInt32 ThreadState;
        public UInt32 WaitReason;
    }

    // note: excluding most of the fields here since they aren't used.
    // ref: https://www.geoffchappell.com/studies/windows/km/ntoskrnl/api/ex/sysinfo/process.htm
    [StructLayout(LayoutKind.Explicit, Size = 0x100)]
    struct SYSTEM_PROCESS_INFORMATION
    {
        [FieldOffset(0x00)]
        public UInt32 NextEntryOffset;
        [FieldOffset(0x04)]
        public UInt32 NumberOfThreads;
        [FieldOffset(0x38)]
        public UNICODE_STRING ImageName;
        [FieldOffset(0x50)]
        public IntPtr UniqueProcessId;
        [FieldOffset(0x60)]
        public UInt32 HandleCount;
        [FieldOffset(0x64)]
        public UInt32 SessionId;
        [FieldOffset(0xF8)]
        public UInt64 OtherTransferCount;
    }


    enum LOGICAL_PROCESSOR_RELATIONSHIP
    {
        RelationProcessorCore,
        RelationNumaNode,
        RelationCache,
        RelationProcessorPackage,
        RelationGroup,
        RelationAll = 0xffff
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
    {
        public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
        public uint Size;
        public PROCESSOR_RELATIONSHIP Processor;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESSOR_RELATIONSHIP
    {
        public byte Flags;
        private byte EfficiencyClass;
        // 20 reserved bytes
        private byte Reserved00;
        private byte Reserved01;
        private byte Reserved02;
        private byte Reserved03;
        private byte Reserved04;
        private byte Reserved05;
        private byte Reserved06;
        private byte Reserved07;
        private byte Reserved08;
        private byte Reserved09;
        private byte Reserved10;
        private byte Reserved11;
        private byte Reserved12;
        private byte Reserved13;
        private byte Reserved14;
        private byte Reserved15;
        private byte Reserved16;
        private byte Reserved17;
        private byte Reserved18;
        private byte Reserved19;
        public ushort GroupCount;
        public IntPtr GroupInfo;
    }

#if DEBUG
    [StructLayout(LayoutKind.Sequential, Size = 0x50, CharSet = CharSet.Ansi)]
    struct RTL_PROCESS_MODULE_INFORMATION
    {
        public IntPtr Section;
        public IntPtr MappedBase;
        public IntPtr ImageBase;
        public UInt32 ImageSize;
        public UInt32 Flags;
        public UInt16 LoadOrderIndex;
        public UInt16 InitOrderIndex;
        public UInt16 LoadCount;
        public UInt16 OffsetToFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x100)]
        public string FullPathName;
    }
#endif
}
