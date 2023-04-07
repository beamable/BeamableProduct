
namespace Beamable.Editor.BeamCli.Commands
{
    using Beamable.Common;
    using Beamable.Common.BeamCli;
    
    public class GetArgs : Beamable.Common.BeamCli.IBeamCommandArgs
    {
        /// <summary></summary>
        public string uri;
        /// <summary>Custom header</summary>
        public string[] header;
        /// <summary>Relative path to request body</summary>
        public string body;
        /// <summary>Make request customer scoped instead of product only</summary>
        public bool customerScoped;
        /// <summary>Serializes the arguments for command line usage.</summary>
        public virtual string Serialize()
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            // Add the uri value to the list of args.
            genBeamCommandArgs.Add(this.uri);
            // If the header value was not default, then add it to the list of args.
            if ((this.header != default(string[])))
            {
                for (int i = 0; (i < this.header.Length); i = (i + 1))
                {
                    // The parameter allows multiple values
                    genBeamCommandArgs.Add(("--header=" + this.header[i]));
                }
            }
            // If the body value was not default, then add it to the list of args.
            if ((this.body != default(string)))
            {
                genBeamCommandArgs.Add(("--body=" + this.body));
            }
            // If the customerScoped value was not default, then add it to the list of args.
            if ((this.customerScoped != default(bool)))
            {
                genBeamCommandArgs.Add(("--customer-scoped=" + this.customerScoped));
            }
            string genBeamCommandStr = "";
            // Join all the args with spaces
            genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            return genBeamCommandStr;
        }
    }
    public partial class BeamCommands
    {
        public virtual Beamable.Common.BeamCli.IBeamCommand Get(GetArgs getArgs)
        {
            // Create a list of arguments for the command
            System.Collections.Generic.List<string> genBeamCommandArgs = new System.Collections.Generic.List<string>();
            genBeamCommandArgs.Add("beam");
            genBeamCommandArgs.Add(defaultBeamArgs.Serialize());
            genBeamCommandArgs.Add("get");
            genBeamCommandArgs.Add(getArgs.Serialize());
            // Create an instance of an IBeamCommand
            Beamable.Common.BeamCli.IBeamCommand command = this._factory.Create();
            // Join all the command paths and args into one string
            string genBeamCommandStr = string.Join(" ", genBeamCommandArgs);
            // Configure the command with the command string
            command.SetCommand(genBeamCommandStr);
            // Return the command!
            return command;
        }
    }
}
