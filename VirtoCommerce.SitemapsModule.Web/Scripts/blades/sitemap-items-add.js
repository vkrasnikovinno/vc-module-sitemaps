﻿angular.module('virtoCommerce.sitemapsModule')
.controller('virtoCommerce.sitemapsModule.sitemapItemsAddController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.sitemapsModule.sitemapApi', function ($scope, bladeNavigationService, sitemapApi) {
    var blade = $scope.blade;
    blade.isLoading = false;

    $scope.addCatalogItems = function () {
        var selectedItems = [];
        var newBlade = {
            id: 'addSitemapCatalogItems',
            title: 'sitemapsModule.blades.addCatalogItems.title',
            controller: 'virtoCommerce.catalogModule.catalogItemSelectController',
            template: 'Modules/$(VirtoCommerce.Catalog)/Scripts/blades/common/catalog-items-select.tpl.html',
            breadcrumbs: [],
            toolbarCommands: [{
                name: 'sitemapsModule.blades.addCatalogItems.toolbar.addSelected', icon: 'fa fa-plus',
                canExecuteMethod: function () {
                    return selectedItems.length > 0;
                },
                executeMethod: function (catalogBlade) {
                    var sitemapItems = _.map(selectedItems, itemToSitemapItem);
                    saveNewSitemapItems(sitemapItems, catalogBlade);
                }
            }],
            options: {
                allowCheckingCategory: true,
                checkItemFn: function (listItem, isSelected) {
                    selectedItems = checkSelectedItem(selectedItems, listItem, isSelected);
                }
            }
        }
        bladeNavigationService.showBlade(newBlade, blade.parentBlade);
    }

    $scope.addVendorItems = function () {
        var selectedItems = [];
        var newBlade = {
            id: 'addSitemapVendorItems',
            title: 'sitemapsModule.blades.addVendorItems.title',
            controller: 'virtoCommerce.customerModule.memberItemSelectController',
            template: 'Modules/$(VirtoCommerce.Sitemaps)/Scripts/blades/member-items-select.tpl.html',
            breadcrumbs: [],
            toolbarCommands: [{
                name: 'sitemapsModule.blades.addVendorItems.toolbar.addSelected',
                icon: 'fa fa-plus',
                canExecuteMethod: function () {
                    return selectedItems.length > 0;
                },
                executeMethod: function (vendorsBlade) {
                    var sitemapItems = _.map(selectedItems, itemToSitemapItem);
                    saveNewSitemapItems(sitemapItems, vendorsBlade);
                }
            }],
            options: {
                memberTypes: ['vendor'],
                checkItemFn: function (listItem, isSelected) {
                    selectedItems = checkSelectedItem(selectedItems, listItem, isSelected);
                }
            }
        }
        bladeNavigationService.showBlade(newBlade, blade.parentBlade);
    }

    $scope.addCustomItem = function () {
        var addCustomItemBlade = {
            id: 'addCustomItemBlade',
            currentEntity: {},
            confirmChangesFn: saveNewSitemapItems,
            title: 'sitemapsModule.blades.addCustomItem.title',
            controller: 'virtoCommerce.sitemapsModule.sitemapItemsAddCustomItemController',
            template: 'Modules/$(VirtoCommerce.Sitemaps)/Scripts/blades/sitemap-add-custom-item.tpl.html'
        }
        bladeNavigationService.closeBlade(blade, function () {
            bladeNavigationService.showBlade(addCustomItemBlade, blade.parentBlade);
        });
    }

    function checkSelectedItem(selectedItems, listItem, isSelected) {
        if (isSelected) {
            if (_.all(selectedItems, function (x) { return x.id != listItem.id; })) {
                selectedItems.push(listItem);
            }
        } else {
            selectedItems = _.reject(selectedItems, function (x) { return x.id == listItem.id; });
        }
        blade.error = undefined;
        return selectedItems;
    }

    function saveNewSitemapItems(sitemapItems, currentBlade) {
        currentBlade.isLoading = true;

        sitemapApi.addSitemapItems({ sitemapId: blade.sitemap.id },
            sitemapItems,
            function () {
                bladeNavigationService.closeBlade(currentBlade, blade.parentRefresh);
            });
    }

    function itemToSitemapItem(item) {
        return {
            title: item.name,
            imageUrl: item.imageUrl,
            objectId: item.id,
            objectType: item.type || item.seoObjectType
        };
    }
}]);