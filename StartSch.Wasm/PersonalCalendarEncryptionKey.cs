namespace StartSch.Wasm;

public record PersonalCalendarEncryptionKey(byte[] AesKey, int UserId);
