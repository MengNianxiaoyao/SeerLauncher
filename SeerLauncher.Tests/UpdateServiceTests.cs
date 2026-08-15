using NUnit.Framework;
using SeerLauncher.Services;

namespace SeerLauncher.Tests
{
    [TestFixture]
    public class UpdateServiceTests
    {
        [Test]
        public void Parse_ExtractsAllMarkers()
        {
            var service = new UpdateService(Constants.UserAgent);
            var html = "前缀最新版本【2.4.0】最新版本中间"
                     + "下载链接【https://example.com/x.exe】下载链接"
                     + "更新信息【修复了若干bug】更新信息"
                     + "强制更新【否】强制更新后缀";

            var info = service.Parse(html);

            Assert.AreEqual("2.4.0", info.Version);
            Assert.AreEqual("https://example.com/x.exe", info.DownloadUrl);
            Assert.AreEqual("修复了若干bug", info.Info);
            Assert.AreEqual("否", info.ForceUpdate);
            Assert.IsFalse(info.IsForceUpdate);
        }

        [Test]
        public void Parse_WhenMarkerMissing_ReturnsEmpty()
        {
            var service = new UpdateService(Constants.UserAgent);
            var info = service.Parse("没有标记的内容");
            Assert.AreEqual("", info.Version);
        }

        [Test]
        public void IsNewer_ComparesVersionNumbers()
        {
            Assert.IsTrue(UpdateService.IsNewer("2.3.7", "2.3.6"));
            Assert.IsTrue(UpdateService.IsNewer("2.10.0", "2.9.9"));
            Assert.IsFalse(UpdateService.IsNewer("2.3.6", "2.3.6"));
            Assert.IsFalse(UpdateService.IsNewer("2.3.5", "2.3.6"));
        }

        [Test]
        public void ToVersionInt_MapsToComparableInteger()
        {
            Assert.AreEqual(20306, UpdateService.ToVersionInt("2.3.6"));
            Assert.AreEqual(21009, UpdateService.ToVersionInt("2.10.9"));
        }
    }
}