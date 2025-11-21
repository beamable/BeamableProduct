// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using System;
using System.Collections.Generic;

namespace Beamable.Common.BeamCli.Contracts
{
    [CliContractType]
    [Serializable]
    public class CollectorStatus
    {
        public bool isRunning; // If this is true, it means the collector is running, but not necessarily ready to receive data
        public bool isReady;
        public int pid;
        public string otlpEndpoint;
        public string version;

        public bool Equals(CollectorStatus otherStatus)
        {
            if (otherStatus.isReady != isReady)
            {
                return false;
            }

            if (otherStatus.isRunning != isRunning)
            {
                return false;
            }

            if (otherStatus.pid != pid)
            {
                return false;
            }

            return true;
        }
    }
    
    [CliContractType]
    [Serializable]
    public class OtelFileStatus
    {
	    public int FileCount;
	    public long FolderSize;
	    public long LastPublishTimestamp;
    }

}
