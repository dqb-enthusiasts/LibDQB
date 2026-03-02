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

        internal const uint CommonDataHeader = 0x2A444; // 172,080 bytes
        internal const uint CommonDataDecompressedBodyLength = 5627194; // The decompressed body will always have this length?

        readonly static byte[] CommonDataMagicNumber = { 0x61, 0x65, 0x72, 0x43, 0x02 };

        internal const uint StageDataHeader = 0x110; // 272 bytes
        readonly static byte[] StageDataMagicNumber = { 0x61, 0x65, 0x72, 0x43, 0xDD };

        internal const uint ScreenshotDataHeader = 0x40; 
        readonly static byte[] ScreenshotDataMagicNumber = { 0x61, 0x65, 0x72, 0x43, 0x10 };

        //===================================== Functions =====================================//
        public static Task<FileData> LoadCommonDataAsync(FileInfo file) => LoadAsync(file, new FileDataReadOptions(),0);
        public static Task<FileData> LoadStageDataAsync(FileInfo file) => LoadAsync(file, new FileDataReadOptions(),1);
        public static Task<FileData> LoadScreenshotDataAsync(FileInfo file) => LoadAsync(file, new FileDataReadOptions(),2);

        /* S> I am using a "type" argument to distinguish the files. They all load with the same LoadAsync
        If you think it would be cleaner to somehow have FileDataReadOptions contain the differenciator I'm down
        I'm also down for a record that holds the relevant options
        I just want to keep the redundancy in fucntions to a minimum. Don't write the same thing twice. Does not end well usually. 
        */
        public static async Task<FileData> LoadAsync(FileInfo file, FileDataReadOptions options, byte type)
        {
            using var readStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, options.FileShare);
            return await LoadAsync(readStream, options, type);
        }

        public static async Task<FileData> LoadAsync(Stream readStream, FileDataReadOptions options, byte type)
        {
            var header = new byte[type switch{0 => CommonDataHeader, 1 => StageDataHeader, _ => ScreenshotDataHeader}];

            await readStream.ReadExactlyAsync(header, 0, 
               (int)(type switch { 0 => CommonDataHeader, 1 => StageDataHeader, _ => ScreenshotDataHeader }));

            if (!IsHeaderValid(header, type switch { 0 => CommonDataMagicNumber, 1 => StageDataMagicNumber, _ => ScreenshotDataMagicNumber }))
                throw new ArgumentException($"Not a valid file (magic number check failed)");

            var body = await DecompressAndValidateLength(readStream);

            if (type == 0 && body.Length != CommonDataDecompressedBodyLength)
                throw new ArgumentException("Not a valid CMNDAT file (decompressed length check failed)");

            return type switch
            {
                0 => new RawCommonData(header, body),
                1 => new RawStageData(header, body),
                _ => new RawScreenshotData(header, body),
            };    
        }

        // S> Isn't there a fancier way to do this? So many fancy things in c# and this looks like c implementation >u<
        private static bool IsHeaderValid(ReadOnlySpan<byte> header, byte[] check)
        {
            for (int i = 0; i < check.Length; i++)
            {
                if (header[i] != check[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static async Task<byte[]> DecompressAndValidateLength(Stream readStream)
        {
            // S> I'm going to change this to allow STGDAT with the same function. Feel free to change.

            using var zlib = new ZLibStream(readStream, CompressionMode.Decompress, leaveOpen: true);
            using var bodyStream = new MemoryStream();
            await zlib.CopyToAsync(bodyStream); //S> Why async if we're just gonna wait tho.
            bodyStream.Flush();
            zlib.Flush();
            return bodyStream.ToArray();
            /*
            var body = new byte[decompressedBodyLength];
            if (bodyStream.Position != decompressedBodyLength)
            {
                throw new ArgumentException("Not a valid CMNDAT file (decompressed length check failed)");
            }

            return body;
            */
        }
    }
}
