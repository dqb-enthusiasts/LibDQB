using LibDQB.B2.Records;
using System;
using System.Collections.Generic;
using System.IO.Compression;


namespace LibDQB.B2
{
    /// <summary>
    /// Handles opening the data files. Should probably handle saving to them too.
    /// </summary>
    public static class FileFactory
    {
        //===================================== Constants =====================================//

        const int CommonDataHeaderLength = 0x2A444; // 172,080 bytes
        const uint CommonDataDecompressedBodyLength = 5627194; // The decompressed body will always have this length?
        readonly static byte[] CommonDataMagicNumber = { 0x61, 0x65, 0x72, 0x43, 0x02 };

        const int StageDataHeaderLength = 0x110; // 272 bytes
        readonly static byte[] StageDataMagicNumber = { 0x61, 0x65, 0x72, 0x43, 0xDD };

        const int ScreenshotDataHeaderLength = 0x40;
        readonly static byte[] ScreenshotDataMagicNumber = { 0x61, 0x65, 0x72, 0x43, 0x10 };

        //===================================== Functions =====================================//
        public static async Task<RawCommonData> LoadCommonDataAsync(FileInfo file)
        {
            var (header, body) = await LoadCompressedFileAsync(file, new FileDataReadOptions(), CommonDataHeaderLength);

            if (!IsHeaderValid(header, CommonDataMagicNumber))
            {
                throw new ArgumentException("Not a valid CMNDAT file (magic number check failed)");
            }
            if (body.Length != CommonDataDecompressedBodyLength)
            {
                throw new ArgumentException("Not a valid CMNDAT file (decompressed length check failed)");
            }

            return new RawCommonData(header, body);
        }

        internal static async Task<RawStageData> LoadStageDataAsync(FileInfo file)
        {
            var (header, body) = await LoadCompressedFileAsync(file, new FileDataReadOptions(), StageDataHeaderLength);

            if (!IsHeaderValid(header, StageDataMagicNumber))
            {
                throw new ArgumentException("Not a valid STGDAT file (magic number check failed)");
            }

            return new RawStageData(header, body);
        }

        internal static async Task<RawScreenshotData> LoadScreenshotDataAsync(FileInfo file)
        {
            var (header, body) = await LoadCompressedFileAsync(file, new FileDataReadOptions(), ScreenshotDataHeaderLength);

            if (!IsHeaderValid(header, ScreenshotDataMagicNumber))
            {
                throw new ArgumentException("Not a valid SCSHDAT file (magic number check failed)");
            }

            return new RawScreenshotData(header, body);
        }

        private sealed record FileContent(byte[] Header, byte[] Body);

        /// <summary>
        /// For files that have an uncompressed header of some fixed length followed by a compressed body
        /// </summary>
        private static async Task<FileContent> LoadCompressedFileAsync(FileInfo file, FileDataReadOptions options, int headerLength)
        {
            using var readStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, options.FileShare);
            return await LoadCompressedFileAsync(readStream, options, headerLength);
        }

        /// <summary>
        /// For files that have an uncompressed header of some fixed length followed by a compressed body
        /// </summary>
        private static async Task<FileContent> LoadCompressedFileAsync(Stream readStream, FileDataReadOptions options, int headerLength)
        {
            var header = new byte[headerLength];
            await readStream.ReadExactlyAsync(header, 0, headerLength);

            using var zlib = new ZLibStream(readStream, CompressionMode.Decompress, leaveOpen: true);
            using var bodyStream = new MemoryStream();
            await zlib.CopyToAsync(bodyStream);
            bodyStream.Flush();
            zlib.Flush();
            var body = bodyStream.ToArray();

            return new FileContent(header, body);
        }

        private static bool IsHeaderValid(ReadOnlySpan<byte> header, byte[] check)
        {
            return check.AsSpan().SequenceEqual(header.Slice(0, check.Length));
        }
    }
}
