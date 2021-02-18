using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperVDriverThreadDetection
{
    static class NativeConsts
    {
        public const uint STATUS_SUCCESS = 0;
        public const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
        public const uint ERROR_INSUFFICIENT_BUFFER = 0x0000007A;
    }
}
