using NUnit.Framework;
using System;
using PublixVaultProxy;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Sockets;
namespace PublixVaultProxy.Tests
{
    [TestFixture]
    public class Tests
    {
        private VaultProxyService _vaultProxyService;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private HttpClient _httpClient;
        private const string _vaultUrl = "http://unittestvaulturl.com";

        [SetUp]
        public void Setup()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _vaultProxyService = new VaultProxyService(_vaultUrl, _httpClient);
        }

        [Test]
        public async Task RetrieveSecretFromVaultAsync_ShouldRetrurnTrue_WhenCacheLocally()
        {
            var secret = "VaultFakeSecret";
            _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(secret)
            });
            var result = await _vaultProxyService.RetrieveSecretFromRemoteVaultAsync();
            Assert.That(result);
            string cacheFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "offlineToken.dat");
            Assert.That(File.Exists(cacheFilePath), "Cache file should be created");
        }

        [Test]
        public async Task RetrieveSecretFromVaultAsync_ShouldRetrurnFalse_WhenRequestFails()
        {

            _mockHttpMessageHandler
             .Protected()
             .Setup<Task<HttpResponseMessage>>(
                 "SendAsync",
                 ItExpr.IsAny<HttpRequestMessage>(),
                 ItExpr.IsAny<CancellationToken>())
             .ThrowsAsync(new HttpRequestException("Network error"));
            bool cache_status = await _vaultProxyService.RetrieveSecretFromRemoteVaultAsync();
            await Task.Delay(500);
            Assert.That(cache_status, Is.False);
        }

        [Test]
        public async Task RetrieveSecretFromVaultAsync_ShouldRetrurnFalse_SecretIsNull()
        {
            // Empty response simulating null secret
            _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(string.Empty)
            });
            var result = await _vaultProxyService.RetrieveSecretFromRemoteVaultAsync();
            Assert.That(result, Is.False);

        }


    }
}