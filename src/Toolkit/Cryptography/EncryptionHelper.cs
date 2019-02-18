using System.IO;
using System.Security.Cryptography;

namespace Toolkit.Cryptography
{
	public static class EncryptionHelper
	{
		public static byte[] Encrypt(byte[] content, byte[] key, byte[] initVector)
		{
			using (var algorithm = new RijndaelManaged())
			{
				using (var stream = new MemoryStream())
				{
					using (var cryptoStream = new CryptoStream(stream, algorithm.CreateEncryptor(key, initVector), CryptoStreamMode.Write))
					{
						cryptoStream.Write(content, 0, content.Length);
					}
					return stream.ToArray();
				}
			}
		}

		public static byte[] Decrypt(byte[] content, byte[] key, byte[] initVector)
		{
			using (var algorithm = new RijndaelManaged())
			{
				using (var stream = new MemoryStream())
				{
					using (var cryptoStream = new CryptoStream(stream, algorithm.CreateDecryptor(key, initVector), CryptoStreamMode.Write))
					{
						cryptoStream.Write(content, 0, content.Length);
					}
					return stream.ToArray();
				}
			}
		}
	}
}