using ArchiveApi.Interfaces;
using ArchiveApi.Services;
namespace ArchiveApi
{
    public enum DefaultServices
    {
        ArchiveIs,
        ArchiveFo,
        ArchiveLi,
        ArchivePh,
        ArchiveVn,
        ArchiveMd,
        ArchiveToday,
        WebArchiveOrg
    }
    public class ArchiveService : IArchiveServiceFactory
    {
        readonly DefaultServices service;
        public ArchiveService(DefaultServices service = DefaultServices.ArchiveFo) => this.service = service;
        public ArchiveService(string service)
        {
            this.service = service.ToLower() switch
            {
                "web.archive.org" => DefaultServices.WebArchiveOrg,
                "archive.is" => DefaultServices.ArchiveIs,
                "archive.fo" => DefaultServices.ArchiveFo,
                "archive.li" => DefaultServices.ArchiveLi,
                "archive.vn" => DefaultServices.ArchiveVn,
                "archive.ph" => DefaultServices.ArchivePh,
                "archive.md" => DefaultServices.ArchiveMd,
                "archive.today" => DefaultServices.ArchiveToday,
                _ => DefaultServices.ArchiveFo,
            };
        }
        public override IArchiveService CreateNewService()
        {
            return service switch
            {
                DefaultServices.ArchiveIs => new ArchiveIsService(),
                DefaultServices.ArchiveFo => new ArchiveFoService(),
                DefaultServices.ArchiveLi => new ArchiveLiService(),
                DefaultServices.ArchivePh => new ArchivePhService(),
                DefaultServices.ArchiveVn => new ArchiveVnService(),
                DefaultServices.ArchiveMd => new ArchiveMdService(),
                DefaultServices.ArchiveToday => new ArchiveTodayService(),
                DefaultServices.WebArchiveOrg => new InternetArchiveService(),
                _ => new ArchiveFoService(),
            };
        }
    }
}
