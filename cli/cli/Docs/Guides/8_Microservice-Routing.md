Calling a function on a Microservice
## Dependencies

Before you can configure Beamable Standalone Microservices, you need to complete the [Getting-Started Guide](doc:cli-guide-getting-started). That means having [Dotnet 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed, and getting the  [Beam CLI](https://www.nuget.org/packages/Beamable.Tools). 

You can confirm you have everything installed checking the versions of the tools.
```sh
dotnet --version
beam version # beam --version also works.
```

In order to configure a Microservice, you also need to have a local `.beamable` workspace with a Beamable Standalone Microservice. As a reminder, you can create one quickly using the commands below.
```sh
beam init MyProject
cd MyProject
beam project new service HelloWorld
```

## Microservice Routing


Beamable Microservices use a privileged web socket to communicate with Beamable's existing APIs and services. There is a custom Beamable application level protocol, _Thorium_, that allows a Microservice to receive HTTPS traffic sent to `https://api.beamable.com`. However, for the Microservice to receive the traffic, the HTTP request needs to meet the following requirements, 

1. The `uri` of the HTTP request must have a `path` that maps to the desired Microservice. 
2. The HTTP request must have an `X-DE-SCOPE` header, and 
3. _Optionally_, the HTTP request should have an `Authorization` header that carries a Bearer Token. 

#### Path Routing

The `uri` of the HTTP request destined for a Microservice must follow a strict format. The format is given by the code snippet below.

```javascript
var uri = 'https://' + host + '/basic/' + cid + '.' + pid + '.' + localPrefix + 'micro_' + serviceName + '/' + method;
```

The variables are described in the following table.

| Variable      | Required | Example               | Description                                                                                                                                                                                                                                                                                |
| ------------- | -------- | --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `host`        | yes      | `api.beamable.com`    | In most cases, the host is always `api.beamable.com`. However, if you have a Beamable Private Cloud arrangement, this may vary.                                                                                                                                                            |
| `cid`         | yes      | `1338004997867618`    | The Customer Id, or CID, is the ID that defines your organization apart from other Beamable developers.                                                                                                                                                                                    |
| `pid`         | yes      | `DE_1752011665993728` | The Project Id, or PID, is the ID that defines your Realm.                                                                                                                                                                                                                                 |
| `localPrefix` | no       | `Macbook-Pro`         | A Microservice may run locally on a developer's machine, and it may run remotely deployed to the Beamable Cloud. In this scenario, the two instances should not receive traffic from the same origin. Thus, a prefix is injected into the routing to distinguish between the two services. |
| `serviceName` | yes      | `HelloWorld`          | The name of your service. This maps to the Beam Id of your service.                                                                                                                                                                                                                        |
| `method`      | yes      | `Add`                 | The name of the Method you want to invoke.                                                                                                                                                                                                                                                 |

When the `host` (`api.beamable.com`) receives an HTTP request with the following `uri` format, the request will be deconstructed into the variable components, and the contents of the HTTP request will be forwarded to your Microservice via the _Thorium_ web socket protocol. The Microservice receives the request, and uses the `method` component of the original request to invoke the right `[Callable]` method.  

#### X-DE-SCOPE Header

In addition to specifying the `cid` and `pid` in the `uri` of the HTTP request, those values must also be sent in a special HTTP header, `X-DE-SCOPE`. The value for this header should take the format, 

```javascript
var scope = cid + '.' + pid;
```
#### Authorization 

Finally, while not required, it is important to send an HTTP authorization header in the form of a Bearer token. The bearer token should be a valid access token for a Beamable Player. These tokens can be fetched from the Portal, or you can use the following command to view the token information from a local beamable CLI project. 

```sh
cat .beamable/temp/connection-auth.json
```


Beamable does not require that any access token be provided via an authorization header. However, if an authorization header is provided, then Beamable will decorate the request with account information before the request is forwarded to the Microservice. The Microservice itself may enforce the account details to have minimal access permissions. These constraints vary based on the type of `[Callable]` attribute you use. 

The account information is accessible via the `Context.UserId` property when executing the `[Callable]` method. 

| Attribute             | Authorization Requirements                                                                                   |
| --------------------- | ------------------------------------------------------------------------------------------------------------ |
| `[Callable]`          | None. This method may be invoked when no Authorization is provided. `Context.UserId` will yield 0.           |
| `[ClientCallable]`    | A valid player access token is required, and the `Context.UserId` will yield the calling player's player Id. |
| `[AdminOnlyCallable]` | An access token for a player with the admin role.                                                            |
| `[ServerCallable]`    | This may be used by requests authenticated with signed requests. However, no player is present.              |

## Client Generation

write docs to file

```sh
beam project oapi --ids HelloWorld | jq '.data.openApi | fromjson' > doc.json
```

Automatically build docs
```xml
<Target Name="Build Local OpenAPI File" AfterTargets="Build">  
    <Exec Command="$(BeamableTool) project oapi --ids HelloWorld | jq '.data.openApi | fromjson' > $(SolutionDir)local/doc.json"/>  
</Target>
```

generate a javascript SDK

```sh
docker run --rm -v "${PWD}:/local" openapitools/openapi-generator-cli generate \
	-i /local/doc.json \
	-g javascript \
	--additional-properties=usePromises=true \
	-o /local/out/js
```


use browserify
```sh
npm install -g browserify
browserify ./dist/index.js --standalone helloWorld > ../../ap
p/bundle.js 
```



client-src

```html
<html>

<head>

<title>Test</title>

</head>

<body>

<div> Hello </div>

  

<script src="bundle.js"></script>

  

<script>

// This script will run after bundle.js is loaded

document.addEventListener("DOMContentLoaded", function() {

var cid = '1338004997867618';
var pid = 'DE_1752011665993728';
var refreshToken = '0b199fd6-71a7-466b-9a13-f9c2e36fe5e1';
var localPrefix = 'Chriss-MacBook-Pro-2';
var serviceName = 'HelloWorld';

var host = 'https://api.beamable.com/basic/' + cid + '.' + pid + '.' + localPrefix + 'micro_' + serviceName;

console.log(host)

var client = new helloWorld.ApiClient(host);

client.authentications['scope'].apiKey = cid + '.' + pid;

client.authentications['user'].accessToken = refreshToken

console.log(client)

  

var api = new helloWorld.UncategorizedApi(client);

console.log(api)

  

var args = new helloWorld.AddRequestArgs();

args.a = 2;

args.b = 3;

  

console.log(args)

var opts = {

'addRequestArgs': args

};

api.addPost(opts).then(function(result) {

console.log('got result', result)

});

  

});

</script>

</body>

</html>
```


https://openapi-generator.tech/docs/generators/javascript/ 