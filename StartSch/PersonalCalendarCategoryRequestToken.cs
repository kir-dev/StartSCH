using System.Buffers.Binary;
using System.Buffers.Text;
using Microsoft.AspNetCore.DataProtection;

namespace StartSch;

public readonly struct PersonalCalendarCategoryRequestToken(int categoryId, byte[] aesKey)
{
    private const string DataProtectionPurpose = "StartSch.PersonalCalendarCategoryRequestToken";
    private const int PayloadSize = 4 + 32;
    
    public int CategoryId { get; } = categoryId;
    public byte[] AesKey { get; } = aesKey;

    public string Serialize(IDataProtectionProvider dataProtectionProvider)
    {
        if (AesKey.Length != 32)
            throw new NotSupportedException();
        
        byte[] unprotectedData = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(unprotectedData, CategoryId);
        AesKey.CopyTo(unprotectedData.AsSpan(4));
        
        byte[] protectedData = dataProtectionProvider
            .CreateProtector(DataProtectionPurpose)
            .Protect(unprotectedData);
        return Base64Url.EncodeToString(protectedData);
    }

    public static PersonalCalendarCategoryRequestToken Deserialize(string s, IDataProtectionProvider dataProtectionProvider)
    {
        byte[] protectedData = Base64Url.DecodeFromChars(s);
        byte[] unprotectedData = dataProtectionProvider
            .CreateProtector(DataProtectionPurpose)
            .Unprotect(protectedData);
        if (unprotectedData.Length != PayloadSize)
            throw new InvalidOperationException();
        return new(
            BinaryPrimitives.ReadInt32LittleEndian(unprotectedData),
            unprotectedData.AsSpan(4).ToArray()
        );
    }
}
