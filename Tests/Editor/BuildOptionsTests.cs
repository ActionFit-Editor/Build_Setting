#if UNITY_EDITOR

using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ActionFit.BuildSetting.Editor.Tests
{
    public class BuildOptionsTests
    {
        private BuildSettingsSO _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<BuildSettingsSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void DevelopmentBuildDefaultsOffAndPreservesOptions()
        {
            BuildOptions baseOptions = BuildOptions.AutoRunPlayer;

            Assert.That(_settings.developmentBuild, Is.False);
            Assert.That(_settings.ResolveBuildOptions(baseOptions), Is.EqualTo(baseOptions));
        }

        [TestCase(BuildOptions.None)]
        [TestCase(BuildOptions.AutoRunPlayer)]
        [TestCase(BuildOptions.AcceptExternalModificationsToPlayer)]
        public void DevelopmentBuildAddsFlagAndPreservesExistingOptions(BuildOptions baseOptions)
        {
            _settings.developmentBuild = true;

            BuildOptions resolved = _settings.ResolveBuildOptions(baseOptions);

            Assert.That((resolved & BuildOptions.Development) != 0, Is.True);
            Assert.That((resolved & baseOptions), Is.EqualTo(baseOptions));
        }
    }
}

#endif
