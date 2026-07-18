using System.Data.SqlTypes;
using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class DirectChatTest {
    // Tests - Authentication Failures

    [Fact]
    public void OpenDirectEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/openDirect", new { AuthToken = "", Username = "someone" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void OpenDirectInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/openDirect", new { AuthToken = "not-a-real-token", Username = "someone" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Creation

    [Fact]
    public void FriendsOpenDirectCreatesAnActiveUnownedPrivateGroupWithBothMembers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.AddresseeAuthToken);

        JsonElement root = OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername);

        Assert.Equal("opened", root.GetProperty("status").GetString());
        Guid chatGroupId = Guid.Parse(root.GetProperty("chatGroupId").GetString());
        ChatGroup chatGroup = LoadGroup(chatGroupId);
        (Guid pairLowId, Guid pairHighId) = ChatGroupManager.ComputeDirectPair(requesterUserAccountId, addresseeUserAccountId);
        Assert.Equal("", chatGroup.Name);
        Assert.False(chatGroup.IsPublic);
        Assert.Null(chatGroup.OwnerUserAccountId);
        Assert.Equal(ChatGroupStatus.Active, chatGroup.Status);
        Assert.Equal(pairLowId, chatGroup.DirectPairLowId);
        Assert.Equal(pairHighId, chatGroup.DirectPairHighId);
        List<ChatGroupMember> memberRows = LoadMemberRows(chatGroupId);
        Assert.Equal(2, memberRows.Count);
        Assert.All(memberRows, member => Assert.Equal(ChatGroupMemberStatus.Active, member.Status));
        Assert.All(memberRows, member => Assert.Equal(ChatGroupMemberRole.Member, member.MemberRole));
        Assert.Contains(memberRows, member => member.UserAccountId == requesterUserAccountId);
        Assert.Contains(memberRows, member => member.UserAccountId == addresseeUserAccountId);
    }

    [Fact]
    public void RepeatOpenDirectReturnsTheSameGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);

        string firstChatGroupId = OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString();
        string secondChatGroupId = OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString();

        Assert.Equal(firstChatGroupId, secondChatGroupId);
        Assert.Equal(2, LoadMemberRows(Guid.Parse(firstChatGroupId)).Count);
    }

    [Fact]
    public void OpenDirectFromTheOtherSideReturnsTheSameGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);

        string requesterChatGroupId = OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString();
        string addresseeChatGroupId = OpenDirect(testingMockProvidersContainer, pair.AddresseeAuthToken, pair.RequesterUsername).GetProperty("chatGroupId").GetString();

        Assert.Equal(requesterChatGroupId, addresseeChatGroupId);
    }

    [Fact]
    public void OpenDirectResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);

        JsonElement root = OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["chatGroupId", "status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    [Fact]
    public void ConcurrentOpenDirectFromBothSidesCollapsesToOneGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        for (int trial = 0; trial < 5; trial++) {
            FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
            Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.RequesterAuthToken);
            Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.AddresseeAuthToken);
            string[] statuses = new string[2];
            string[] chatGroupIds = new string[2];

            List<Exception> exceptions = FriendshipTestActions.RunConcurrently(
                () => {
                    JsonElement root = OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername);
                    statuses[0] = root.GetProperty("status").GetString();
                    chatGroupIds[0] = root.GetProperty("chatGroupId").GetString();
                },
                () => {
                    JsonElement root = OpenDirect(testingMockProvidersContainer, pair.AddresseeAuthToken, pair.RequesterUsername);
                    statuses[1] = root.GetProperty("status").GetString();
                    chatGroupIds[1] = root.GetProperty("chatGroupId").GetString();
                });

            Assert.Empty(exceptions);
            Assert.Equal("opened", statuses[0]);
            Assert.Equal("opened", statuses[1]);
            Assert.Equal(chatGroupIds[0], chatGroupIds[1]);
            Assert.Equal(1, CountDirectGroups(requesterUserAccountId, addresseeUserAccountId));
            Assert.Equal(2, LoadMemberRows(Guid.Parse(chatGroupIds[0])).Count);
        }
    }

    [Fact]
    public void ComputeDirectPairUsesSqlServerGuidOrdering() {
        Guid dotNetFirst = Guid.Parse("00000000-0000-0000-0000-000000000002");
        Guid sqlServerFirst = Guid.Parse("10000000-0000-0000-0000-000000000001");

        (Guid pairLowId, Guid pairHighId) = ChatGroupManager.ComputeDirectPair(dotNetFirst, sqlServerFirst);
        (Guid swappedLowId, Guid swappedHighId) = ChatGroupManager.ComputeDirectPair(sqlServerFirst, dotNetFirst);

        Assert.True(dotNetFirst.CompareTo(sqlServerFirst) < 0);
        Assert.True(new SqlGuid(sqlServerFirst).CompareTo(new SqlGuid(dotNetFirst)) < 0);
        Assert.Equal(sqlServerFirst, pairLowId);
        Assert.Equal(dotNetFirst, pairHighId);
        Assert.Equal(pairLowId, swappedLowId);
        Assert.Equal(pairHighId, swappedHighId);
    }

    [Fact]
    public void DatabaseRejectsAReversedDirectPair() {
        Guid dotNetFirst = Guid.Parse("00000000-0000-0000-0000-000000000002");
        Guid sqlServerFirst = Guid.Parse("10000000-0000-0000-0000-000000000001");
        using (var cleanupContext = HappyPlaceDbContext.Create())
            cleanupContext.ChatGroups.Where(field => field.DirectPairLowId == sqlServerFirst || field.DirectPairLowId == dotNetFirst).ExecuteDelete();

        using (var orderedContext = HappyPlaceDbContext.Create()) {
            orderedContext.ChatGroups.Add(BuildDirectGroupRow(sqlServerFirst, dotNetFirst));
            orderedContext.SaveChanges();
        }
        using var reversedContext = HappyPlaceDbContext.Create();
        reversedContext.ChatGroups.Add(BuildDirectGroupRow(dotNetFirst, sqlServerFirst));

        Assert.Throws<DbUpdateException>(() => reversedContext.SaveChanges());
    }

    [Fact]
    public void DatabaseEnforcesOneDirectGroupPerPair() {
        (Guid pairLowId, Guid pairHighId) = ChatGroupManager.ComputeDirectPair(Guid.NewGuid(), Guid.NewGuid());

        using (var firstContext = HappyPlaceDbContext.Create()) {
            firstContext.ChatGroups.Add(BuildDirectGroupRow(pairLowId, pairHighId));
            firstContext.SaveChanges();
        }
        using var duplicateContext = HappyPlaceDbContext.Create();
        duplicateContext.ChatGroups.Add(BuildDirectGroupRow(pairLowId, pairHighId));

        Assert.Throws<DbUpdateException>(() => duplicateContext.SaveChanges());
    }

    // Tests - Refusals

    [Fact]
    public void StrangerOpenDirectReturnsNotFriendsWithoutCreatingAGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Caller");
        string targetAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Target");
        Guid callerUserAccountId = FriendshipTestActions.ResolveUserAccountId(callerAuthToken);
        Guid targetUserAccountId = FriendshipTestActions.ResolveUserAccountId(targetAuthToken);

        JsonElement root = OpenDirect(testingMockProvidersContainer, callerAuthToken, FriendshipTestActions.ResolveUsername(targetAuthToken));

        Assert.Equal("notFriends", root.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("chatGroupId").ValueKind);
        Assert.Equal(0, CountDirectGroups(callerUserAccountId, targetUserAccountId));
    }

    [Fact]
    public void PendingRequestOnlyReturnsNotFriends() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pendingPair = FriendshipTestActions.CreatePendingPair(testingMockProvidersContainer);

        JsonElement requesterRoot = OpenDirect(testingMockProvidersContainer, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername);
        JsonElement addresseeRoot = OpenDirect(testingMockProvidersContainer, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername);

        Assert.Equal("notFriends", requesterRoot.GetProperty("status").GetString());
        Assert.Equal("notFriends", addresseeRoot.GetProperty("status").GetString());
    }

    [Fact]
    public void BlockedPairReturnsNotFriendsInBothDirections() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        FriendshipTestActions.Block(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).EnsureSuccessStatusCode();

        JsonElement blockerRoot = OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername);
        JsonElement blockedRoot = OpenDirect(testingMockProvidersContainer, pair.AddresseeAuthToken, pair.RequesterUsername);

        Assert.Equal("notFriends", blockerRoot.GetProperty("status").GetString());
        Assert.Equal("notFriends", blockedRoot.GetProperty("status").GetString());
    }

    [Fact]
    public void SelfOpenDirectReturnsNotFriends() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Caller");

        JsonElement root = OpenDirect(testingMockProvidersContainer, callerAuthToken, FriendshipTestActions.ResolveUsername(callerAuthToken));

        Assert.Equal("notFriends", root.GetProperty("status").GetString());
    }

    [Fact]
    public void UnknownUsernameReturnsNotFriends() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Caller");

        JsonElement root = OpenDirect(testingMockProvidersContainer, callerAuthToken, "no-such-user-" + Guid.NewGuid());

        Assert.Equal("notFriends", root.GetProperty("status").GetString());
    }

    [Fact]
    public void AnonymousTargetReturnsNotFriends() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        FriendshipTestActions.MakeAnonymous(pair.AddresseeAuthToken);

        JsonElement root = OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername);

        Assert.Equal("notFriends", root.GetProperty("status").GetString());
    }

    [Fact]
    public void AnonymousCallerReturnsAccountRequired() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        FriendshipTestActions.MakeAnonymous(pair.RequesterAuthToken);

        JsonElement root = OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername);

        Assert.Equal("accountRequired", root.GetProperty("status").GetString());
    }

    // Tests - Feed Visibility

    [Fact]
    public void DirectGroupAppearsForBothMembersWithIsDirectAndPartnerContact() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.AddresseeAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        UserAccount addresseeAccount = LoadUserAccount(addresseeUserAccountId);

        JsonElement requesterRow = GetGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId);
        JsonElement addresseeRow = GetGroup(List(testingMockProvidersContainer, pair.AddresseeAuthToken), chatGroupId);

        Assert.True(requesterRow.GetProperty("isDirect").GetBoolean());
        Assert.Equal("", requesterRow.GetProperty("title").GetString());
        Assert.False(requesterRow.GetProperty("isPublic").GetBoolean());
        Assert.False(requesterRow.GetProperty("owner").GetBoolean());
        Assert.True(requesterRow.GetProperty("joined").GetBoolean());
        Assert.Equal(2, requesterRow.GetProperty("memberCount").GetInt32());
        Assert.Equal(0, requesterRow.GetProperty("unreadCount").GetInt32());
        JsonElement requesterContact = requesterRow.GetProperty("directContact");
        Assert.Equal(addresseeAccount.DisplayName, requesterContact.GetProperty("displayName").GetString());
        Assert.Equal(addresseeAccount.Username, requesterContact.GetProperty("username").GetString());
        Assert.Null(requesterContact.GetProperty("profilePhotoUrl").GetString());
        Assert.Equal(UserAccountRegistrar.GetAvatarColor(addresseeUserAccountId), requesterContact.GetProperty("avatarColor").GetString());
        Assert.Equal(addresseeAccount.DisplayName[..1].ToUpperInvariant(), requesterContact.GetProperty("initial").GetString());
        Assert.True(addresseeRow.GetProperty("isDirect").GetBoolean());
        Assert.Equal(pair.RequesterUsername, addresseeRow.GetProperty("directContact").GetProperty("username").GetString());
    }

    [Fact]
    public void RegularGroupRowsCarryIsDirectFalseAndNullDirectContact() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(FriendshipTestActions.ResolveUserAccountId(ownerAuthToken), "Regular Group", true);

        JsonElement row = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);

        Assert.False(row.GetProperty("isDirect").GetBoolean());
        Assert.Equal(JsonValueKind.Null, row.GetProperty("directContact").ValueKind);
    }

    [Fact]
    public void DirectGroupIsInvisibleToThirdPartiesAcrossEverySortPageAndSearch() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.AddresseeAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        string strangerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Stranger");
        string partnerDisplayName = LoadUserAccount(addresseeUserAccountId).DisplayName;
        string[] sortModes = [null, "Popular", "Latest", "Most Active", "Public", "Private", "Direct"];

        foreach (string sortMode in sortModes) {
            Assert.False(ContainsGroup(List(testingMockProvidersContainer, strangerAuthToken, sortMode, null), chatGroupId));
            Assert.DoesNotContain(chatGroupId.ToString(), WalkAllPages(testingMockProvidersContainer, strangerAuthToken, sortMode, null));
        }
        Assert.False(ContainsGroup(List(testingMockProvidersContainer, strangerAuthToken, null, partnerDisplayName), chatGroupId));
        Assert.DoesNotContain(chatGroupId.ToString(), WalkAllPages(testingMockProvidersContainer, strangerAuthToken, null, partnerDisplayName));
    }

    [Fact]
    public void PrivateFilterExcludesDirectGroupsForTheirOwnMembers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.RequesterAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        Guid privateGroupId = CreateActiveGroup(requesterUserAccountId, "My Private Group", false);

        JsonElement root = List(testingMockProvidersContainer, pair.RequesterAuthToken, "Private", null);

        Assert.False(ContainsGroup(root, chatGroupId));
        Assert.True(ContainsGroup(root, privateGroupId));
    }

    [Fact]
    public void DirectMessagesFilterReturnsOnlyDirectGroups() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.RequesterAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        Guid regularGroupId = CreateActiveGroup(requesterUserAccountId, "Regular Group", true);

        JsonElement root = List(testingMockProvidersContainer, pair.RequesterAuthToken, "Direct", null);

        Assert.True(ContainsGroup(root, chatGroupId));
        Assert.False(ContainsGroup(root, regularGroupId));
    }

    [Fact]
    public void SearchMatchesThePartnersNameAndUsername() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.AddresseeAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        string partnerDisplayName = LoadUserAccount(addresseeUserAccountId).DisplayName;

        Assert.True(ContainsGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken, null, partnerDisplayName), chatGroupId));
        Assert.True(ContainsGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken, null, pair.AddresseeUsername), chatGroupId));
        Assert.False(ContainsGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken, null, "zzz-no-match-" + Guid.NewGuid()), chatGroupId));
    }

    [Fact]
    public void UnreadCountCountsPartnerMessages() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        Send(testingMockProvidersContainer, pair.AddresseeAuthToken, chatGroupId, "first message");
        Send(testingMockProvidersContainer, pair.AddresseeAuthToken, chatGroupId, "second message");

        JsonElement row = GetGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId);

        Assert.Equal(2, row.GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public void BlockHidesTheDirectGroupAndUnblockAloneRestoresIt() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());

        FriendshipTestActions.Block(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).EnsureSuccessStatusCode();
        Assert.False(ContainsGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId));
        Assert.False(ContainsGroup(List(testingMockProvidersContainer, pair.AddresseeAuthToken), chatGroupId));

        FriendshipTestActions.Unblock(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).EnsureSuccessStatusCode();
        Assert.True(ContainsGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId));
        Assert.True(ContainsGroup(List(testingMockProvidersContainer, pair.AddresseeAuthToken), chatGroupId));
    }

    [Fact]
    public void UnfriendKeepsTheDirectGroupVisible() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());

        FriendshipTestActions.Unfriend(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).EnsureSuccessStatusCode();

        Assert.True(ContainsGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId));
        Assert.True(ContainsGroup(List(testingMockProvidersContainer, pair.AddresseeAuthToken), chatGroupId));
    }

    // Tests - Guards

    [Fact]
    public void RequestToJoinADirectGroupIsRefusedWithoutCreatingAPendingRow() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        string strangerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Stranger");
        Guid strangerUserAccountId = FriendshipTestActions.ResolveUserAccountId(strangerAuthToken);

        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/requestToJoin", new { AuthToken = strangerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        Assert.Equal(2, LoadMemberRows(chatGroupId).Count);
        Assert.DoesNotContain(LoadMemberRows(chatGroupId), member => member.UserAccountId == strangerUserAccountId);
        Assert.False(GetGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId).GetProperty("pendingMembers").GetBoolean());
    }

    [Fact]
    public void HelpOfferJoinCannotEnterADirectGroupAndNeverSeizesOwnership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.RequesterAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        string strangerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Stranger");
        Guid strangerUserAccountId = FriendshipTestActions.ResolveUserAccountId(strangerAuthToken);

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = strangerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = pair.RequesterAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        ChatGroup chatGroup = LoadGroup(chatGroupId);
        List<ChatGroupMember> memberRows = LoadMemberRows(chatGroupId);
        Assert.Null(chatGroup.OwnerUserAccountId);
        Assert.False(chatGroup.IsPublic);
        Assert.Equal(2, memberRows.Count);
        Assert.DoesNotContain(memberRows, member => member.UserAccountId == strangerUserAccountId);
        Assert.Equal(ChatGroupMemberRole.Member, memberRows.Single(member => member.UserAccountId == requesterUserAccountId).MemberRole);
    }

    [Fact]
    public void OwnerControlsAllRefuseOnADirectGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.AddresseeAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());

        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/rename", new { AuthToken = pair.RequesterAuthToken, ChatGroupId = chatGroupId, Name = "Hacked Name" }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/setVisibility", new { AuthToken = pair.RequesterAuthToken, ChatGroupId = chatGroupId, IsPublic = true }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/removeMember", new { AuthToken = pair.RequesterAuthToken, ChatGroupId = chatGroupId, MemberUserAccountId = addresseeUserAccountId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = pair.RequesterAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        ChatGroup chatGroup = LoadGroup(chatGroupId);
        List<ChatGroupMember> memberRows = LoadMemberRows(chatGroupId);
        Assert.Equal("", chatGroup.Name);
        Assert.False(chatGroup.IsPublic);
        Assert.Equal(ChatGroupStatus.Active, chatGroup.Status);
        Assert.Equal(2, memberRows.Count);
        Assert.Contains(memberRows, member => member.UserAccountId == addresseeUserAccountId && member.Status == ChatGroupMemberStatus.Active);
    }

    [Fact]
    public void LeaveIsRefusedOnADirectGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());

        string status = FriendshipTestActions.ReadStatus(testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = pair.RequesterAuthToken, ChatGroupId = chatGroupId }));

        Assert.Equal("notAllowed", status);
        List<ChatGroupMember> memberRows = LoadMemberRows(chatGroupId);
        Assert.Equal(2, memberRows.Count);
        Assert.All(memberRows, member => Assert.Equal(ChatGroupMemberStatus.Active, member.Status));
    }

    [Fact]
    public void ListMembersOnADirectGroupIsHiddenFromNonMembers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        string strangerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Stranger");

        JsonElement strangerRoot = ListMembers(testingMockProvidersContainer, strangerAuthToken, chatGroupId);
        JsonElement memberRoot = ListMembers(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId);

        Assert.Equal(0, strangerRoot.GetProperty("members").GetArrayLength());
        Assert.Equal(2, memberRoot.GetProperty("members").GetArrayLength());
    }

    // Tests - Messaging Gates

    [Fact]
    public void SendIsRefusedAfterUnfriend() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        FriendshipTestActions.Unfriend(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).EnsureSuccessStatusCode();

        JsonElement root = SendRaw(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, "hello after unfriend");

        Assert.Equal("notFriends", root.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("message").ValueKind);
        Assert.Equal(0, CountMessages(chatGroupId));
    }

    [Fact]
    public void SendIsRefusedWhileBlocked() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        FriendshipTestActions.Block(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).EnsureSuccessStatusCode();

        JsonElement blockerRoot = SendRaw(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, "from the blocker");
        JsonElement blockedRoot = SendRaw(testingMockProvidersContainer, pair.AddresseeAuthToken, chatGroupId, "from the blocked");

        Assert.Equal("notFriends", blockerRoot.GetProperty("status").GetString());
        Assert.Equal("notFriends", blockedRoot.GetProperty("status").GetString());
        Assert.Equal(0, CountMessages(chatGroupId));
    }

    [Fact]
    public void ReFriendingRestoresSending() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        FriendshipTestActions.Unfriend(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).EnsureSuccessStatusCode();
        Assert.Equal("notFriends", SendRaw(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, "while unfriended").GetProperty("status").GetString());

        FriendshipTestActions.MakeFriends(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeAuthToken);
        JsonElement root = SendRaw(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, "friends again");

        Assert.Equal("sent", root.GetProperty("status").GetString());
        Assert.Equal(1, CountMessages(chatGroupId));
    }

    [Fact]
    public void RetryOfACommittedMessageAfterUnfriendReturnsNotFriends() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        Guid clientMessageId = Guid.NewGuid();
        Assert.Equal("sent", SendRawWithClientMessageId(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, clientMessageId, "committed message").GetProperty("status").GetString());
        FriendshipTestActions.Unfriend(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).EnsureSuccessStatusCode();

        JsonElement retryRoot = SendRawWithClientMessageId(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, clientMessageId, "committed message");

        Assert.Equal("notFriends", retryRoot.GetProperty("status").GetString());
        Assert.Equal(1, CountMessages(chatGroupId));
    }

    [Fact]
    public void ReactIsRefusedAfterUnfriend() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        JsonElement sentRoot = SendRaw(testingMockProvidersContainer, pair.AddresseeAuthToken, chatGroupId, "react to me");
        Guid messageId = Guid.Parse(sentRoot.GetProperty("message").GetProperty("id").GetString());
        FriendshipTestActions.Unfriend(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).EnsureSuccessStatusCode();

        JsonElement root = React(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, messageId, "👍");

        Assert.Equal("notFriends", root.GetProperty("status").GetString());
        Assert.Equal(0, CountReactions(messageId));
    }

    [Fact]
    public void TypingIsRefusedAfterUnfriend() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        FriendshipTestActions.Unfriend(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).EnsureSuccessStatusCode();

        JsonElement root = Typing(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId);

        Assert.Equal("notFriends", root.GetProperty("status").GetString());
    }

    [Fact]
    public void RegularGroupsAreNeverGatedByFriendship() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(FriendshipTestActions.ResolveUserAccountId(ownerAuthToken), "Regular Group", true);
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(memberAuthToken));

        JsonElement sentRoot = SendRaw(testingMockProvidersContainer, memberAuthToken, groupId, "no friendship needed");
        Guid messageId = Guid.Parse(sentRoot.GetProperty("message").GetProperty("id").GetString());
        JsonElement reactRoot = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, "👍");
        JsonElement typingRoot = Typing(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.Equal("sent", sentRoot.GetProperty("status").GetString());
        Assert.Equal("reacted", reactRoot.GetProperty("status").GetString());
        Assert.Equal("ok", typingRoot.GetProperty("status").GetString());
    }

    [Fact]
    public void HistoryStaysReadableAfterUnfriend() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        Assert.Equal("sent", SendRaw(testingMockProvidersContainer, pair.AddresseeAuthToken, chatGroupId, "hello there").GetProperty("status").GetString());
        FriendshipTestActions.Unfriend(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).EnsureSuccessStatusCode();

        JsonElement root = Poll(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, 0);

        Assert.True(root.GetProperty("group").GetProperty("isDirect").GetBoolean());
        Assert.Equal(2, root.GetProperty("group").GetProperty("members").GetArrayLength());
        Assert.Equal(1, root.GetProperty("changes").GetArrayLength());
        Assert.Equal("hello there", root.GetProperty("changes")[0].GetProperty("body").GetString());
    }

    // Tests - Membership Preferences

    [Fact]
    public void HideEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/hide", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void SetMutedEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/setMuted", new { AuthToken = "", ChatGroupId = Guid.NewGuid(), IsMuted = true });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void UnreadTotalEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/unreadTotal", new { AuthToken = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void HideRemovesTheDirectGroupFromYourListOnly() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.RequesterAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());

        JsonElement root = Hide(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId);

        Assert.Equal("hidden", root.GetProperty("status").GetString());
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["status"];
        Assert.Equal(expectedProperties, actualProperties);
        Assert.NotNull(LoadMembership(chatGroupId, requesterUserAccountId).HiddenAtUtc);
        Assert.False(ContainsGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId));
        Assert.True(ContainsGroup(List(testingMockProvidersContainer, pair.AddresseeAuthToken), chatGroupId));
    }

    [Fact]
    public void HideIsRefusedOnRegularGroups() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = FriendshipTestActions.ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Regular Group", true);

        JsonElement root = Hide(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal("notAllowed", root.GetProperty("status").GetString());
        Assert.Null(LoadMembership(groupId, ownerUserAccountId).HiddenAtUtc);
    }

    [Fact]
    public void HideRequiresMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        string strangerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Stranger");

        JsonElement root = Hide(testingMockProvidersContainer, strangerAuthToken, chatGroupId);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void PartnerMessageUnhidesWithUnread() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.RequesterAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        Assert.Equal("hidden", Hide(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId).GetProperty("status").GetString());

        Send(testingMockProvidersContainer, pair.AddresseeAuthToken, chatGroupId, "resurfacing message");

        Assert.Null(LoadMembership(chatGroupId, requesterUserAccountId).HiddenAtUtc);
        JsonElement row = GetGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId);
        Assert.Equal(1, row.GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public void OpenDirectUnhidesForTheCaller() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.RequesterAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        Assert.Equal("hidden", Hide(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId).GetProperty("status").GetString());

        Assert.Equal("opened", OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("status").GetString());

        Assert.Null(LoadMembership(chatGroupId, requesterUserAccountId).HiddenAtUtc);
        Assert.True(ContainsGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId));
    }

    [Fact]
    public void HiddenDirectGroupIsExcludedFromEveryFeedSurface() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.AddresseeAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        string partnerDisplayName = LoadUserAccount(addresseeUserAccountId).DisplayName;
        Assert.Equal("hidden", Hide(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId).GetProperty("status").GetString());

        Assert.False(ContainsGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken, "Direct", null), chatGroupId));
        Assert.False(ContainsGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken, null, partnerDisplayName), chatGroupId));
        Assert.DoesNotContain(chatGroupId.ToString(), WalkAllPages(testingMockProvidersContainer, pair.RequesterAuthToken, null, null));
    }

    [Fact]
    public void MutedMemberGetsNoPushWhileUnmutedDoes() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.AddresseeAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        SeedDeviceToken(requesterUserAccountId, "requester-mute-device");
        SeedDeviceToken(addresseeUserAccountId, "addressee-mute-device");
        Assert.Equal("muted", SetMuted(testingMockProvidersContainer, pair.AddresseeAuthToken, chatGroupId, true).GetProperty("status").GetString());

        Send(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, "muted delivery");
        ForceMessageChannelsDue(chatGroupId);
        NotificationDispatchManager.Sweep();

        Assert.Empty(ChatPushes(testingMockProvidersContainer, chatGroupId));

        Assert.Equal("unmuted", SetMuted(testingMockProvidersContainer, pair.AddresseeAuthToken, chatGroupId, false).GetProperty("status").GetString());
        Send(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, "audible delivery");
        ForceMessageChannelsDue(chatGroupId);
        NotificationDispatchManager.Sweep();

        Assert.Contains(ChatPushes(testingMockProvidersContainer, chatGroupId), field => field.Token == "addressee-mute-device");
    }

    [Fact]
    public void MuteStillCountsUnread() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        Assert.Equal("muted", SetMuted(testingMockProvidersContainer, pair.AddresseeAuthToken, chatGroupId, true).GetProperty("status").GetString());

        Send(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, "first muted");
        Send(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, "second muted");

        JsonElement mutedRow = GetGroup(List(testingMockProvidersContainer, pair.AddresseeAuthToken), chatGroupId);
        Assert.Equal(2, mutedRow.GetProperty("unreadCount").GetInt32());
        Assert.True(mutedRow.GetProperty("isMuted").GetBoolean());
        Assert.False(GetGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId).GetProperty("isMuted").GetBoolean());
        Assert.Equal(2, UnreadTotal(testingMockProvidersContainer, pair.AddresseeAuthToken).GetProperty("total").GetInt32());
    }

    [Fact]
    public void MuteWorksOnRegularGroups() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = FriendshipTestActions.ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Mutable Group", true);
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(memberAuthToken));
        SeedDeviceToken(ownerUserAccountId, "owner-mute-device");
        Assert.Equal("muted", SetMuted(testingMockProvidersContainer, ownerAuthToken, groupId, true).GetProperty("status").GetString());

        Send(testingMockProvidersContainer, memberAuthToken, groupId, "quiet group message");
        ForceMessageChannelsDue(groupId);
        NotificationDispatchManager.Sweep();

        Assert.Empty(ChatPushes(testingMockProvidersContainer, groupId));
        Assert.Equal(1, GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId).GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public void SetMutedRequiresMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        string strangerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Stranger");

        JsonElement root = SetMuted(testingMockProvidersContainer, strangerAuthToken, chatGroupId, true);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    // Tests - Last Message Previews

    [Fact]
    public void RowCarriesLastMessagePreviewAndTimestamp() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());

        JsonElement freshRow = GetGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId);
        Assert.Equal(JsonValueKind.Null, freshRow.GetProperty("lastMessagePreview").ValueKind);
        Assert.Equal(JsonValueKind.Null, freshRow.GetProperty("lastMessageAtUtc").ValueKind);

        Send(testingMockProvidersContainer, pair.AddresseeAuthToken, chatGroupId, "hello preview");

        JsonElement row = GetGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId);
        Assert.Equal("hello preview", row.GetProperty("lastMessagePreview").GetString());
        Assert.False(string.IsNullOrEmpty(row.GetProperty("lastMessageAtUtc").GetString()));
    }

    [Fact]
    public void PreviewIsHiddenFromNonMembers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Owner");
        string strangerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(FriendshipTestActions.ResolveUserAccountId(ownerAuthToken), "Public Room", true);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "secret latest");

        JsonElement ownerRow = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);
        JsonElement strangerRow = GetGroup(List(testingMockProvidersContainer, strangerAuthToken), groupId);

        Assert.Equal("secret latest", ownerRow.GetProperty("lastMessagePreview").GetString());
        Assert.Equal(JsonValueKind.Null, strangerRow.GetProperty("lastMessagePreview").ValueKind);
        Assert.Equal(JsonValueKind.Null, strangerRow.GetProperty("lastMessageAtUtc").ValueKind);
    }

    [Fact]
    public void DeletedLatestMessagePreviewsAsMessageDeleted() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        JsonElement sentRoot = SendRaw(testingMockProvidersContainer, pair.AddresseeAuthToken, chatGroupId, "delete me");
        Guid messageId = Guid.Parse(sentRoot.GetProperty("message").GetProperty("id").GetString());

        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/deleteOwn", new { AuthToken = pair.AddresseeAuthToken, ChatGroupId = chatGroupId, MessageId = messageId }).EnsureSuccessStatusCode();

        JsonElement row = GetGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId);
        Assert.Equal("Message deleted", row.GetProperty("lastMessagePreview").GetString());
    }

    // Tests - Unread Total

    [Fact]
    public void UnreadTotalSumsAcrossGroupsAndDirects() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.RequesterAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        string senderAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Sender");
        Guid groupId = CreateActiveGroup(FriendshipTestActions.ResolveUserAccountId(senderAuthToken), "Busy Group", true);
        AddActiveMember(groupId, requesterUserAccountId);

        JsonElement emptyRoot = UnreadTotal(testingMockProvidersContainer, pair.RequesterAuthToken);
        Assert.Equal("ok", emptyRoot.GetProperty("status").GetString());
        Assert.Equal(0, emptyRoot.GetProperty("total").GetInt32());
        List<string> actualProperties = [.. emptyRoot.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["status", "total"];
        Assert.Equal(expectedProperties, actualProperties);

        Send(testingMockProvidersContainer, senderAuthToken, groupId, "group one");
        Send(testingMockProvidersContainer, senderAuthToken, groupId, "group two");
        Send(testingMockProvidersContainer, pair.AddresseeAuthToken, chatGroupId, "direct one");

        Assert.Equal(3, UnreadTotal(testingMockProvidersContainer, pair.RequesterAuthToken).GetProperty("total").GetInt32());

        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/markRead", new { AuthToken = pair.RequesterAuthToken, ChatGroupId = groupId, UpToSequence = 2 }).EnsureSuccessStatusCode();

        Assert.Equal(1, UnreadTotal(testingMockProvidersContainer, pair.RequesterAuthToken).GetProperty("total").GetInt32());
    }

    // Tests - Poll State

    [Fact]
    public void PollStateCarriesIsDirectAndThePartnersContact() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.AddresseeAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        UserAccount addresseeAccount = LoadUserAccount(addresseeUserAccountId);

        JsonElement requesterGroup = Poll(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, 0).GetProperty("group");
        JsonElement addresseeGroup = Poll(testingMockProvidersContainer, pair.AddresseeAuthToken, chatGroupId, 0).GetProperty("group");

        Assert.True(requesterGroup.GetProperty("isDirect").GetBoolean());
        Assert.Equal("", requesterGroup.GetProperty("title").GetString());
        Assert.Equal(addresseeAccount.DisplayName, requesterGroup.GetProperty("directContact").GetProperty("displayName").GetString());
        Assert.Equal(addresseeAccount.Username, requesterGroup.GetProperty("directContact").GetProperty("username").GetString());
        Assert.True(addresseeGroup.GetProperty("isDirect").GetBoolean());
        Assert.Equal(pair.RequesterUsername, addresseeGroup.GetProperty("directContact").GetProperty("username").GetString());
    }

    [Fact]
    public void RegularGroupPollStateHasIsDirectFalseAndNullContact() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(FriendshipTestActions.ResolveUserAccountId(ownerAuthToken), "Regular Group", true);

        JsonElement group = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("group");

        Assert.False(group.GetProperty("isDirect").GetBoolean());
        Assert.Equal(JsonValueKind.Null, group.GetProperty("directContact").ValueKind);
    }

    // Tests - Pushes

    [Fact]
    public void DirectMessagePushIsTitledWithThePartnersName() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.AddresseeAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());
        string requesterDisplayName = LoadUserAccount(requesterUserAccountId).DisplayName;
        string addresseeDisplayName = LoadUserAccount(addresseeUserAccountId).DisplayName;
        SeedDeviceToken(requesterUserAccountId, "requester-device-token");
        SeedDeviceToken(addresseeUserAccountId, "addressee-device-token");

        Send(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, "first direct message");
        ForceMessageChannelsDue(chatGroupId);
        NotificationDispatchManager.Sweep();

        PushMessage push = Assert.Single(ChatPushes(testingMockProvidersContainer, chatGroupId));
        Assert.Equal("addressee-device-token", push.Token);
        Assert.Equal(requesterDisplayName, push.Title);
        Assert.Equal("1 new message.", push.Body);
        Assert.Equal("chatMessages", push.Data["type"]);

        Send(testingMockProvidersContainer, pair.AddresseeAuthToken, chatGroupId, "reply direct message");
        ForceMessageChannelsDue(chatGroupId);
        NotificationDispatchManager.Sweep();

        Assert.Contains(ChatPushes(testingMockProvidersContainer, chatGroupId), field => field.Title == addresseeDisplayName && field.Token == "requester-device-token");
    }

    // Tests - Account Deletion

    [Fact]
    public void PartnerAccountDeletionSoftDeletesTheDirectGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pair.AddresseeAuthToken);
        Guid chatGroupId = Guid.Parse(OpenDirect(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).GetProperty("chatGroupId").GetString());

        ChatGroupManager.UntangleUserForAccountDeletion(addresseeUserAccountId);

        Assert.Equal(ChatGroupStatus.Deleted, LoadGroup(chatGroupId).Status);
        Assert.Empty(LoadMemberRows(chatGroupId));
        Assert.False(ContainsGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId));
        Assert.Equal("groupGone", Poll(testingMockProvidersContainer, pair.RequesterAuthToken, chatGroupId, 0).GetProperty("status").GetString());
    }

    // Helpers - Acting

    private static JsonElement OpenDirect(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, string username) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/openDirect", new { AuthToken = authToken, Username = username }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement List(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, string sortBy = null, string search = null) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/list", new { AuthToken = authToken, SortBy = sortBy, Search = search }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static List<string> WalkAllPages(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, string sortBy, string search) {
        List<string> collectedIds = [];
        string cursor = null;
        while (true) {
            JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/listPage", new { AuthToken = authToken, SortBy = sortBy, Search = search, Cursor = cursor }).ReadContentAsJsonDocument().RootElement.Clone();
            foreach (JsonElement item in root.GetProperty("items").EnumerateArray())
                collectedIds.Add(item.GetProperty("id").GetString());
            JsonElement nextCursor = root.GetProperty("nextCursor");
            if (nextCursor.ValueKind == JsonValueKind.Null)
                return collectedIds;
            cursor = nextCursor.GetString();
        }
    }

    private static JsonElement Hide(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/hide", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement SetMuted(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, bool isMuted) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/setMuted", new { AuthToken = authToken, ChatGroupId = chatGroupId, IsMuted = isMuted }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement UnreadTotal(TestingMockProvidersContainer testingMockProvidersContainer, string authToken) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/unreadTotal", new { AuthToken = authToken }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement ListMembers(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/listMembers", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement Poll(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, long sinceChangeSequence) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/poll", new { AuthToken = authToken, ChatGroupId = chatGroupId, SinceChangeSequence = sinceChangeSequence }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static void Send(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string body) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = Guid.NewGuid(), Body = body }).EnsureSuccessStatusCode();
    }

    private static JsonElement SendRaw(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string body) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = Guid.NewGuid(), Body = body }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement SendRawWithClientMessageId(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid clientMessageId, string body) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = clientMessageId, Body = body }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement React(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid messageId, string emoji) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/react", new { AuthToken = authToken, ChatGroupId = chatGroupId, MessageId = messageId, Emoji = emoji }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement Typing(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/typing", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static void ForceMessageChannelsDue(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        List<NotificationChannel> dirtyChannels = [.. dbContext.NotificationChannels
            .Where(field => field.Kind == NotificationChannelKind.Messages && field.ScopeChatGroupId == groupId && field.DueAtUtc != null)];
        foreach (NotificationChannel dirtyChannel in dirtyChannels) {
            dirtyChannel.DueAtUtc = DateTime.UtcNow.AddSeconds(-1);
            dirtyChannel.LastSentAtUtc = DateTime.UtcNow.AddSeconds(-2);
        }
        dbContext.SaveChanges();
    }

    // Helpers - Seeding

    private static Guid CreateActiveGroup(Guid ownerUserAccountId, string name, bool isPublic) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = name, OwnerUserAccountId = ownerUserAccountId, IsPublic = isPublic, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = ownerUserAccountId, MemberRole = ChatGroupMemberRole.Owner, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.SaveChanges();
        return groupId;
    }

    private static void AddActiveMember(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }

    private static void SeedDeviceToken(Guid userAccountId, string token) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        dbContext.DeviceTokens.Add(new DeviceToken { Id = Guid.NewGuid(), UserAccountId = userAccountId, Token = token, Platform = "ios", CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.SaveChanges();
    }

    private static ChatGroup BuildDirectGroupRow(Guid pairLowId, Guid pairHighId) {
        DateTime now = DateTime.UtcNow;
        return new ChatGroup { Id = Guid.NewGuid(), Name = "", OwnerUserAccountId = null, IsPublic = false, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now, DirectPairLowId = pairLowId, DirectPairHighId = pairHighId };
    }

    // Helpers - Reading

    private static ChatGroup LoadGroup(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == chatGroupId);
    }

    private static ChatGroupMember LoadMembership(Guid chatGroupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Single(field => field.ChatGroupId == chatGroupId && field.UserAccountId == userAccountId);
    }

    private static List<ChatGroupMember> LoadMemberRows(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return [.. dbContext.ChatGroupMembers.Where(field => field.ChatGroupId == chatGroupId)];
    }

    private static UserAccount LoadUserAccount(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.UserAccounts.Single(field => field.Id == userAccountId);
    }

    private static int CountDirectGroups(Guid firstUserAccountId, Guid secondUserAccountId) {
        (Guid pairLowId, Guid pairHighId) = ChatGroupManager.ComputeDirectPair(firstUserAccountId, secondUserAccountId);
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Count(field => field.DirectPairLowId == pairLowId && field.DirectPairHighId == pairHighId);
    }

    private static int CountMessages(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessages.Count(field => field.ChatGroupId == chatGroupId);
    }

    private static int CountReactions(Guid messageId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessageReactions.Count(field => field.ChatMessageId == messageId);
    }

    private static List<PushMessage> ChatPushes(TestingMockProvidersContainer testingMockProvidersContainer, Guid groupId) {
        return [.. testingMockProvidersContainer.PushProvider.SentMessages.Where(field => field.CollapseId == $"chat-messages-{groupId}" && !field.IsDismiss)];
    }

    private static bool ContainsGroup(JsonElement root, Guid groupId) {
        string target = groupId.ToString();
        foreach (JsonElement element in root.EnumerateArray())
            if (element.GetProperty("id").GetString() == target)
                return true;
        return false;
    }

    private static JsonElement GetGroup(JsonElement root, Guid groupId) {
        string target = groupId.ToString();
        foreach (JsonElement element in root.EnumerateArray())
            if (element.GetProperty("id").GetString() == target)
                return element;
        throw new InvalidOperationException("Chat group was not present in the response.");
    }
}
