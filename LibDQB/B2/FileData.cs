using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDQB.B2
{
    public abstract class FileData
    {
        protected abstract uint headerLength { get; }
        protected abstract uint decompressedBodyLength { get; }

        protected readonly Memory<byte> _header;
        protected readonly Memory<byte> _body;
        protected Span<byte> Header => _header.Span;
        protected Span<byte> Body => _body.Span;

        protected FileData(Memory<byte> header, Memory<byte> body)
        {
            this._header = header;
            this._body = body;
        }
    }
}
