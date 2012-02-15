using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Thrift;
using Thrift.Protocol;
using Thrift.Transport;
using Evernote.EDAM.Type;
using Evernote.EDAM.UserStore;
using Evernote.EDAM.NoteStore;
using Evernote.EDAM.Error;


namespace en2ki
{
    internal class EvernoteHelper
    {
        internal static NoteStore.Client GetNoteStoreClient(String edamBaseUrl, User user)
        {
            Uri noteStoreUrl = new Uri(edamBaseUrl + "/edam/note/" + user.ShardId);
            TTransport noteStoreTransport = new THttpClient(noteStoreUrl);
            TProtocol noteStoreProtocol = new TBinaryProtocol(noteStoreTransport);
            NoteStore.Client noteStore = new NoteStore.Client(noteStoreProtocol);
            return noteStore;
        }

        internal static UserStore.Client GetUserStoreClient(string edamBaseUrl)
        {
            Uri userStoreUrl = new Uri(edamBaseUrl + "/edam/user");
            TTransport userStoreTransport = new THttpClient(userStoreUrl);
            TProtocol userStoreProtocol = new TBinaryProtocol(userStoreTransport);
            UserStore.Client userStore = new UserStore.Client(userStoreProtocol);
            return userStore;
        }

        internal static bool VerifyEDAM(UserStore.Client userStore)
        {
            bool versionOK =
                userStore.checkVersion("C# EDAMTest",
                   Evernote.EDAM.UserStore.Constants.EDAM_VERSION_MAJOR,
                   Evernote.EDAM.UserStore.Constants.EDAM_VERSION_MINOR);
            Console.WriteLine("Is my EDAM protocol version up to date? " + versionOK);
            return versionOK;
        }

        internal static AuthenticationResult Authenticate(String username, String password, String consumerKey, String consumerSecret, String evernoteHost, UserStore.Client userStore)
        {
            AuthenticationResult authResult;
            try
            {
                authResult = userStore.authenticate(username, password, consumerKey, consumerSecret);
            }
            catch (EDAMUserException ex)
            {
                String parameter = ex.Parameter;
                EDAMErrorCode errorCode = ex.ErrorCode;

                if (parameter.ToLower() == "consumerkey")
                {
                    throw new ApplicationException("API Key Missing. \r\n Please download latest en2ki release from homepage");
                }
                else
                {
                    throw new ApplicationException(String.Format("Authentication Failed \r\n (Make sure {0} is correct)", parameter));
                }
            }
            return authResult;
        }


    }
}
