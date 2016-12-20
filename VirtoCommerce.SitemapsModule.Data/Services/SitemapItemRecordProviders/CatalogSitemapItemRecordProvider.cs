﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SitemapsModule.Core.Models;
using VirtoCommerce.SitemapsModule.Core.Services;

namespace VirtoCommerce.SitemapsModule.Data.Services.SitemapItemRecordProviders
{
    public class CatalogSitemapItemRecordProvider : ISitemapItemRecordProvider
    {
        public CatalogSitemapItemRecordProvider(
            ICategoryService categoryService,
            IItemService itemService,
            ICatalogSearchService catalogSearchService,
            ISitemapUrlBuilder sitemapUrlBuilder,
            ISitemapItemRecordBuilder sitemapItemRecordBuilder,
            ISettingsManager settingsManager)
        {
            CategoryService = categoryService;
            ItemService = itemService;
            CatalogSearchService = catalogSearchService;
            SitemapUrlBuilder = sitemapUrlBuilder;
            SitemapItemRecordBuilder = sitemapItemRecordBuilder;
            SettingsManager = settingsManager;
        }

        protected ICategoryService CategoryService { get; private set; }
        protected IItemService ItemService { get; private set; }
        protected ICatalogSearchService CatalogSearchService { get; private set; }
        protected ISitemapUrlBuilder SitemapUrlBuilder { get; private set; }
        protected ISitemapItemRecordBuilder SitemapItemRecordBuilder { get; private set; }
        protected ISettingsManager SettingsManager { get; private set; }

        public virtual ICollection<SitemapItemRecord> GetSitemapItemRecords(Store store, Sitemap sitemap)
        {
            var sitemapItemRecords = new List<SitemapItemRecord>();

            var searchBunchSize = SettingsManager.GetValue("Sitemap.SearchBunchSize", 1000);

            var categorySitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.Category));
            var categoryIds = categorySitemapItems.Select(si => si.ObjectId).ToArray();
            var categories = CategoryService.GetByIds(categoryIds, CategoryResponseGroup.WithSeo);
            foreach (var category in categories)
            {
                var categorySitemapItemRecords = SitemapItemRecordBuilder.CreateSitemapItemRecords(store, sitemap.UrlTemplate, SitemapItemTypes.Category, category);
                sitemapItemRecords.AddRange(categorySitemapItemRecords);
            }

            var productSitemapItems = sitemap.Items.Where(si => si.ObjectType.EqualsInvariant(SitemapItemTypes.Product));
            var productIds = productSitemapItems.Select(si => si.ObjectId).ToArray();
            var products = ItemService.GetByIds(productIds, ItemResponseGroup.Seo);
            foreach (var product in products)
            {
                var productSitemapItemRecords = SitemapItemRecordBuilder.CreateSitemapItemRecords(store, sitemap.UrlTemplate, SitemapItemTypes.Product, product);
                sitemapItemRecords.AddRange(productSitemapItemRecords);
            }

            if (categoryIds.Any())
            {
                var catalogSearchCriteria = new Domain.Catalog.Model.SearchCriteria
                {
                    CatalogId = store.Catalog,
                    CategoryIds = categoryIds,
                    ResponseGroup = SearchResponseGroup.WithCategories | SearchResponseGroup.WithProducts,
                    Skip = 0,
                    Take = searchBunchSize
                };
                var catalogSearchResult = CatalogSearchService.Search(catalogSearchCriteria);
                foreach (var category in catalogSearchResult.Categories)
                {
                    var categorySitemapItemRecords = SitemapItemRecordBuilder.CreateSitemapItemRecords(store, sitemap.UrlTemplate, SitemapItemTypes.Category, category);
                    sitemapItemRecords.AddRange(categorySitemapItemRecords);
                }

                var partsCount = catalogSearchResult.ProductsTotalCount / searchBunchSize + 1;
                var cbProducts = new ConcurrentBag<CatalogProduct>();
                Parallel.For(1, partsCount + 1, new ParallelOptions { MaxDegreeOfParallelism = 5 }, i =>
                {
                    foreach (var product in catalogSearchResult.Products)
                    {
                        cbProducts.Add(product);
                    }
                    if (partsCount > 1)
                    {
                        catalogSearchCriteria.Skip = searchBunchSize * i;
                        catalogSearchResult = CatalogSearchService.Search(catalogSearchCriteria);
                    }
                });
                foreach (var product in cbProducts)
                {
                    var productSitemapItemRecords = SitemapItemRecordBuilder.CreateSitemapItemRecords(store, sitemap.UrlTemplate, SitemapItemTypes.Product, product);
                    sitemapItemRecords.AddRange(productSitemapItemRecords);
                }
            }

            return sitemapItemRecords;
        }
    }
}