namespace Beamable.Common.Api
{
   public interface IHasBeamableRequester
   {
      IBeamableRequester Requester { get; }
   }
}