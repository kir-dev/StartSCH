namespace StartSch.Wasm;

public record ResetEncryptionKeyResult(
    byte[] AesKey,
    string EncryptionToken
);
