using System;
using System.Collections.Generic;
using Microsoft.Graph;

namespace azure_ad_console
{
    public interface IAuthProvider
    {
        IEnumerable<Group> GetGroups();
        IEnumerable<User> GetUsers();
        Group CreateGroup(String groupName);
        User CreateUser(String userPrincipalName, String displayName, String mailNickName, String password);
        
    }
}