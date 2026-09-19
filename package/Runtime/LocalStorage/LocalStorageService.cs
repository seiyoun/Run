/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: JSON を暗号化してローカルファイルへ保存する機能を提供する。
 */

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Shiyuan.Foundation.LocalStorage
{
    public sealed class LocalStorageService
    {
        private const int Version = 1;
        private const int AesKeySize = 32;
        private const int HmacKeySize = 32;
        private const int IvSize = 16;
        private const string FileExtension = ".hls";
        private const string FixedSecret = "Hero.LocalStorage.v1.FixedSecret.2026";

        private readonly string storageDirectory;
        private readonly byte[] aesKey;
        private readonly byte[] hmacKey;

        /// <summary>
        /// 永続データパスを使用するローカル保存サービスを作成する。
        /// </summary>
        public LocalStorageService()
            : this(Path.Combine(Application.persistentDataPath, "LocalStorage"))
        {
        }

        /// <summary>
        /// 指定したディレクトリを使用するローカル保存サービスを作成する。
        /// </summary>
        public LocalStorageService(string storageDirectory)
        {
            if (string.IsNullOrWhiteSpace(storageDirectory))
            {
                throw new ArgumentException("保存先ディレクトリが未設定です。", nameof(storageDirectory));
            }

            this.storageDirectory = storageDirectory;
            var keyMaterial = DeriveKeyMaterial();
            aesKey = new byte[AesKeySize];
            hmacKey = new byte[HmacKeySize];
            Buffer.BlockCopy(keyMaterial, 0, aesKey, 0, AesKeySize);
            Buffer.BlockCopy(keyMaterial, AesKeySize, hmacKey, 0, HmacKeySize);
        }

        /// <summary>
        /// 指定したキーにデータを保存する。
        /// </summary>
        public void Save<T>(string key, T data)
        {
            ValidateKey(key);
            SaveInternal(key, data);
        }

        /// <summary>
        /// 指定したキーのデータを読み込む。
        /// </summary>
        public LocalStorageResult<T> Load<T>(string key)
        {
            ValidateKey(key);
            return LoadInternal<T>(key);
        }

        /// <summary>
        /// 指定したキーの保存データが存在するかどうかを取得する。
        /// </summary>
        public bool Exists(string key)
        {
            ValidateKey(key);
            return File.Exists(GetFilePath(key));
        }

        /// <summary>
        /// 指定したキーの保存データを削除する。
        /// </summary>
        public void Delete(string key)
        {
            ValidateKey(key);
            var filePath = GetFilePath(key);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// ローカル保存サービスが管理する全データを削除する。
        /// </summary>
        public void DeleteAll()
        {
            if (!Directory.Exists(storageDirectory))
            {
                return;
            }

            foreach (var filePath in Directory.GetFiles(storageDirectory, $"*{FileExtension}"))
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// JSON 化したデータを暗号化してファイルへ書き込む。
        /// </summary>
        private void SaveInternal<T>(string key, T data)
        {
            Directory.CreateDirectory(storageDirectory);

            var json = JsonUtility.ToJson(data);
            var envelope = Encrypt(json);
            var fileJson = JsonUtility.ToJson(envelope);
            var filePath = GetFilePath(key);
            var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";

            try
            {
                File.WriteAllText(temporaryPath, fileJson, Encoding.UTF8);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                File.Move(temporaryPath, filePath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        /// <summary>
        /// ファイルから暗号化データを読み込み、復号して型へ戻す。
        /// </summary>
        private LocalStorageResult<T> LoadInternal<T>(string key)
        {
            var filePath = GetFilePath(key);
            if (!File.Exists(filePath))
            {
                return LocalStorageResult<T>.Failure(LocalStorageStatus.NotFound, "保存データが存在しません。");
            }

            try
            {
                var fileJson = File.ReadAllText(filePath, Encoding.UTF8);

                var envelope = JsonUtility.FromJson<EncryptedLocalStorageEnvelope>(fileJson);
                if (envelope == null || envelope.version != Version)
                {
                    return LocalStorageResult<T>.Failure(LocalStorageStatus.InvalidData, "保存データの形式が不正です。");
                }

                var json = Decrypt(envelope);
                var value = JsonUtility.FromJson<T>(json);
                return LocalStorageResult<T>.Success(value);
            }
            catch (CryptographicException exception)
            {
#if DEBUG_LOG
                Debug.LogWarning($"ローカル保存データの復号に失敗しました。key:{key} error:{exception.Message}");
#endif
                return LocalStorageResult<T>.Failure(LocalStorageStatus.CryptoError, "保存データの復号に失敗しました。");
            }
            catch (FormatException exception)
            {
#if DEBUG_LOG
                Debug.LogWarning($"ローカル保存データの形式が不正です。key:{key} error:{exception.Message}");
#endif
                return LocalStorageResult<T>.Failure(LocalStorageStatus.InvalidData, "保存データの形式が不正です。");
            }
            catch (ArgumentException exception)
            {
#if DEBUG_LOG
                Debug.LogWarning($"ローカル保存データの読み込みに失敗しました。key:{key} error:{exception.Message}");
#endif
                return LocalStorageResult<T>.Failure(LocalStorageStatus.InvalidData, "保存データの読み込みに失敗しました。");
            }
            catch (IOException exception)
            {
#if DEBUG_LOG
                Debug.LogWarning($"ローカル保存データの読み込みに失敗しました。key:{key} error:{exception.Message}");
#endif
                return LocalStorageResult<T>.Failure(LocalStorageStatus.InvalidData, "保存データの読み込みに失敗しました。");
            }
        }

        /// <summary>
        /// 平文 JSON を AES-CBC で暗号化し、HMAC を付与する。
        /// </summary>
        private EncryptedLocalStorageEnvelope Encrypt(string json)
        {
            var iv = CreateRandomBytes(IvSize);
            byte[] cipherText;

            using (var aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                var plainBytes = Encoding.UTF8.GetBytes(json);
                cipherText = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }

            var mac = ComputeMac(Version, iv, cipherText);
            return new EncryptedLocalStorageEnvelope
            {
                version = Version,
                iv = Convert.ToBase64String(iv),
                cipherText = Convert.ToBase64String(cipherText),
                mac = Convert.ToBase64String(mac)
            };
        }

        /// <summary>
        /// HMAC を検証したうえで暗号文を JSON へ復号する。
        /// </summary>
        private string Decrypt(EncryptedLocalStorageEnvelope envelope)
        {
            var iv = Convert.FromBase64String(envelope.iv);
            var cipherText = Convert.FromBase64String(envelope.cipherText);
            var savedMac = Convert.FromBase64String(envelope.mac);
            var computedMac = ComputeMac(envelope.version, iv, cipherText);

            if (!FixedTimeEquals(savedMac, computedMac))
            {
                throw new CryptographicException("保存データの改ざんを検知しました。");
            }

            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>
        /// 保存キーからファイルパスを作成する。
        /// </summary>
        private string GetFilePath(string key)
        {
            return Path.Combine(storageDirectory, $"{ComputeHexHash(key)}{FileExtension}");
        }

        /// <summary>
        /// 保存キーが使用可能か検証する。
        /// </summary>
        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("保存キーが未設定です。", nameof(key));
            }
        }

        /// <summary>
        /// アプリと端末の情報から暗号化用の鍵素材を導出する。
        /// </summary>
        private static byte[] DeriveKeyMaterial()
        {
            var source = string.Join(
                "|",
                FixedSecret,
                Application.companyName,
                Application.productName,
                SystemInfo.deviceUniqueIdentifier);

            using var sha512 = SHA512.Create();
            return sha512.ComputeHash(Encoding.UTF8.GetBytes(source));
        }

        /// <summary>
        /// 指定した長さの暗号論的乱数を作成する。
        /// </summary>
        private static byte[] CreateRandomBytes(int length)
        {
            var bytes = new byte[length];
            using var random = RandomNumberGenerator.Create();
            random.GetBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// 暗号文の改ざん検知用 HMAC を計算する。
        /// </summary>
        private byte[] ComputeMac(int version, byte[] iv, byte[] cipherText)
        {
            var versionBytes = BitConverter.GetBytes(version);
            var source = new byte[versionBytes.Length + iv.Length + cipherText.Length];
            Buffer.BlockCopy(versionBytes, 0, source, 0, versionBytes.Length);
            Buffer.BlockCopy(iv, 0, source, versionBytes.Length, iv.Length);
            Buffer.BlockCopy(cipherText, 0, source, versionBytes.Length + iv.Length, cipherText.Length);

            using var hmac = new HMACSHA256(hmacKey);
            return hmac.ComputeHash(source);
        }

        /// <summary>
        /// 文字列を SHA-256 の 16 進文字列へ変換する。
        /// </summary>
        private static string ComputeHexHash(string text)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
            var builder = new StringBuilder(hash.Length * 2);
            foreach (var value in hash)
            {
                builder.Append(value.ToString("x2"));
            }

            return builder.ToString();
        }

        /// <summary>
        /// タイミング差を抑えてバイト列を比較する。
        /// </summary>
        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            var difference = 0;
            for (var i = 0; i < left.Length; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }

        [Serializable]
        private sealed class EncryptedLocalStorageEnvelope
        {
            public int version;
            public string iv;
            public string cipherText;
            public string mac;
        }
    }
}
