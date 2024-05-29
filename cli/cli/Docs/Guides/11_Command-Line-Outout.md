Passing output from the CLI to other processes

## Dependencies

Before you can use the Beamable CLI, you need to complete the [Getting-Started Guide](doc:cli-guide-getting-started). That means having [Dotnet 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed, and getting the  [Beam CLI](https://www.nuget.org/packages/Beamable.Tools). 

You can confirm you have everything installed checking the versions of the tools.
```sh
dotnet --version
beam version # beam --version also works.
```


## Logging

By default, the Beam CLI uses an informational level of logging. Usually, only the final output of a command will be printed to the console. However, for diagnostic purposes, you can change the log level of the CLI using the `--log` switch. 

The log levels are as follows, 

| Level     | Description                                                                    |
| --------- | ------------------------------------------------------------------------------ |
| `VERBOSE` | a diagnostic level of logs that includes pseudo developer commentary           |
| `DEBUG`   | a diagnostic level of logs that prints out additional context                  |
| `INFO`    | the default log level, where only the expected output of the command is logged |
| `WARNING` | ignores default output, and only prints out a warning of something went wrong  |
| `ERROR`   | ignores everything unless an error occurs                                      |
| `FATAL`   | ignores everything unless the process fatally crashes                          |

In order to set the log level, the full level name may be used (case insensitive), or simply the first letter of the level. For example, to use verbose logs, the following expression may be used. 

```sh
beam config --logs v
```


## Raw Output Mode

By default, the Beam CLI prints output in a variety of ways. Some commands will print JSON to the Standard Output Buffer (StdOut), and other commands will print conversational messages. However, all commands can be forced to print in a structured JSON format. 

The `--raw` flag will always print messages in JSON. The `--raw` message contains additional information than what may be printed for commands that already use JSON. For example, the `beam config` command prints JSON, 

```sh
beam config
 {                                                                 
    "host": "https://api.beamable.com",                            
    "cid": "123",                                     
    "pid": "DE_123",                                  
    "configPath": "/Users/Test/MyProject/.beamable" 
 } 
```

However, when the `--raw` flag is given, the output becomes, 

```sh
beam config --raw
{"ts":1716908373832,"type":"stream","data":{"host":"https://api.beamable.com","cid":"123","pid":"DE_123","configPath":"/Users/Test/MyProject/.beamable"}}
```

There are 3 top level parameters.

| Field  | Description                                                                                                                                                                                                                         |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ts`   | A UNIX timestamp in seconds, documenting when the output was logged.                                                                                                                                                                |
| `type` | A string declaring the type of output. Most commands will output one type of data to the `"stream"` type. However, some commands may output multiple schemas of data, and the `type` field may be used to distinguish between them. |
| `data` | Contains the actual JSON payload                                                                                                                                                                                                    |

## Piping

When a Beam CLI command is piped into another process, the `--raw` flag is force enabled. For example, if `beam config` is piped into a file, then the contents of the file will be the `--raw` output.

```sh
beam config > output.txt
cat output.txt 
{"ts":1716908709811,"type":"stream","data":{"host":"https://api.beamable.com","cid":"123","pid":"DE_123","configPath":"/Users/Test/MyProject/.beamable"}}
```

A common workflow is to use the [JQ](https://jqlang.github.io/jq/download/) tool to navigate the piped `--raw` output. Below are some common examples.

In order to pipe the `data` to a file, we could write, 
```sh
beam config | jq '.data' > output.txt
cat output.txt 
{
  "host": "https://api.beamable.com",
  "cid": "123",
  "pid": "DE_123",
  "configPath": "/Users/Test/MyProject/.beamable"
}
```

In order to only print the `cid` value to console, we could write,
```sh
beam config | jq '.data.cid'
"123"
```

Sometimes it is important to get the unescaped JSON, so the `fromjson` component of JQ can be used. For example, if we wanted the `cid` value, but without quotes, 

```sh
beam config | jq '.data.cid | fromjson'
123
```

## Piping and Logging

When the `--raw` flag is used, or a command is piped to a file, then the Standard Output Buffer will only receive the `--raw` output data. If the `--logs` flag is set, then logs will be sent to the Standard Error Buffer, such that will appear in the console. However, it can be tricky to emit the process logs to a file. 

In order to do so, the Standard Error Buffer must be piped to a file. For example, the following expression will put the process logs into a file. 

```sh
 beam config --logs v 2> test.txt
```

The contents of the `test.txt` file contain the process logs, not the `--raw` output.

```sh
cat test.txt 
Trying to get option=ConfigDirOption from Env Vars! Value Found=
Using standard unix docker uri=[unix:/var/run/docker.sock]
GET call: /basic/beamo/manifest/current
...
```

## Error Logging

The Beam CLI usually creates a temporary log file and emits verbose logs to the file for diagnostic purposes. This can be disabled if the environment variable, `BEAM_CLI_NO_FILE_LOG` is set to anything. 

When an error occurs, the output log should include a line similar to,

```
Logs at
  /var/folders/ys/949qmfy15r7bl8x36s6wmm000000gn/T/beamCliLog.txt
```
