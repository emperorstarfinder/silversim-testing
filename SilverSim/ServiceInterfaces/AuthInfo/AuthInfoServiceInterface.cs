﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.AuthInfo;
using System;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.AuthInfo
{
    public class VerifyTokenFailedException : Exception
    {
        public VerifyTokenFailedException()
        {

        }

        public VerifyTokenFailedException(string message)
             : base(message)
        {

        }

        protected VerifyTokenFailedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        {

        }

        public VerifyTokenFailedException(string message, Exception innerException)
        : base(message, innerException)
        {

        }
    }

    public class AuthenticationFailedException : Exception
    {
        public AuthenticationFailedException()
        {

        }

        public AuthenticationFailedException(string message)
             : base(message)
        {

        }

        protected AuthenticationFailedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        {

        }

        public AuthenticationFailedException(string message, Exception innerException)
        : base(message, innerException)
        {

        }
    }

    public abstract class AuthInfoServiceInterface
    {
        protected AuthInfoServiceInterface()
        {

        }

        public abstract UserAuthInfo this[UUID accountid] { get; }
        public abstract void Store(UserAuthInfo info);

        public abstract UUID AddToken(UUID principalId, UUID sessionid, int lifetime_in_minutes);
        public abstract void VerifyToken(UUID principalId, UUID token, int lifetime_extension_in_minutes);
        public abstract void ReleaseToken(UUID accountId, UUID secureSessionId);
        public abstract void ReleaseTokenBySession(UUID accountId, UUID sessionId);

        public virtual UUID Authenticate(UUID sessionId, UUID principalId, string password, int lifetime_in_minutes)
        {
            if (!password.StartsWith("$1$"))
            {
                password = password.ComputeMD5();
            }
            else
            {
                password = password.Substring(3);
            }
            UserAuthInfo uai = this[principalId];
            string salted = (password + ":" + uai.PasswordSalt).ComputeMD5();

            if (salted != uai.PasswordHash)
            {
                throw new AuthenticationFailedException("Could not authenticate your avatar. Please check your username and password, and check the grid if problems persist.");
            }
            return AddToken(uai.ID, sessionId, 30);
        }
    }
}
