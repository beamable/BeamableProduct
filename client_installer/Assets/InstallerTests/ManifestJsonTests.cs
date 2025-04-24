using Beamable.Installer.Editor;
using NUnit.Framework;

namespace Beamabler.Installer.Tests
{
    public class ManifestJsonTests
    {
        [Test]
        public void AddScopedRegistries_WhenOnlyHaveEmptyDeps()
        {
            var manifest = @"{""dependencies"":{}}";

            var output = BeamableInstaller.EnsureScopedRegistryJson(manifest);
            var expected =
                "{\"dependencies\":{},\"scopedRegistries\":[{\"name\":\"Beamable\",\"url\":\"https://nexus.beamable.com/nexus/content/repositories/unity\",\"scopes\":[\"com.beamable\"]}]}";

            Assert.AreEqual(expected, output);
        }

        [Test]
        public void AddScopedRegistries_WhenOnlyHaveDeps()
        {
            var manifest = @"{""dependencies"":{""a"":""b""}}";

            var output = BeamableInstaller.EnsureScopedRegistryJson(manifest);
            var expected =
                "{\"dependencies\":{\"a\":\"b\"},\"scopedRegistries\":[{\"name\":\"Beamable\",\"url\":\"https://nexus.beamable.com/nexus/content/repositories/unity\",\"scopes\":[\"com.beamable\"]}]}";

            Assert.AreEqual(expected, output);
        }

        [Test]
        public void AddScopedRegistries_WhenDeps_EmptyExistingScopes()
        {
            var manifest = @"{""dependencies"":{""a"":""b""}, ""scopedRegistries"": []}";

            var output = BeamableInstaller.EnsureScopedRegistryJson(manifest);
            var expected =
                "{\"dependencies\":{\"a\":\"b\"},\"scopedRegistries\":[{\"name\":\"Beamable\",\"url\":\"https://nexus.beamable.com/nexus/content/repositories/unity\",\"scopes\":[\"com.beamable\"]}]}";

            Assert.AreEqual(expected, output);
        }

        [Test]
        public void AddScopedRegistries_WhenDeps_Overwrite()
        {
            var manifest = @"{""dependencies"":{""a"":""b""}, ""scopedRegistries"": [{""name"": ""com.beamable""}]}";

            var output = BeamableInstaller.EnsureScopedRegistryJson(manifest, BeamableInstaller.BeamableRegistryDict_UnityDev);
            var expected =
                "{\"dependencies\":{\"a\":\"b\"},\"scopedRegistries\":[{\"name\":\"com.beamable\"},{\"name\":\"Beamable\",\"url\":\"https://nexus.beamable.com/nexus/content/repositories/unity-dev\",\"scopes\":[\"com.beamable\"]}]}";

            Assert.AreEqual(expected, output);
        }

        [Test]
        public void AddScopedRegistries_WhenDeps_NonEmptyExistingScopes()
        {
            var manifest = @"{""dependencies"":{""a"":""b""}, ""scopedRegistries"": [{""name"": ""tuna""}]}";

            var output = BeamableInstaller.EnsureScopedRegistryJson(manifest);
            var expected =
                "{\"dependencies\":{\"a\":\"b\"},\"scopedRegistries\":[{\"name\":\"tuna\"},{\"name\":\"Beamable\",\"url\":\"https://nexus.beamable.com/nexus/content/repositories/unity\",\"scopes\":[\"com.beamable\"]}]}";

            Assert.AreEqual(expected, output);
        }
    }
}
