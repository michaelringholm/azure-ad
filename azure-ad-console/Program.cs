using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace azure_ad_console
{
    class Program
    {
        static void Main(string[] args)
        {
            // https://github.com/microsoftgraph/aspnet-snippets-sample/blob/master/Graph-ASPNET-46-Snippets/Microsoft%20Graph%20ASPNET%20Snippets/Controllers/UsersController.cs
            Console.WriteLine("Started...");             

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration, JSONConfig>();
            serviceCollection.AddSingleton<IAuthProvider, MicrosoftGraphAPI>();
            var builder = serviceCollection.BuildServiceProvider();

            var msGraph = builder.GetService<IAuthProvider>();
            var groups = msGraph.GetGroups();
            var users = msGraph.GetUsers();
            foreach(var group in groups) 
                Console.WriteLine(group.DisplayName + "\t\t\t{" + group.Id + "}");
            foreach(var user in users)
                Console.WriteLine(user.UserPrincipalName + "\t\t\t{" + user.Id + "}");
            Console.WriteLine("Ended!");
        }
    }
}
