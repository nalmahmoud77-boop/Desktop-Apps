using System.Security.Cryptography;
using System.Text;

namespace StoreApp.Licensing
{
    internal static class LicenseSecret
    {
        // 32-byte HMAC secret embedded in the shared library.
        // Both the keygen tool and the runtime app rely on this value matching.
        // Rotating it invalidates every previously issued key.
        internal static readonly byte[] Bytes =
        {
            0x7A, 0x3F, 0xB1, 0x29, 0xC4, 0x5E, 0x08, 0xD7,
            0x91, 0x6B, 0x4A, 0xE2, 0x0C, 0x55, 0xF3, 0x8D,
            0x42, 0x17, 0xBE, 0x60, 0xA9, 0x33, 0xCF, 0x71,
            0x5C, 0x88, 0xE0, 0x14, 0x9D, 0x26, 0x4F, 0xB5
        };

        internal static byte[] Hmac(string payload)
        {
            using var hmac = new HMACSHA256(Bytes);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        }
    }
}
