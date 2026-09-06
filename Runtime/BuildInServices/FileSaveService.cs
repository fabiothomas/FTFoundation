using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FTFoundation.BuildInReferences;
using FTFoundation.Core;
using UnityEngine;

namespace FTFoundation.BuildInServices
{
    [InstantiateOnStartup]
    [Service(typeof(IFileSaveService), ServiceType.SINGLETON)]
    public class FileSaveService : IFileSaveService
    {
        private const string SavePath = "savedata/save.dat";
        private const float FlushInterval = 5f;
        // Passphrase used to derive the AES key. Prevents casual file editing.
        private const string Passphrase = "FTFoundation_SaveData_v1";

        private IFileService _fileService = null!;
        private readonly Dictionary<string, string> _data = new();
        private bool _isDirty;
        private float _timeSinceLastFlush;
        private IDisposable? _updateSubscription;

        void Inject(IFileService fileService, ILifetimeService lifetimeService)
        {
            _fileService = fileService;
            Load();
            _updateSubscription = lifetimeService.OnUpdate(OnUpdate);
            Application.quitting += OnApplicationQuit;
        }

        public void Set(string id, string serializedValue)
        {
            _data[id] = serializedValue;
            _isDirty = true;
        }

        public string? Get(string id)
        {
            _data.TryGetValue(id, out string? value);
            return value;
        }

        public void Flush()
        {
            if (!_isDirty) return;

            string json = SerializeDict(_data);
            string encrypted = Encrypt(json);
            _fileService.Write(SavePath, encrypted);
            _isDirty = false;
            _timeSinceLastFlush = 0f;
        }

        private void OnUpdate()
        {
            if (!_isDirty) return;
            _timeSinceLastFlush += Time.deltaTime;
            if (_timeSinceLastFlush >= FlushInterval)
                Flush();
        }

        private void OnApplicationQuit()
        {
            Flush();
        }

        private void Load()
        {
            if (!_fileService.TryRead(SavePath, out string encrypted))
                return;

            try
            {
                string json = Decrypt(encrypted);
                DeserializeInto(json, _data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FileSaveService] Failed to load save data: {e.Message}");
            }
        }

        // ── Serialization ─────────────────────────────────────────────────────────
        // Simple line-per-entry format: "id:value\n"
        // Values are Base64-encoded to survive newlines inside the serialized value.
        // The delimiter must not be a character that can appear in Base64 output
        // (letters, digits, '+', '/', '=' padding) or it can collide with padding
        // on the encoded key and corrupt parsing.
        private const char FieldDelimiter = ':';

        private static string SerializeDict(Dictionary<string, string> dict)
        {
            var sb = new StringBuilder();
            foreach (var kvp in dict)
            {
                string encodedKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(kvp.Key));
                string encodedValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(kvp.Value));
                sb.Append(encodedKey).Append(FieldDelimiter).Append(encodedValue).Append('\n');
            }
            return sb.ToString();
        }

        private static void DeserializeInto(string text, Dictionary<string, string> target)
        {
            foreach (string line in text.Split('\n'))
            {
                if (string.IsNullOrEmpty(line)) continue;
                int sep = line.IndexOf(FieldDelimiter);
                if (sep < 0) continue;
                string key = Encoding.UTF8.GetString(Convert.FromBase64String(line.Substring(0, sep)));
                string value = Encoding.UTF8.GetString(Convert.FromBase64String(line.Substring(sep + 1)));
                target[key] = value;
            }
        }

        // ── AES-CBC encryption ─────────────────────────────────────────────────────
        // Key  = SHA-256 of the passphrase bytes (32 bytes → AES-256)
        // IV   = random 16 bytes, prepended to the ciphertext before Base64 encoding

        private static byte[] DeriveKey()
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(Passphrase));
        }

        private static string Encrypt(string plaintext)
        {
            byte[] key = DeriveKey();
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
                sw.Write(plaintext);

            return Convert.ToBase64String(ms.ToArray());
        }

        private static string Decrypt(string cipherBase64)
        {
            byte[] key = DeriveKey();
            byte[] allBytes = Convert.FromBase64String(cipherBase64);

            using var aes = Aes.Create();
            aes.Key = key;

            byte[] iv = new byte[aes.BlockSize / 8];
            Array.Copy(allBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var ms = new MemoryStream(allBytes, iv.Length, allBytes.Length - iv.Length);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }
    }
}
