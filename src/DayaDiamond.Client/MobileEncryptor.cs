using System.Security.Cryptography;
using System.Text;

namespace DayaDiamond.Client;

/// <summary>
/// رمزنگاری شماره موبایل طبق مستند: AES، حالت ECB، Padding از نوع PKCS7، خروجی Base64.
/// </summary>
public sealed class MobileEncryptor
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public MobileEncryptor(string base64Key, string base64IV)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
            throw new ArgumentException("کلید AES تنظیم نشده است.", nameof(base64Key));

        _key = Convert.FromBase64String(base64Key);
        if (_key.Length is not (16 or 24 or 32))
            throw new ArgumentException("طول کلید AES باید 16، 24 یا 32 بایت باشد.", nameof(base64Key));

        _iv = string.IsNullOrWhiteSpace(base64IV) ? new byte[16] : Convert.FromBase64String(base64IV);
    }

    public string Encrypt(string mobile)
    {
        byte[] plainText = Encoding.UTF8.GetBytes(mobile);

        using Aes aesAlg = Aes.Create();
        aesAlg.Key = _key;
        aesAlg.IV = _iv;
        aesAlg.Mode = CipherMode.ECB;
        aesAlg.Padding = PaddingMode.PKCS7;

        using ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainText, 0, plainText.Length);
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>برای تست: همان کاری که سمت دایا برای خواندن موبایل انجام می‌شود.</summary>
    public string Decrypt(string code)
    {
        byte[] cipherText = Convert.FromBase64String(code);

        using Aes aesAlg = Aes.Create();
        aesAlg.Key = _key;
        aesAlg.IV = _iv;
        aesAlg.Mode = CipherMode.ECB;
        aesAlg.Padding = PaddingMode.PKCS7;

        using ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
        byte[] plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
