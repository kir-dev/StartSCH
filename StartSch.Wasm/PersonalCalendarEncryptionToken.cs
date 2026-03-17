namespace StartSch.Wasm;

public record PersonalCalendarEncryptionToken(byte[] AesKey, int UserId);
