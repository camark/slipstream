﻿using System;
using System.Net;


namespace ObjectServer.Client
{
    public interface IObjectServerClient : IRemoteService
    {
        string SessionToken { get; }
        string LoggedUserName { get; }
        string LoggedDatabase { get; }
        Uri Uri { get; }
        Uri ServerAddress { get; }

        bool Logged { get; }
    }
}
