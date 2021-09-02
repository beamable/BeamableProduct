namespace Beamable.Common.Api.Mail
{
  public interface IMailApi : ISupportsGet<MailQueryResponse>
  {
    Promise<SearchMailResponse> SearchMail(SearchMailRequest request);
    Promise<ListMailResponse> GetMail(string category, long startId = 0, long limit = 100);
    Promise<EmptyResponse> SendMail(MailSendRequest request);
    Promise<EmptyResponse> Update(MailUpdateRequest updates);
  }
}