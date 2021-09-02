using System;
using System.IO;
using System.Linq;
using Beamable.Editor.Environment;

namespace Beamable.Server.Editor.CodeGen
{
   public class DockerfileGenerator
   {
      public MicroserviceDescriptor Descriptor { get; }
      public MicroserviceConfigurationEntry Config { get; }

      private bool DebuggingEnabled = true;
      public const string DOTNET_RUNTIME_DEBUGGING_TOOLS_IMAGE = "mcr.microsoft.com/dotnet/runtime:5.0";
      public const string DOTNET_RUNTIME_IMAGE = "mcr.microsoft.com/dotnet/runtime:5.0-alpine";


      #if BEAMABLE_DEVELOPER
      public const string BASE_IMAGE = "beamservice"; // Use a locally built image.
      public string BASE_TAG = "latest"; // Use a locally built image.
#else
      public const string BASE_IMAGE = "beamableinc/beamservice"; // use the public online image.
      public string BASE_TAG = BeamableEnvironment.BeamServiceTag;
#endif

      public DockerfileGenerator(MicroserviceDescriptor descriptor, bool includeDebugTools)
      {
         DebuggingEnabled = includeDebugTools;
         Descriptor = descriptor;
         Config = MicroserviceConfiguration.Instance.GetEntry(descriptor.Name);
      }

      string GetOpenSshConfigString()
      {
         return $@"
Port 			             2222
ListenAddress 		       0.0.0.0
LoginGraceTime 		    180
Ciphers                  aes128-cbc,3des-cbc,aes256-cbc,aes128-ctr,aes192-ctr,aes256-ctr
MACs                     hmac-sha1,hmac-sha1-96
StrictModes 	       	 yes
SyslogFacility 	       DAEMON
PasswordAuthentication 	 yes
PermitEmptyPasswords 	 no
PermitRootLogin 	       yes
Subsystem sftp internal-sftp

";
      }

      string GetSupervisorDConfigString()
      {
         return $@"
[supervisord]
nodaemon=true
user=root
loglevel=error

[program:${Descriptor.Name}]
command=/usr/bin/dotnet {GetProgramDll()}
stdout_logfile=/dev/stdout
stdout_logfile_maxbytes=0

[program:ssh]
command=/usr/sbin/sshd -D
";
      }

      string WriteToFile(string multiline, string fileName)
      {
         return
            $@"RUN {string.Join(" && \\\n", multiline.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).Select(x => $"echo \"{x}\" >> {fileName}"))}";
      }

      string GetDebugLayer()
      {
         if (!DebuggingEnabled) return "";

         return $@"
#inject the debugging tools
RUN apt update && \
    apt install -y unzip curl supervisor openssh-server && \
    curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l /vsdbg

RUN mkdir -p /var/log/supervisor /run/sshd

{WriteToFile(GetSupervisorDConfigString(), "/etc/supervisor/conf.d/supervisord.conf")}
RUN rm -f /etc/ssh/sshd_config
{WriteToFile(GetOpenSshConfigString(), "/etc/ssh/sshd_config")}

RUN echo ""{Config.DebugData.Username}:{Config.DebugData.Password}"" | chpasswd

EXPOSE 80 2222
";
      }

      string GetProgramDll()
      {
         return $"/subapp/{Descriptor.ImageName}.dll";
      }

      string GetEntryPoint()
      {
         if (DebuggingEnabled)
         {
            return @"ENTRYPOINT [""/usr/bin/supervisord"", ""-c"", ""/etc/supervisor/conf.d/supervisord.conf""]";
         }
         else
         {
            return $@"ENTRYPOINT [""dotnet"", ""{GetProgramDll()}""]";
         }
      }

      string ReleaseMode()
      {
         return DebuggingEnabled ? "debug" : "release";
      }

      public string GetString()
      {
         var text = $@"
# step 1. Build...
FROM {BASE_IMAGE}:{BASE_TAG} AS build-env
WORKDIR /subsrc

COPY {Descriptor.ImageName}.csproj .

#RUN dotnet restore
COPY . .
RUN dotnet publish -c {ReleaseMode()} -o /subapp
RUN echo $BEAMABLE_SDK_VERSION > /subapp/.beamablesdkversion

# step 2. Package using the runtime
FROM {(DebuggingEnabled
            ? DOTNET_RUNTIME_DEBUGGING_TOOLS_IMAGE
            : DOTNET_RUNTIME_IMAGE)}
{GetDebugLayer()}

WORKDIR /subapp

COPY --from=build-env /subapp .
COPY --from=build-env /app/baseImageDocs.xml .
ENV BEAMABLE_SDK_VERSION_EXECUTION={BeamableEnvironment.SdkVersion}
{GetEntryPoint()}
";

         return text;
      }


      public void Generate(string filePath)
      {
         var content = GetString();

         Beamable.Common.BeamableLogger.Log("DOCKER FILE");
         Beamable.Common.BeamableLogger.Log(content);

         File.WriteAllText(filePath, content);
      }

   }
}