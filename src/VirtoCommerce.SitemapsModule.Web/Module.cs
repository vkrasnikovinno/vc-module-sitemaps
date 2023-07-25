using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.Extensions;
using VirtoCommerce.SitemapsModule.Core;
using VirtoCommerce.SitemapsModule.Core.Services;
using VirtoCommerce.SitemapsModule.Data.ExportImport;
using VirtoCommerce.SitemapsModule.Data.MySql;
using VirtoCommerce.SitemapsModule.Data.PostgreSql;
using VirtoCommerce.SitemapsModule.Data.Repositories;
using VirtoCommerce.SitemapsModule.Data.Services;
using VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders;
using VirtoCommerce.SitemapsModule.Data.SqlServer;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.Tools;

namespace VirtoCommerce.SitemapsModule.Web
{
    public class Module : IModule, IExportSupport, IImportSupport, IHasConfiguration
    {
        private IApplicationBuilder _appBuilder;

        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<SitemapDbContext>(options =>
            {
                var databaseProvider = Configuration.GetValue("DatabaseProvider", "SqlServer");
                var connectionString = Configuration.GetConnectionString(ModuleInfo.Id) ?? Configuration.GetConnectionString("VirtoCommerce");

                switch (databaseProvider)
                {
                    case "MySql":
                        options.UseMySqlDatabase(connectionString);
                        break;
                    case "PostgreSql":
                        options.UsePostgreSqlDatabase(connectionString);
                        break;
                    default:
                        options.UseSqlServerDatabase(connectionString);
                        break;
                }
            });

            serviceCollection.AddTransient<ISitemapRepository, SitemapRepository>();
            serviceCollection.AddTransient<Func<ISitemapRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetRequiredService<ISitemapRepository>());

            serviceCollection.AddTransient<ISitemapService, SitemapService>();
            serviceCollection.AddTransient<ISitemapItemService, SitemapItemService>();
            serviceCollection.AddTransient<ISitemapSearchService, SitemapSearchService>();
            serviceCollection.AddTransient<ISitemapItemSearchService, SitemapItemSearchService>();
            serviceCollection.AddTransient<IUrlBuilder, UrlBuilder>();
            serviceCollection.AddTransient<ISitemapUrlBuilder, SitemapUrlBuilder>();
            serviceCollection.AddTransient<ISitemapItemRecordProvider, CatalogSitemapItemRecordProvider>();
            serviceCollection.AddTransient<ISitemapItemRecordProvider, CustomSitemapItemRecordProvider>();
            serviceCollection.AddTransient<ISitemapItemRecordProvider, VendorSitemapItemRecordProvider>();
            serviceCollection.AddTransient<ISitemapItemRecordProvider, StaticContentSitemapItemRecordProvider>();
            serviceCollection.AddTransient<ISitemapXmlGenerator, SitemapXmlGenerator>();
            serviceCollection.AddTransient<SitemapExportImport>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            _appBuilder = appBuilder;

            using (var serviceScope = appBuilder.ApplicationServices.CreateScope())
            {
                var databaseProvider = Configuration.GetValue("DatabaseProvider", "SqlServer");
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<SitemapDbContext>();
                if (databaseProvider == "SqlServer")
                {
                    dbContext.Database.MigrateIfNotApplied(MigrationName.GetUpdateV2MigrationName(ModuleInfo.Id));
                }
                dbContext.Database.Migrate();
            }

            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);
            settingsRegistrar.RegisterSettingsForType(ModuleConstants.Settings.StoreLevelSettings, nameof(Store));

            var permissionsRegistrar = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
            permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "Sitemaps", ModuleConstants.Security.Permissions.AllPermissions);
        }

        public void Uninstall()
        {
            // Nothing to do here
        }

        public async Task ExportAsync(Stream outStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback,
            ICancellationToken cancellationToken)
        {
            await _appBuilder.ApplicationServices.GetRequiredService<SitemapExportImport>().DoExportAsync(outStream,
                progressCallback, cancellationToken);
        }

        public async Task ImportAsync(Stream inputStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback,
            ICancellationToken cancellationToken)
        {
            await _appBuilder.ApplicationServices.GetRequiredService<SitemapExportImport>().DoImportAsync(inputStream,
                progressCallback, cancellationToken);
        }
    }
}
