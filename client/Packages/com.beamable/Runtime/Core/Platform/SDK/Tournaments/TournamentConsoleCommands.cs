using UnityEngine;
using Beamable.Service;
using Beamable.ConsoleCommands;
using Beamable.Common.Api;
using UnityEngine.Scripting;
using System.Collections.Generic;

namespace Beamable.Api.Tournaments
{
    /// <summary>
    /// This type defines the %Tournament feature's console commands.
    /// 
    /// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
    /// 
    /// #### Related Links
    /// - See the <a target="_blank" href="https://docs.beamable.com/docs/tournaments-feature">Tournaments</a> feature documentation
    /// - See Beamable.Api.Tournaments.TournamentService script reference 
    ///
    /// ![img beamable-logo]
    ///
    /// </summary>
    [BeamableConsoleCommandProvider]
    public class TournamentConsoleCommands
    {
        private BeamableConsole Console => ServiceManager.Resolve<BeamableConsole>();

        [Preserve]
        public TournamentConsoleCommands()
        {
        }

        [BeamableConsoleCommand("TOURNAMENT-JOIN", "Join a tournament cycle by id.", "TOURNAMENT-JOIN <tournament-id>")]
        protected string JoinTournament(params string[] args)
        {
            if (args.Length == 1)
            {
                string tournamentId = args[0];
                var platform = ServiceManager.Resolve<PlatformService>();

                platform.Tournaments.JoinTournament(tournamentId).Then(response =>
                {
                    Debug.Log($"Tournament {response.tournamentId} Joined.");
                });

                return $"Joining tournament cycle {tournamentId}...";
            }
            else
            {
                return "Please provide a fully qualified tournament id (e.g. 'tournaments.sample.1')";
            }
        }

        [BeamableConsoleCommand("TOURNAMENT-END", "End a tournament cycle by id. (Admin Only)", "TOURNAMENT-END <tournament-id>")]
        protected string ChangeCycle(params string[] args)
        {
            if(args.Length == 1)
            {
                string tournamentId = args[0];
                var platform = ServiceManager.Resolve<PlatformService>();

                platform.Requester.Request<EmptyResponse>(
                    Method.PUT, $"/object/tournaments/{tournamentId}/internal/cycle", new TournamentEndCycleRequest(tournamentId)
                ).Then(response =>
                {
                    Debug.Log($"Tournament {tournamentId} Ended.");
                });

                return $"Ending tournament cycle {tournamentId}...";
            }
            else
            {
                return "Please provide a fully qualified tournament id (e.g. 'tournaments.sample.1')";
            }
        }
    }

    [System.Serializable]
    class TournamentEndCycleRequest
    {
        public string tournamentId;

        public TournamentEndCycleRequest(string tournamentId)
        {
            this.tournamentId = tournamentId;
        }
    }
}