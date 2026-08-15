using System.IO;
using NUnit.Framework;
using SeerLauncher.Services;

namespace SeerLauncher.Tests
{
    [TestFixture]
    public class ProgramScanServiceTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "SeerLauncherTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void Scan_FindsMatchingExes_CaseInsensitive()
        {
            File.WriteAllText(Path.Combine(_dir, "ChikaLauncher.exe"), "");
            File.WriteAllText(Path.Combine(_dir, "SeerHelper.exe"), "");
            File.WriteAllText(Path.Combine(_dir, "unrelated.exe"), "");
            File.WriteAllText(Path.Combine(_dir, "readme.txt"), "");

            var service = new ProgramScanService();
            var result = service.Scan(_dir, "SeerLauncher.exe", new[] { "chika", "seer" });

            Assert.AreEqual(2, result.Count);
            CollectionAssert.Contains(result, "ChikaLauncher.exe");
            CollectionAssert.Contains(result, "SeerHelper.exe");
        }

        [Test]
        public void Scan_ExcludesSelf()
        {
            File.WriteAllText(Path.Combine(_dir, "ChikaLauncher.exe"), "");

            var service = new ProgramScanService();
            var result = service.Scan(_dir, "ChikaLauncher.exe", new[] { "chika" });

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Merge_ConfiguredFirst_ScannedDeduped()
        {
            var service = new ProgramScanService();
            var result = service.MergeConfiguredAndScanned(
                new[] { "A.exe", "B.exe" },
                new[] { "B.exe", "C.exe" });

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("A.exe", result[0]);
            Assert.AreEqual("B.exe", result[1]);
            Assert.AreEqual("C.exe", result[2]);
        }

        [Test]
        public void Merge_KeepsConfiguredEvenIfNotOnDisk()
        {
            var service = new ProgramScanService();
            var result = service.MergeConfiguredAndScanned(
                new[] { "Ghost.exe" },
                new string[0]);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Ghost.exe", result[0]);
        }
    }
}