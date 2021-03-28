angular.module("umbraco.resources").factory("ForumResource", function ($http) {
    return {
        memberTypes: function (groupName,maxTags) {
            return $http.get("/Umbraco/Api/ForumsApi/MemberTypes");
        },
        publishContentPages: function() {
            return $.get("/Umbraco/Api/ForumsApi/PublishedContentPages");
        },
        //MembershipInstalled
        membershipInstalled: function() {
            return $.get("/Umbraco/Api/ForumsApi/MembershipInstalled");
        },
        publishMembershipPages: function() {
            return $.get("/Umbraco/Api/ForumsApi/PublishedMembershipPages");
        },
        getContentPages: function() {
            return $.get("/Umbraco/Api/ForumsApi/GetAllContentPages");
        },
        createMembershipPages: function(rootid) {
            return $.get(`/Umbraco/Api/ForumsApi/CreateMembershipContent/?rootId=${rootid}`); //CreateRootContent(int rootId)
        },
        createForumPages: function(rootid) {
            return $.get(`/Umbraco/Api/ForumsApi/CreateForumContent/?rootId=${rootid}`); //CreateRootContent(int rootId)
        }

    };
});