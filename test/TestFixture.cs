using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OwlCore.Kubo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OwlCore.Storage.System.IO;
using OwlCore.Storage;
using System.Diagnostics;

namespace Ipfs.Http
{
    [TestClass]
    public class TestFixture
    {
        // Publicly accessible client and API URI for tests.
        public static IpfsClient Ipfs { get; private set; } = null!;
        public static Uri? ApiUri { get; private set; }
        public static KuboBootstrapper? Node;

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            try
            {
                OwlCore.Diagnostics.Logger.MessageReceived += (sender, args) => context.WriteLine(args.Message);

                // Ensure the test runner has provided a deployment directory to use for working folders.
                Assert.IsNotNull(context.DeploymentDirectory);

                // Create a working folder and start a fresh Kubo node with default bootstrap peers.
                var workingFolder = SafeCreateWorkingFolder(new SystemFolder(context.DeploymentDirectory), typeof(TestFixture).Namespace ?? "test").GetAwaiter().GetResult();

                // Use non-default ports to avoid conflicts with any locally running node.
                int apiPort = 11501;
                int gatewayPort = 18080;

                Node = CreateNodeAsync(workingFolder, "kubo-node", apiPort, gatewayPort).GetAwaiter().GetResult();
                ApiUri = Node.ApiUri;
                Ipfs = new IpfsClient(ApiUri.ToString());

                context?.WriteLine($"Connected to existing Kubo node: {ApiUri}");
            }
            catch (Exception ex)
            {
                context?.WriteLine($"Kubo bootstrapper failed to start: {ex}");
                throw;
            }
        }

        public static async Task<KuboBootstrapper> CreateNodeAsync(SystemFolder workingDirectory, string nodeRepoName, int apiPort, int gatewayPort)
        {
            var nodeRepo = (SystemFolder)await workingDirectory.CreateFolderAsync(nodeRepoName, overwrite: true);

            var node = new KuboBootstrapper(nodeRepo.Path)
            {
                ApiUri = new Uri($"http://127.0.0.1:{apiPort}"),
                GatewayUri = new Uri($"http://127.0.0.1:{gatewayPort}"),
                RoutingMode = DhtRoutingMode.AutoClient,
                LaunchConflictMode = BootstrapLaunchConflictMode.Relaunch,
                BinaryWorkingFolder = workingDirectory,
                EnableFilestore = true,
            };

            OwlCore.Diagnostics.Logger.LogInformation($"Starting node {nodeRepoName}\n");

            await node.StartAsync().ConfigureAwait(false);
            await node.Client.IdAsync().ConfigureAwait(false);

            Debug.Assert(node.Process != null);
            return node;
        }

        public static async Task<SystemFolder> SafeCreateWorkingFolder(SystemFolder rootFolder, string name)
        {
            var testTempRoot = (SystemFolder)await rootFolder.CreateFolderAsync(name, overwrite: false);
            await SetAllFileAttributesRecursive(testTempRoot, attributes => attributes & ~FileAttributes.ReadOnly).ConfigureAwait(false);

            // Delete and recreate the folder.
            return (SystemFolder)await rootFolder.CreateFolderAsync(name, overwrite: true).ConfigureAwait(false);
        }

        public static async Task SetAllFileAttributesRecursive(SystemFolder rootFolder, Func<FileAttributes, FileAttributes> transform)
        {
            await foreach (SystemFile file in rootFolder.GetFilesAsync())
                file.Info.Attributes = transform(file.Info.Attributes);

            await foreach (SystemFolder folder in rootFolder.GetFoldersAsync())
                await SetAllFileAttributesRecursive(folder, transform).ConfigureAwait(false);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            try
            {
                Node?.Dispose();
            }
            catch { }
        }
    }
}
