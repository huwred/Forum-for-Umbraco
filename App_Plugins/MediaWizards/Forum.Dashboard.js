(function () {

    "use strict";

    function forumDashboardController($scope,$sce,ForumResource,editorService,navigationService,contentResource) {

        var vm = this;
        vm.loaded = false;
        vm.selectedPage = null;
        vm.contentPublished = false;
        vm.memberContentPublished = false;
        vm.memberProperties = "";
        vm.ddlList = {};
        vm.pickRedirectPage = pickRedirectPage;
        vm.createMembershipContent = createMembershipContent;
        vm.createForumContent = createForumContent;
        vm.publishedProperites = "";
        vm.installing = false;
        vm.membershipInstalled = membershipInstalled;
        vm.membership = false;
        var currNode = null;
        init();

        ///////////////////////////
        $scope.addProperties = function () {
            vm.loading = true;
            ForumResource.memberTypes().then(function (response) {
                vm.memberProperties = response;
                vm.loading = false;
            });

        };

        // ## Open editorService contentpicker
        function pickRedirectPage() {
            navigationService.allowHideDialog(false);
            editorService.contentPicker({
                submit: function (model) {
                    // Get the first selected content node
                    setContent(model.selection[0]);
                    editorService.close();
                    navigationService.allowHideDialog(true);
                },
                close: function () {
                    editorService.close();
                    navigationService.allowHideDialog(true);
                }
            });
        }

        // ## Set Content
        function setContent(data) {
            var node = data;
            // Get node data
            contentResource.getById(data.id).then(function (data) {
                currNode = data;
                getUrl(node);
                vm.node = node;
            });
        }
        function init() {
            vm.loading = true;
            membershipInstalled();

            ForumResource.memberTypes().then(function (response) {
                vm.memberProperties = $sce.trustAsHtml(response.data);
                vm.loading = false;
            });
            ForumResource.getContentPages().then(function (response) {
                vm.ddlList = response;
            });
            ForumResource.publishContentPages().then(function (response) {
                vm.contentPublished = response;
            });
            ForumResource.publishMembershipPages().then(function (response) {
                vm.memberContentPublished = response;
            });
            vm.loading = false;
            vm.loaded = true;
        }
        // ## Get the url based on variant culture
        function getUrl(node) {
            if (currNode.urls.length > 0) {
                var currUrl = null;
                currUrl = _.find(currNode.urls, function (u) {
                    if (!u.culture) return false;

                    return true; //u.culture.toLowerCase() === vm.selectedLanguage.culture.toLowerCase();
                });
                node.url = currUrl;

                if (node.metaData.IsPublished)
                    node.status = node.url.text;
            }
        }
        function membershipInstalled() {
            ForumResource.membershipInstalled().then(function (response) {
                console.log(response);
                vm.membership = response;
                vm.loading = false;
            });
        }
        function createMembershipContent() {
            if (!vm.membership) {
                return;
            }
            vm.installing = true;
            vm.memberContentPublished = false;
            if (typeof vm.node !== "undefined") {
                vm.loading = true;

                ForumResource.createMembershipPages(vm.node.id).then(function (response) {
                    vm.publishedProperites = $sce.trustAsHtml(response);
                    vm.memberContentPublished = true;
                    vm.loading = false;
                });
            } else {
                ForumResource.createMembershipPages(-1).then(function (response) {
                    vm.publishedProperites = $sce.trustAsHtml(response);
                    vm.memberContentPublished = true;
                    vm.loading = false;
                });
            }
            
        }
        function createForumContent() {
            vm.installing = true;
            vm.contentPublished = false;
            if (typeof vm.node !== "undefined") {
                vm.loading = true;
                ForumResource.createForumPages(vm.node.id).then(function (response) {
                    var obj = JSON.parse(response);
                    vm.publishedProperites = $sce.trustAsHtml(obj.content);
                    vm.contentPublished = true;
                    vm.loading = false;
                    if (vm.membership) {
                        vm.loading = true;
                        ForumResource.createMembershipPages(obj.root).then(function (data) {
                            vm.publishedProperites += $sce.trustAsHtml(data);
                            vm.memberContentPublished = true;
                            vm.loading = false;
                        });
                    }

                });
                
            } else {
                vm.loading = true;
                ForumResource.createForumPages(-1).then(function (response) {
                    var obj = JSON.parse(response);
                    vm.publishedProperites = $sce.trustAsHtml(obj.content);
                    vm.contentPublished = true;
                    vm.loading = false;
                    if (vm.membership) {
                        vm.loading = true;
                        ForumResource.createMembershipPages(obj.root).then(function (data) {
                            vm.publishedProperites += $sce.trustAsHtml(data);
                            vm.memberContentPublished = true;
                            vm.loading = false;
                        });
                    }

                });
                
            }
            
        }
    }

    angular.module("umbraco").controller("forumDashboardController", forumDashboardController);
})();