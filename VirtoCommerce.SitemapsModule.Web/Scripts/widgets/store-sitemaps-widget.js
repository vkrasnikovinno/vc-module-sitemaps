﻿angular.module('virtoCommerce.sitemapsModule')
.controller('virtoCommerce.sitemapsModule.storeSitemapsWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
    var blade = $scope.blade;

    $scope.openBlade = function () {
        var newBlade = {
            id: 'storeSitemapsList',
            title: 'sitemapsModule.blades.sitemapList.title',
            controller: 'virtoCommerce.sitemapsModule.sitemapListController',
            template: 'Modules/$(VirtoCommerce.Sitemaps)/Scripts/blades/sitemap-list.tpl.html'
        };
        bladeNavigationService.showBlade(newBlade, blade);
    }
}]);