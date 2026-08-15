using System.IO;
using System.Text;
using NUnit.Framework;
using SeerLauncher.Services;

namespace SeerLauncher.Tests
{
    [TestFixture]
    public class ConfigServiceTests
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
        public void Load_WhenNothingExists_CreatesDefaultKeywords()
        {
            var service = new ConfigService(_dir);
            var config = service.Load();

            Assert.AreEqual(4, config.Keywords.Count);
            Assert.Contains("Seer", config.Keywords);
            Assert.Contains("雷小伊", config.Keywords);
            Assert.IsTrue(File.Exists(Path.Combine(_dir, ConfigService.ConfigFileName)));
        }

        [Test]
        public void Load_WhenIniExists_MigratesToJsonAndBacksUpIni()
        {
            var iniContent = "[config]\r\nkeywords=Seer|雷小伊\r\n"
                           + "[filesname]\r\nfilesname=ChikaLauncher.exe\r\n"
                           + "[fileconfig]\r\nChikaLauncher.exe=C:\\Games\\\r\n";
            File.WriteAllText(Path.Combine(_dir, ConfigService.IniFileName), iniContent, Encoding.Default);

            var service = new ConfigService(_dir);
            var config = service.Load();

            Assert.AreEqual(2, config.Keywords.Count);
            Assert.AreEqual("Seer", config.Keywords[0]);
            Assert.AreEqual("雷小伊", config.Keywords[1]);
            Assert.AreEqual("C:\\Games\\", config.Programs["ChikaLauncher.exe"]);
            Assert.IsTrue(File.Exists(Path.Combine(_dir, ConfigService.ConfigFileName)));
            Assert.IsFalse(File.Exists(Path.Combine(_dir, ConfigService.IniFileName)));
            Assert.IsTrue(File.Exists(Path.Combine(_dir, ConfigService.IniBackupFileName)));
        }

        [Test]
        public void Save_ThenLoad_RoundTrips()
        {
            var service = new ConfigService(_dir);
            var config = service.Load();
            config.Keywords.Add("新关键字");
            config.Programs["Test.exe"] = "D:\\tools\\";
            service.Save();

            var reloaded = new ConfigService(_dir).Load();
            Assert.Contains("新关键字", reloaded.Keywords);
            Assert.AreEqual("D:\\tools\\", reloaded.Programs["Test.exe"]);
        }

        [Test]
        public void IsValidKeyword_RejectsIllegalChars()
        {
            Assert.IsFalse(ConfigService.IsValidKeyword("a/b"));
            Assert.IsFalse(ConfigService.IsValidKeyword("a|b"));
            Assert.IsFalse(ConfigService.IsValidKeyword("a\"b"));
            Assert.IsFalse(ConfigService.IsValidKeyword("a<b"));
            Assert.IsTrue(ConfigService.IsValidKeyword("雷小伊"));
            Assert.IsTrue(ConfigService.IsValidKeyword("Seer"));
        }

        [Test]
        public void Split_FiltersEmptyParts()
        {
            var parts = ConfigService.Split("Seer||雷小伊");
            Assert.AreEqual(2, parts.Count);
            Assert.AreEqual("Seer", parts[0]);
            Assert.AreEqual("雷小伊", parts[1]);
        }
    }
}