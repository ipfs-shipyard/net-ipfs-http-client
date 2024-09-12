// See https://aka.ms/new-console-template for more information
using Ipfs.Http;
using OwlCore.Diagnostics;
using OwlCore.Kubo;
using OwlCore.Storage.System.IO;



var res = await BootstrapKuboAsync(new SystemFolder("D:\\Projects\\_repo"), 9000, 9001, CancellationToken.None);
//await res.Client.Config.SetAsync("--json Experimental.FilestoreEnabled","true");

var result = await res.Client.FilestoreApi.DupsAsync(CancellationToken.None);

Logger.MessageReceived += Logger_MessageReceived;

void Logger_MessageReceived(object? sender, LoggerMessageEventArgs e)
{

}


//await ipfsClient.FilestoreApi.DupsAsync(CancellationToken.None);

while (true) ;
async Task<KuboBootstrapper> BootstrapKuboAsync(SystemFolder kuboRepo, int apiPort, int gatewayPort, CancellationToken cancellationToken)
{
    var kubo = new KuboBootstrapper(kuboRepo.Path)
    {
        ApiUri = new Uri($"http://127.0.0.1:{apiPort}"),
        GatewayUri = new Uri($"http://127.0.0.1:{gatewayPort}"),
        RoutingMode = DhtRoutingMode.None,
        LaunchConflictMode = BootstrapLaunchConflictMode.Relaunch,
        ApiUriMode = ConfigMode.OverwriteExisting,
        GatewayUriMode = ConfigMode.OverwriteExisting,
        BinaryWorkingFolder = new SystemFolder("D:\\ipfsBinary"),

    };

    await kubo.StartAsync(cancellationToken);

    return kubo;
}