using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB.B2
{
    internal class RawScreenshotData : FileData
    {
        /// <summary>
        /// Provides direct, low-level access to a SCDHDAT file.
        /// </summary>

        protected override uint headerLength { get { return FileFactory.ScreenshotDataHeader; } }
        protected override uint decompressedBodyLength { get { return (uint)_body.Length; } }

        internal RawScreenshotData(Memory<byte> header, Memory<byte> body)
            : base(header, body)
        {
        }
    }
}
