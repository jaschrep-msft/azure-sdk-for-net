// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure.Core.Cryptography;
using Azure.Core.Testing;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Storage.Common.Cryptography;
using Azure.Storage.Common.Cryptography.Models;
using Azure.Storage.Queues.Specialized;
using Azure.Storage.Queues.Specialized.Models;
using Azure.Storage.Queues.Tests;
using Azure.Storage.Tests.Shared;
using NUnit.Framework;

namespace Azure.Storage.Queues.Cryptography.Tests
{
    public class EncryptedQueueClientTests : QueueTestBase
    {
        private readonly string SampleUTF8String = Encoding.UTF8.GetString(
            new byte[] { 0xe1, 0x9a, 0xa0, 0xe1, 0x9b, 0x87, 0xe1, 0x9a, 0xbb, 0x0a }); // valid UTF-8 bytes

        public EncryptedQueueClientTests(bool async)
            : base(async, null /* RecordedTestMode.Record /* to re-record */)
        {
        }

        #region Utility

        private string LocalManualEncryption(string message, byte[] key, byte[] iv)
        {
            using (var aesProvider = new AesCryptoServiceProvider() { Key = key, IV = iv })
            using (var encryptor = aesProvider.CreateEncryptor())
            using (var memStream = new MemoryStream())
            using (var cryptoStream = new CryptoStream(memStream, encryptor, CryptoStreamMode.Write))
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                cryptoStream.Write(messageBytes, 0, messageBytes.Length);
                cryptoStream.FlushFinalBlock();
                return Convert.ToBase64String(memStream.ToArray());
            }
        }

        private async Task<IKeyEncryptionKey> GetKeyvaultIKeyEncryptionKey()
        {
            var keyClient = GetKeyClient_TargetKeyClient();
            Security.KeyVault.Keys.KeyVaultKey key = await keyClient.CreateRsaKeyAsync(
                new Security.KeyVault.Keys.CreateRsaKeyOptions($"CloudRsaKey-{Guid.NewGuid()}", false));
            return new CryptographyClient(key.Id, GetTokenCredential_TargetKeyClient());
        }

        /// <summary>
        /// Creates an encrypted queue client from a normal queue client. Note that this method does not copy over any
        /// client options from the container client. You must pass in your own options. These options will be mutated.
        /// </summary>
        public EncryptedQueueClient GetEncryptedQueueClient(
            QueueClient queue,
            ClientSideEncryptionOptions encryptionOptions,
            QueueClientOptions options)
        {
            var client = InstrumentClient(new EncryptedQueueClient(
                queue.Uri,
                new StorageSharedKeyCredential(TestConfigDefault.AccountName, TestConfigDefault.AccountKey),
                encryptionOptions,
                options));

            return client;
        }

        /// <summary>
        /// Generates a random string of the given size.
        /// For implementation simplicity, this generates an ASCII string.
        /// </summary>
        /// <param name="size">Size of the string IN BYTES, not chars.</param>
        /// <returns></returns>
        public string GetRandomMessage(int size)
        {
            var buf = new byte[size];
            var random = new Random();
            for (int i = 0; i < size; i++)
            {
                buf[i] = (byte)random.Next(32, 127); // printable ASCII has values [32, 127)
            }

            return Encoding.ASCII.GetString(buf);
        }

        #endregion

        [TestCase(16, false)] // a single cipher block
        [TestCase(14, false)] // a single unalligned cipher block
        [TestCase(Constants.KB, false)] // multiple blocks
        [TestCase(Constants.KB - 4, false)] // multiple unalligned blocks
        [TestCase(0, true)] // utf8 support testing
        [LiveOnly] // cannot seed content encryption key
        public async Task UploadAsync(int messageSize, bool usePrebuiltMessage)
        {
            var message = usePrebuiltMessage
                ? GetRandomMessage(messageSize)
                : SampleUTF8String;
            var mockKey = new MockKeyEncryptionKey();
            await using (var disposable = await GetTestQueueAsync())
            {
                var queue = GetEncryptedQueueClient(
                    disposable.Queue,
                    new ClientSideEncryptionOptions()
                    {
                        KeyEncryptionKey = mockKey,
                        KeyResolver = mockKey
                    },
                    GetOptions());

                // upload with encryption
                await queue.SendMessageAsync(message);

                // download without decrypting
                var receivedMessages = (await disposable.Queue.ReceiveMessagesAsync()).Value;
                Assert.AreEqual(1, receivedMessages.Length);
                var encryptedMessage = receivedMessages[0].MessageText; // json of message and metadata
                var parsedEncryptedMessage = EncryptedMessageSerializer.Deserialize(encryptedMessage);

                // encrypt original data manually for comparison
                EncryptionData encryptionMetadata = parsedEncryptedMessage.EncryptionData;
                Assert.NotNull(encryptionMetadata, "Never encrypted data.");
                string expectedEncryptedMessage = LocalManualEncryption(
                    message,
                    (await mockKey.UnwrapKeyAsync(null, encryptionMetadata.WrappedContentKey.EncryptedKey)
                        .ConfigureAwait(false)).ToArray(),
                    encryptionMetadata.ContentEncryptionIV);

                // compare data
                Assert.AreEqual(expectedEncryptedMessage, parsedEncryptedMessage.EncryptedMessageContents);
            }
        }

        [TestCase(16, false)] // a single cipher block
        [TestCase(14, false)] // a single unalligned cipher block
        [TestCase(Constants.KB, false)] // multiple blocks
        [TestCase(Constants.KB - 4, false)] // multiple unalligned blocks
        [TestCase(0, true)] // utf8 support testing
        [LiveOnly] // cannot seed content encryption key
        public async Task RoundtripAsync(int messageSize, bool usePrebuiltMessage)
        {
            var message = usePrebuiltMessage
                ? GetRandomMessage(messageSize)
                : SampleUTF8String;
            var mockKey = new MockKeyEncryptionKey();
            await using (var disposable = await GetTestQueueAsync())
            {
                var queue = GetEncryptedQueueClient(
                    disposable.Queue,
                    new ClientSideEncryptionOptions()
                    {
                        KeyEncryptionKey = mockKey,
                        KeyResolver = mockKey
                    },
                    GetOptions());

                // upload with encryption
                await queue.SendMessageAsync(message);

                // download with decryption
                var receivedMessages = (await queue.ReceiveMessagesAsync()).Value;
                Assert.AreEqual(1, receivedMessages.Length);
                var downloadedMessage = receivedMessages[0].MessageText;

                // compare data
                Assert.AreEqual(message, downloadedMessage);
            }
        }

        [TestCase(Constants.KB, false)] // multiple blocks
        [TestCase(Constants.KB - 4, false)] // multiple unalligned blocks
        [TestCase(0, true)] // utf8 support testing
        [LiveOnly] // cannot seed content encryption key
        public async Task Track2DownloadTrack1Blob(int messageSize, bool usePrebuiltMessage)
        {
            var message = usePrebuiltMessage
                ? GetRandomMessage(messageSize)
                : SampleUTF8String;
            var mockKey = new MockKeyEncryptionKey();
            await using (var disposable = await GetTestQueueAsync())
            {
                var track2Queue = GetEncryptedQueueClient(
                    disposable.Queue,
                    new ClientSideEncryptionOptions()
                    {
                        KeyEncryptionKey = mockKey,
                        KeyResolver = mockKey
                    },
                    GetOptions());

                // upload with track 1
                var creds = GetNewSharedKeyCredentials();
                var track1Queue = new Microsoft.Azure.Storage.Queue.CloudQueue(
                    track2Queue.Uri,
                    new Microsoft.Azure.Storage.Auth.StorageCredentials(creds.AccountName, creds.GetAccountKey()));
                await track1Queue.AddMessageAsync(
                    new Microsoft.Azure.Storage.Queue.CloudQueueMessage(message),
                    null,
                    null,
                    new Microsoft.Azure.Storage.Queue.QueueRequestOptions()
                    {
                        EncryptionPolicy = new Microsoft.Azure.Storage.Queue.QueueEncryptionPolicy(mockKey, mockKey)
                    },
                    null);

                // download with track 2
                var receivedMessages = (await track2Queue.ReceiveMessagesAsync()).Value;
                Assert.AreEqual(1, receivedMessages.Length);
                var downloadedMessage = receivedMessages[0].MessageText;

                // compare original data to downloaded data
                Assert.AreEqual(message, downloadedMessage);
            }
        }

        [TestCase(Constants.KB, false)] // multiple blocks
        [TestCase(Constants.KB - 4, false)] // multiple unalligned blocks
        [TestCase(0, true)] // utf8 support testing
        [LiveOnly] // cannot seed content encryption key
        public async Task Track1DownloadTrack2Blob(int messageSize, bool usePrebuiltMessage)
        {
            var message = usePrebuiltMessage
                ? GetRandomMessage(messageSize)
                : SampleUTF8String;
            var mockKey = new MockKeyEncryptionKey();
            await using (var disposable = await GetTestQueueAsync())
            {
                var track2Queue = GetEncryptedQueueClient(
                    disposable.Queue,
                    new ClientSideEncryptionOptions()
                    {
                        KeyEncryptionKey = mockKey,
                        KeyResolver = mockKey
                    },
                    GetOptions());

                // upload with track 2
                await track2Queue.SendMessageAsync(message);

                // download with track 1
                var creds = GetNewSharedKeyCredentials();
                var track1Queue = new Microsoft.Azure.Storage.Queue.CloudQueue(
                    track2Queue.Uri,
                    new Microsoft.Azure.Storage.Auth.StorageCredentials(creds.AccountName, creds.GetAccountKey()));
                var response = await track1Queue.GetMessageAsync(
                    null,
                    new Microsoft.Azure.Storage.Queue.QueueRequestOptions()
                    {
                        EncryptionPolicy = new Microsoft.Azure.Storage.Queue.QueueEncryptionPolicy(mockKey, mockKey)
                    },
                    null);

                // compare original data to downloaded data
                Assert.AreEqual(message, response.AsString);
            }
        }

        [Test]
        [LiveOnly] // need access to keyvault service && cannot seed content encryption key
        public async Task RoundtripWithKeyvaultProvider()
        {
            var message = GetRandomMessage(Constants.KB);
            IKeyEncryptionKey key = await GetKeyvaultIKeyEncryptionKey();
            await using (var disposable = await GetTestQueueAsync())
            {
                var queue = new EncryptedQueueClient(
                    disposable.Queue.Uri,
                    GetNewSharedKeyCredentials(),
                    new ClientSideEncryptionOptions()
                    {
                        KeyEncryptionKey = key,
                        EncryptionKeyWrapAlgorithm = "RSA-OAEP-256"
                    },
                    GetOptions());

                await queue.SendMessageAsync(message);

                var receivedMessages = (await queue.ReceiveMessagesAsync()).Value;
                Assert.AreEqual(1, receivedMessages.Length);
                var downloadedMessage = receivedMessages[0].MessageText;

                Assert.AreEqual(message, downloadedMessage);
            }
        }

        [Test]
        [LiveOnly] // cannot seed content encryption key
        public async Task ReadPlaintextMessage()
        {
            var message = "any old message";
            var mockKey = new MockKeyEncryptionKey();
            await using (var disposable = await GetTestQueueAsync())
            {
                var plainQueueClient = disposable.Queue;
                var encryptedQueueClient = GetEncryptedQueueClient(
                    disposable.Queue,
                    new ClientSideEncryptionOptions()
                    {
                        KeyEncryptionKey = mockKey,
                        KeyResolver = mockKey
                    },
                    GetOptions());

                // upload with encryption
                await plainQueueClient.SendMessageAsync(message);

                // download with decryption
                var receivedMessages = (await encryptedQueueClient.ReceiveMessagesAsync()).Value;
                Assert.AreEqual(1, receivedMessages.Length);
                var downloadedMessage = receivedMessages[0].MessageText;

                // compare data
                Assert.AreEqual(message, downloadedMessage);
            }
        }

        [Test]
        [LiveOnly] // cannot seed content encryption key
        public async Task OnlyOneKeyWrapCall()
        {
            var message = "any old message";
            var mockKey = new MockKeyEncryptionKey();
            await using (var disposable = await GetTestQueueAsync())
            {
                var queue = GetEncryptedQueueClient(
                    disposable.Queue,
                    new ClientSideEncryptionOptions()
                    {
                        KeyEncryptionKey = mockKey,
                        KeyResolver = mockKey
                    },
                    GetOptions());

                await queue.SendMessageAsync(message).ConfigureAwait(false);

                Assert.AreEqual(1, IsAsync ? mockKey.WrappedAsync : mockKey.WrappedSync);
                Assert.AreEqual(0, IsAsync ? mockKey.WrappedSync : mockKey.WrappedAsync);
            }
        }

        [Test]
        [LiveOnly] // cannot seed content encryption key
        public async Task OnlyOneKeyResolveAndUnwrapCall()
        {
            var message = "any old message";
            var mockKey = new MockKeyEncryptionKey();
            await using (var disposable = await GetTestQueueAsync())
            {
                var queue = GetEncryptedQueueClient(
                    disposable.Queue,
                    new ClientSideEncryptionOptions()
                    {
                        KeyEncryptionKey = mockKey,
                        KeyResolver = mockKey
                    },
                    GetOptions());
                await queue.SendMessageAsync(message).ConfigureAwait(false);
                mockKey.ResetCounters();

                queue = GetEncryptedQueueClient(
                    disposable.Queue,
                    new ClientSideEncryptionOptions()
                    {
                        KeyEncryptionKey = default, // we want the key resolver to trigger; no cached key
                        KeyResolver = mockKey
                    },
                    GetOptions());

                await queue.ReceiveMessagesAsync();

                Assert.AreEqual(1, IsAsync ? mockKey.ResolvedAsync : mockKey.ResolvedSync);
                Assert.AreEqual(0, IsAsync ? mockKey.ResolvedSync : mockKey.ResolvedAsync);

                Assert.AreEqual(1, IsAsync ? mockKey.UnwrappedAsync : mockKey.UnwrappedSync);
                Assert.AreEqual(0, IsAsync ? mockKey.UnwrappedSync : mockKey.UnwrappedAsync);
            }
        }
    }
}
