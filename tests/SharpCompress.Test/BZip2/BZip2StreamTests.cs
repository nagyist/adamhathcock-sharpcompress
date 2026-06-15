using System.IO;
using System.Text;
using SharpCompress.Common;
using SharpCompress.Compressors.BZip2;
using Xunit;

namespace SharpCompress.Test.BZip2;

public class BZip2StreamTests
{
    [Fact]
    public void BZip2Stream_Throws_On_Corrupt_Checksum()
    {
        var compressed = Compress("BZip2 checksum validation test data.");
        compressed[^5] ^= 1;

        using var stream = BZip2Stream.Create(
            new MemoryStream(compressed),
            SharpCompress.Compressors.CompressionMode.Decompress,
            false
        );
        using var output = new MemoryStream();

        Assert.Throws<ArchiveOperationException>(() => stream.CopyTo(output));
    }

    private static byte[] Compress(string value)
    {
        using var memoryStream = new MemoryStream();
        using (
            var bzip2Stream = BZip2Stream.Create(
                memoryStream,
                SharpCompress.Compressors.CompressionMode.Compress,
                false,
                leaveOpen: true
            )
        )
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            bzip2Stream.Write(bytes, 0, bytes.Length);
        }

        return memoryStream.ToArray();
    }
}
