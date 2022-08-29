# net-ipfs-http-client

[![Version](https://img.shields.io/nuget/v/Ipfs.Http.Client.svg)](https://www.nuget.org/packages/IpfsShipyard.Ipfs.Http.Client)

A .NET client library for managing IPFS using the [HTTP API](https://docs.ipfs.io/reference/api/http/). 

![](https://ipfs.io/ipfs/QmQJ68PFMDdAsgCZvA1UVzzn18asVcf7HVvCDgpjiSCAse)

## Features

- Targets .NET Standard 2.0
- Asynchronous I/O to an IPFS server
- Supports request cancellation
- Requests compressed responses
- Documentation website coming soon™️
- C# style access to the ipfs core interface
  - Bitswap API
  - Block API
  - Config API
  - Dag API
  - Dht API
  - Misc API
  - FileSystem API
  - Key API
  - Name API
  - Object API
  - Pin API
  - PubSub API
  - Stats API
  - Swarm API

## Getting started

Published releases of IPFS API are available on [NuGet](https://www.nuget.org/packages/IpfsShipyard.Ipfs.Http.Client/).  To install, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console).

    PM> Install-Package IpfsShipyard.Ipfs.Http.Client
    
Or using [dotnet](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet)

    > dotnet add package IpfsShipyard.Ipfs.Http.Client

## IpfsClient

Every feature of IPFS is a property of the IpfsClient).  The following example 
uses `FileSystem` to read a text file

```csharp
using Ipfs.Http;

var ipfs = new IpfsClient();

const string filename = "QmXarR6rgkQ2fDSHjSY5nM2kuCXKYGViky5nohtwgF65Ec/about";
string text = await ipfs.FileSystem.ReadAllTextAsync(filename);
```

# License
The IPFS API library is licensed under the [MIT](http://www.opensource.org/licenses/mit-license.php "Read more about the MIT license form") license. Refer to the [LICENSE](https://github.com/ipfs-shipyard/net-ipfs-http-client/blob/master/LICENSE) file for more information.
