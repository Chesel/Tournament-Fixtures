using System;
using System.Security.Cryptography;
using System.Text;

public static class UrlEncryptionHelper
{
    private static readonly string Key = "A1B2C3D4E5F6G7H8"; // 16-char key
    private static readonly string IV = "1H2G3F4E5D6C7B8A"; // 16-char IV

    public static string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(Key);
        aes.IV = Encoding.UTF8.GetBytes(IV);

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encrypted)
            .Replace("+", "-").Replace("/", "_").Replace("=", ""); // URL-safe
    }

    public static string Decrypt(string encryptedText)
    {
        var base64 = encryptedText.Replace("-", "+").Replace("_", "/");
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(Key);
        aes.IV = Encoding.UTF8.GetBytes(IV);

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var encryptedBytes = Convert.FromBase64String(base64);
        var decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        return Encoding.UTF8.GetString(decrypted);
    }
}
