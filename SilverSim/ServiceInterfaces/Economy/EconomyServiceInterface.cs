﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Types;
using SilverSim.Types.Economy.Transactions;
using System;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.Economy
{
    public abstract class EconomyServiceInterface
    {
        public interface IActiveTransaction
        {
            /** <summary>Function to finalize transaction</summary> */
            void Commit();
            /** <summary>Function to rollback a transaction</summary> */
            void Rollback(Exception exception);
        }

        public interface IMoneyBalanceAccessor
        {
            int this[UUI agentID] { get; }

            bool TryGetValue(UUI agentID, out int balance);
        }

        public interface IGroupMoneyBalanceAccessor
        {
            int this[UGI groupID] { get; }
            bool TryGetValue(UGI groupID, out int balance);
        }

        public abstract string CurrencySymbol { get; }

        public abstract void Login(UUID sceneID, UUI agentID, UUID sessionID, UUID secureSessionID);

        public abstract void Logout(UUI agentID, UUID sessionID, UUID secureSessionID);

        public abstract IMoneyBalanceAccessor MoneyBalance { get; }

        /** <summary>access group money balance</summary>
         * <exception cref="NotSupportedException">this exception is thrown when the economy service does not support group accounts</exception>
         */
        public virtual IGroupMoneyBalanceAccessor GroupMoneyBalance
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        /** <summary> Start a transaction for paying a grid service</summary>
         * <exception cref="InsufficientFundsException">this exception is thrown when not enough funds are available</exception>
         */
        public abstract IActiveTransaction BeginChargeTransaction(UUI agentID, BaseTransaction transactionData, int amount);

        /** <summary> Start a transaction for paying another user by a user</summary>
         * <exception cref="InsufficientFundsException">this exception is thrown when not enough funds are available</exception>
         */
        public abstract IActiveTransaction BeginTransferTransaction(UUI sourceID, UUI destinationID, BaseTransaction transactionData, int amount);

        /** <summary> Start a transaction for paying a group by a user</summary>
         * <exception cref="InsufficientFundsException">this exception is thrown when not enough funds are available</exception>
         * <exception cref="NotSupportedException">this exception is thrown when the economy service does not support group accounts</exception>
         */
        public virtual IActiveTransaction BeginTransferTransaction(UUI sourceID, UGI destinationID, BaseTransaction transactionData, int amount)
        {
            throw new NotSupportedException();
        }

        /** <summary> Start a transaction for paying a user by a group</summary>
         * <exception cref="InsufficientFundsException">this exception is thrown when not enough funds are available</exception>
         * <exception cref="NotSupportedException">this exception is thrown when the economy service does not support group accounts</exception>
         */
        public virtual IActiveTransaction BeginTransferTransaction(UGI sourceID, UUI destinationID, BaseTransaction transactionData, int amount)
        {
            throw new NotSupportedException();
        }

        /** <summary>Request script debit permission</summary>
         * this function has to throw exception for signaling error
         */
        public virtual UUID RequestScriptDebitPermission(UUI sourceID, UUID regionID, UUID objectID, UUID itemID) => UUID.Zero;

        public void ChargeAmount(UUI agentID, BaseTransaction transactionData, int amount, Action processOperation)
        {
            IActiveTransaction transaction = BeginChargeTransaction(agentID, transactionData, amount);
            try
            {
                processOperation();
            }
            catch(Exception e)
            {
                transaction.Rollback(e);
                throw;
            }
            transaction.Commit();
        }

        /** <summary> Start a transaction for paying another user by a user</summary>
         * <exception cref="InsufficientFundsException">this exception is thrown when not enough funds are available</exception>
         */
        public void TransferMoney(UUI sourceID, UUI destinationID, BaseTransaction transactionData, int amount, Action processOperation)
        {
            IActiveTransaction transaction = BeginTransferTransaction(sourceID, destinationID, transactionData, amount);
            try
            {
                processOperation();
            }
            catch(Exception e)
            {
                transaction.Rollback(e);
                throw;
            }
            transaction.Commit();
        }

        /** <summary> Process a transaction for paying a group by a user</summary>
         * <exception cref="InsufficientFundsException">this exception is thrown when not enough funds are available</exception>
         * <exception cref="NotSupportedException">this exception is thrown when the economy service does not support group accounts</exception>
         */
        public void TransferMoney(UUI sourceID, UGI destinationID, BaseTransaction transactionData, int amount, Action processOperation)
        {
            IActiveTransaction transaction = BeginTransferTransaction(sourceID, destinationID, transactionData, amount);
            try
            {
                processOperation();
            }
            catch (Exception e)
            {
                transaction.Rollback(e);
                throw;
            }
            transaction.Commit();
        }

        /** <summary> Process a transaction for paying a user by a group</summary>
         * <exception cref="InsufficientFundsException">this exception is thrown when not enough funds are available</exception>
         * <exception cref="NotSupportedException">this exception is thrown when the economy service does not support group accounts</exception>
         */
        public void TransferMoney(UGI sourceID, UUI destinationID, BaseTransaction transactionData, int amount, Action processOperation)
        {
            IActiveTransaction transaction = BeginTransferTransaction(sourceID, destinationID, transactionData, amount);
            try
            {
                processOperation();
            }
            catch (Exception e)
            {
                transaction.Rollback(e);
                throw;
            }
            transaction.Commit();
        }

        public enum ConfirmTypeEnum
        {
            None = 0,
            Click = 1,
            Password = 2
        }

        public struct CurrencyQuote
        {
            public int EstimatedUsCents;
            public string EstimatedLocalCost;
            public string LocalCurrency;
            public int CurrencyToBuy;
            public ConfirmTypeEnum ConfirmType;
        }

        public struct CurrencyBuy
        {
            public int EstimatedUsCents;
            public string EstimatedLocalCost;
            public int CurrencyToBuy;
            public ConfirmTypeEnum ConfirmType;
            public string Password;
        }

        public abstract CurrencyQuote GetCurrencyQuote(UUI sourceID, string language, int currencyToBuy);

        public abstract void BuyCurrency(UUI sourceID, string language, CurrencyBuy quote);

        public class UrlAttachedErrorException : Exception
        {
            public readonly string Uri = string.Empty;

            public UrlAttachedErrorException(string message, string uri) : base(message)
            {
                Uri = uri;
            }

            public UrlAttachedErrorException()
            {
            }

            public UrlAttachedErrorException(string message) : base(message)
            {
            }

            public UrlAttachedErrorException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected UrlAttachedErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
                Uri = info.GetString("Uri");
            }

            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                info.AddValue("Uri", Uri);
            }
        }
    }
}
