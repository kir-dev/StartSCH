// https://developer.mozilla.org/en-US/docs/Web/API/SubtleCrypto/digest#converting_a_digest_to_a_hex_string
export async function computeSha256(s: string) {
    const utf8Bytes = new TextEncoder().encode(s);
    const hashBuffer = await window.crypto.subtle.digest("SHA-256", utf8Bytes);
    const hashArray = Array.from(new Uint8Array(hashBuffer));
    return hashArray
        .map(b => b.toString(16).padStart(2, "0"))
        .join("")
        .toUpperCase();
}
