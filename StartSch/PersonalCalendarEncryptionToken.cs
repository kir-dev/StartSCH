using System.Buffers.Binary;
using System.Buffers.Text;
using Microsoft.AspNetCore.DataProtection;

namespace StartSch;

public readonly ref struct PersonalCalendarEncryptionToken(int userId, ReadOnlySpan<byte> aesKey)
{
    private const string DataProtectionPurpose = "StartSch.PersonalCalendarEncryptionToken";
    private const int UnprotectedDataLength = 4 + 32;
    
    public int UserId { get; } = userId;
    public ReadOnlySpan<byte> AesKey { get; } = aesKey;

    public string Serialize(IDataProtectionProvider dataProtectionProvider)
    {
        if (AesKey.Length != 32)
            throw new NotSupportedException();
        
        byte[] unprotectedData = new byte[UnprotectedDataLength];
        BinaryPrimitives.WriteInt32LittleEndian(unprotectedData, UserId);
        AesKey.CopyTo(unprotectedData.AsSpan(4));
        
        byte[] protectedData = dataProtectionProvider
            .CreateProtector(DataProtectionPurpose)
            .Protect(unprotectedData);
        return Base64Url.EncodeToString(protectedData);
    }

    public static PersonalCalendarEncryptionToken Deserialize(string s, IDataProtectionProvider dataProtectionProvider)
    {
        byte[] protectedData = Base64Url.DecodeFromChars(s);
        byte[] unprotectedData = dataProtectionProvider
            .CreateProtector(DataProtectionPurpose)
            .Unprotect(protectedData);
        if (unprotectedData.Length != UnprotectedDataLength)
            throw new InvalidOperationException();
        return new(
            BinaryPrimitives.ReadInt32LittleEndian(unprotectedData),
            unprotectedData.AsSpan(4)
        );
    }
}
