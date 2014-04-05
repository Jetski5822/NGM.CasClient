using JetBrains.Annotations;
using NGM.CasClient.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Localization;

namespace NGM.CasClient.Handlers {
    [UsedImplicitly]
    public class CASSettingsPartHandler : ContentHandler {
        public CASSettingsPartHandler(IRepository<CASSettingsPartRecord> repository) {
            T = NullLocalizer.Instance;
            Filters.Add(new ActivatingFilter<CASSettingsPart>("Site"));
            Filters.Add(StorageFilter.For(repository));
            Filters.Add(new TemplateFilterForRecord<CASSettingsPartRecord>("CASClientSettings", "Parts/CASClient.CASSettings", "CAS Client"));
        }

        public Localizer T { get; set; }

        protected override void GetItemMetadata(GetContentItemMetadataContext context) {
            if (context.ContentItem.ContentType != "Site")
                return;
            base.GetItemMetadata(context);
            context.Metadata.EditorGroupInfo.Add(new GroupInfo(T("CAS Client")));
        }
    }
}