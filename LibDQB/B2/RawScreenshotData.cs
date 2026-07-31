using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB.B2
{
    /// <summary>
    /// Provides direct, low-level access to a SCDHDAT file.
    /// </summary>
    internal sealed class RawScreenshotData
    {
        private readonly Memory<byte> header;
        private readonly Memory<byte> body;

        internal RawScreenshotData(Memory<byte> header, Memory<byte> body)
        {
            this.header = header;
            this.body = body;
        }
    }
}
