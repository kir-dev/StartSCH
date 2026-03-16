using System.Security.Cryptography;

namespace StartSch;

public static class Crypto
{
    public static byte[] GenerateAesEncryptionKey()
    {
        return RandomNumberGenerator.GetBytes(32);
    }
}
