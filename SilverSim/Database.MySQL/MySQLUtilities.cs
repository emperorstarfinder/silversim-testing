﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;

namespace SilverSim.Database.MySQL
{
    public static class MySQLUtilities
    {
        #region Connection String Creator
        public static string BuildConnectionString(IConfig config, ILog log)
        {
            if (!(config.Contains("Server") && config.Contains("Username") && config.Contains("Password") && config.Contains("Database")))
            {
                string configName = config.Name;
                if (!config.Contains("Server"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Server' missing in [{0}]", configName);
                }
                if (!config.Contains("Username"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Username' missing in [{0}]", configName);
                }
                if (!config.Contains("Password"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Password' missing in [{0}]", configName);
                }
                if (!config.Contains("Database"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Database' missing in [{0}]", configName);
                }
                throw new ConfigurationLoader.ConfigurationErrorException();
            }
            return String.Format("Server={0};Uid={1};Pwd={2};Database={3};", 
                config.GetString("Server"),
                config.GetString("Username"),
                config.GetString("Password"),
                config.GetString("Database"));
        }
        #endregion

        #region Exceptions
        [Serializable]
        public class MySQLInsertException : Exception
        {
            public MySQLInsertException()
            {

            }

            public MySQLInsertException(string msg)
                : base(msg)
            {

            }

            protected MySQLInsertException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public MySQLInsertException(string msg, Exception innerException)
                : base(msg, innerException)
            {

            }
        }

        [Serializable]
        public class MySQLMigrationException : Exception
        {
            public MySQLMigrationException()
            {

            }

            public MySQLMigrationException(string msg)
                : base(msg)
            {

            }

            protected MySQLMigrationException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public MySQLMigrationException(string msg, Exception innerException)
                : base(msg, innerException)
            {

            }
        }

        [Serializable]
        public class MySQLTransactionException : Exception
        {
            public MySQLTransactionException()
            {

            }

            public MySQLTransactionException(string msg)
                : base(msg)
            {

            }

            protected MySQLTransactionException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public MySQLTransactionException(string msg, Exception inner)
                : base(msg, inner)
            {

            }
        }
        #endregion

        #region Transaction Helper
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public static void InsideTransaction(this MySqlConnection connection, Action del)
        {
            using (MySqlCommand cmd = new MySqlCommand("BEGIN", connection))
            {
                cmd.ExecuteNonQuery();
            }
            try
            {
                del();
            }
            catch(Exception e)
            {
                using (MySqlCommand cmd = new MySqlCommand("ROLLBACK", connection))
                {
                    cmd.ExecuteNonQuery();
                }
                throw new MySQLTransactionException("Transaction failed", e);
            }
            using (MySqlCommand cmd = new MySqlCommand("COMMIT", connection))
            {
                cmd.ExecuteNonQuery();
            }
        }
        #endregion

        #region Push parameters
        public static void AddParameter(this MySqlParameterCollection mysqlparam, string key, object value)
        {
            Type t = value != null ? value.GetType() : null;
            if (t == typeof(Vector3))
            {
                Vector3 v = (Vector3)value;
                mysqlparam.AddWithValue(key + "X", v.X);
                mysqlparam.AddWithValue(key + "Y", v.Y);
                mysqlparam.AddWithValue(key + "Z", v.Z);
            }
            else if (t == typeof(GridVector))
            {
                GridVector v = (GridVector)value;
                mysqlparam.AddWithValue(key + "X", v.X);
                mysqlparam.AddWithValue(key + "Y", v.Y);
            }
            else if (t == typeof(Quaternion))
            {
                Quaternion v = (Quaternion)value;
                mysqlparam.AddWithValue(key + "X", v.X);
                mysqlparam.AddWithValue(key + "Y", v.Y);
                mysqlparam.AddWithValue(key + "Z", v.Z);
                mysqlparam.AddWithValue(key + "W", v.W);
            }
            else if (t == typeof(Color))
            {
                Color v = (Color)value;
                mysqlparam.AddWithValue(key + "Red", v.R);
                mysqlparam.AddWithValue(key + "Green", v.G);
                mysqlparam.AddWithValue(key + "Blue", v.B);
            }
            else if (t == typeof(ColorAlpha))
            {
                ColorAlpha v = (ColorAlpha)value;
                mysqlparam.AddWithValue(key + "Red", v.R);
                mysqlparam.AddWithValue(key + "Green", v.G);
                mysqlparam.AddWithValue(key + "Blue", v.B);
                mysqlparam.AddWithValue(key + "Alpha", v.A);
            }
            else if (t == typeof(EnvironmentController.WLVector2))
            {
                EnvironmentController.WLVector2 vec = (EnvironmentController.WLVector2)value;
                mysqlparam.AddWithValue(key + "X", vec.X);
                mysqlparam.AddWithValue(key + "Y", vec.Y);
            }
            else if (t == typeof(EnvironmentController.WLVector4))
            {
                EnvironmentController.WLVector4 vec = (EnvironmentController.WLVector4)value;
                mysqlparam.AddWithValue(key + "Red", vec.X);
                mysqlparam.AddWithValue(key + "Green", vec.Y);
                mysqlparam.AddWithValue(key + "Blue", vec.Z);
                mysqlparam.AddWithValue(key + "Value", vec.W);
            }
            else if (t == typeof(bool))
            {
                mysqlparam.AddWithValue(key, (bool)value ? 1 : 0);
            }
            else if (t == typeof(UUID) || t == typeof(UUI) || t == typeof(UGI) || t == typeof(Uri))
            {
                mysqlparam.AddWithValue(key, value.ToString());
            }
            else if (t == typeof(AnArray))
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    LlsdBinary.Serialize((AnArray)value, stream);
                    mysqlparam.AddWithValue(key, stream.GetBuffer());
                }
            }
            else if (t == typeof(Date))
            {
                mysqlparam.AddWithValue(key, ((Date)value).AsULong);
            }
            else if (t.IsEnum)
            {
                mysqlparam.AddWithValue(key, Convert.ChangeType(value, t.GetEnumUnderlyingType()));
            }
            else
            {
                mysqlparam.AddWithValue(key, value);
            }
        }

        static void AddParameters(this MySqlParameterCollection mysqlparam, Dictionary<string, object> vals)
        {
            foreach (KeyValuePair<string, object> kvp in vals)
            {
                if (kvp.Value != null)
                {
                    AddParameter(mysqlparam, "?v_" + kvp.Key, kvp.Value);
                }
            }
        }
        #endregion

        #region Common REPLACE INTO/INSERT INTO helper
        public static void AnyInto(this MySqlConnection connection, string cmd, string tablename, Dictionary<string, object> vals)
        {
            List<string> q = new List<string>();
            foreach (KeyValuePair<string, object> kvp in vals)
            {
                object value = kvp.Value;

                Type t = value != null ? value.GetType() : null;
                string key = kvp.Key;

                if (t == typeof(Vector3))
                {
                    q.Add(key + "X");
                    q.Add(key + "Y");
                    q.Add(key + "Z");
                }
                else if (t == typeof(GridVector) || t == typeof(EnvironmentController.WLVector2))
                {
                    q.Add(key + "X");
                    q.Add(key + "Y");
                }
                else if (t == typeof(Quaternion))
                {
                    q.Add(key + "X");
                    q.Add(key + "Y");
                    q.Add(key + "Z");
                    q.Add(key + "W");
                }
                else if (t == typeof(Color))
                {
                    q.Add(key + "Red");
                    q.Add(key + "Green");
                    q.Add(key + "Blue");
                }
                else if(t == typeof(EnvironmentController.WLVector4))
                {
                    q.Add(key + "Red");
                    q.Add(key + "Green");
                    q.Add(key + "Blue");
                    q.Add(key + "Value");
                }
                else if (t == typeof(ColorAlpha))
                {
                    q.Add(key + "Red");
                    q.Add(key + "Green");
                    q.Add(key + "Blue");
                    q.Add(key + "Alpha");
                }
                else if (value == null)
                {
                    /* skip */
                }
                else
                {
                    q.Add(key);
                }
            }

            string q1 = string.Empty;
            string q2 = string.Empty;
            foreach(string p in q)
            {
                if(q1.Length != 0)
                {
                    q1 += ",";
                    q2 += ",";
                }
                q1 += "`" + p + "`";
                q2 += "?v_" + p;
            }

            string query = cmd + " INTO `" + tablename + "` (" + q1 + ") VALUES (" + q2 + ")";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                AddParameters(command.Parameters, vals);
                if (command.ExecuteNonQuery() < 1)
                {
                    throw new MySQLInsertException();
                }
            }
        }
        #endregion

        #region Generate values
        public static string GenerateFieldNames(Dictionary<string, object> vals)
        {
            List<string> q = new List<string>();
            foreach (KeyValuePair<string, object> kvp in vals)
            {
                object value = kvp.Value;

                Type t = value != null ? value.GetType() : null;
                string key = kvp.Key;

                if (t == typeof(Vector3))
                {
                    q.Add(key + "X");
                    q.Add(key + "Y");
                    q.Add(key + "Z");
                }
                else if (t == typeof(GridVector) || t == typeof(EnvironmentController.WLVector2))
                {
                    q.Add(key + "X");
                    q.Add(key + "Y");
                }
                else if (t == typeof(Quaternion))
                {
                    q.Add(key + "X");
                    q.Add(key + "Y");
                    q.Add(key + "Z");
                    q.Add(key + "W");
                }
                else if (t == typeof(Color))
                {
                    q.Add(key + "Red");
                    q.Add(key + "Green");
                    q.Add(key + "Blue");
                }
                else if (t == typeof(EnvironmentController.WLVector4))
                {
                    q.Add(key + "Red");
                    q.Add(key + "Green");
                    q.Add(key + "Blue");
                    q.Add(key + "Value");
                }
                else if (t == typeof(ColorAlpha))
                {
                    q.Add(key + "Red");
                    q.Add(key + "Green");
                    q.Add(key + "Blue");
                    q.Add(key + "Alpha");
                }
                else
                {
                    q.Add(key);
                }
            }

            string q1 = string.Empty;
            foreach (string p in q)
            {
                if (q1.Length != 0)
                {
                    q1 += ",";
                }
                q1 += "`" + p + "`";
            }
            return q1;
        }

        public static string GenerateValues(Dictionary<string, object> vals)
        {
            List<string> resvals = new List<string>();

            foreach (object value in vals.Values)
            {
                Type t = value != null ? value.GetType() : null;
                if (t == typeof(Vector3))
                {
                    Vector3 v = (Vector3)value;
                    resvals.Add(v.X.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.Y.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.Z.ToString(CultureInfo.InvariantCulture));
                }
                else if (t == typeof(GridVector))
                {
                    GridVector v = (GridVector)value;
                    resvals.Add(v.X.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.Y.ToString(CultureInfo.InvariantCulture));
                }
                else if (t == typeof(Quaternion))
                {
                    Quaternion v = (Quaternion)value;
                    resvals.Add(v.X.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.Y.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.Z.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.W.ToString(CultureInfo.InvariantCulture));
                }
                else if (t == typeof(Color))
                {
                    Color v = (Color)value;
                    resvals.Add(v.R.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.G.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.B.ToString(CultureInfo.InvariantCulture));
                }
                else if (t == typeof(ColorAlpha))
                {
                    ColorAlpha v = (ColorAlpha)value;
                    resvals.Add(v.R.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.G.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.B.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.A.ToString(CultureInfo.InvariantCulture));
                }
                else if (t == typeof(EnvironmentController.WLVector2))
                {
                    EnvironmentController.WLVector2 v = (EnvironmentController.WLVector2)value;
                    resvals.Add(v.X.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.Y.ToString(CultureInfo.InvariantCulture));
                }
                else if (t == typeof(EnvironmentController.WLVector4))
                {
                    EnvironmentController.WLVector4 v = (EnvironmentController.WLVector4)value;
                    resvals.Add(v.X.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.Y.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.Z.ToString(CultureInfo.InvariantCulture));
                    resvals.Add(v.W.ToString(CultureInfo.InvariantCulture));
                }
                else if (t == typeof(bool))
                {
                    resvals.Add((bool)value ? "1" : "0");
                }
                else if (t == typeof(UUID) || t == typeof(UUI) || t == typeof(UGI) || t == typeof(Uri))
                {
                    resvals.Add("'" + MySqlHelper.EscapeString(value.ToString()) + "'");
                }
                else if (t == typeof(AnArray))
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        LlsdBinary.Serialize((AnArray)value, stream);
                        byte[] b = stream.GetBuffer();
                        resvals.Add(b.Length == 0 ? "''" : "0x" + b.ToHexString());
                    }
                }
                else if(t == typeof(byte[]))
                {
                    byte[] b = (byte[])value;
                    resvals.Add(b.Length == 0 ? "''" : "0x" + b.ToHexString());
                }
                else if (t == typeof(Date))
                {
                    resvals.Add(((Date)value).AsULong.ToString());
                }
                else if (t == typeof(float))
                {
                    resvals.Add(((float)value).ToString(CultureInfo.InvariantCulture));
                }
                else if (t == typeof(double))
                {
                    resvals.Add(((double)value).ToString(CultureInfo.InvariantCulture));
                }
                else if(t == typeof(string))
                {
                    resvals.Add("'" + MySqlHelper.EscapeString(value.ToString()) + "'");
                }
                else if (null == value)
                {
                    resvals.Add("NULL");
                }
                else if (t.IsEnum)
                {
                    resvals.Add(Convert.ChangeType(value, t.GetEnumUnderlyingType()).ToString());
                }
                else
                {
                    resvals.Add(value.ToString());
                }
            }
            return string.Join(",", resvals);
        }
        #endregion

        #region REPLACE INSERT INTO helper
        public static void ReplaceInto(this MySqlConnection connection, string tablename, Dictionary<string, object> vals)
        {
            connection.AnyInto("REPLACE", tablename, vals);
        }
        #endregion

        #region INSERT INTO helper
        public static void InsertInto(this MySqlConnection connection, string tablename, Dictionary<string, object> vals)
        {
            connection.AnyInto("INSERT", tablename, vals);
        }
        #endregion

        #region UPDATE SET helper
        static List<string> UpdateSetFromVals(Dictionary<string, object> vals)
        {
            List<string> updates = new List<string>();

            foreach (KeyValuePair<string, object> kvp in vals)
            {
                object value = kvp.Value;
                Type t = value != null ? value.GetType() : null;
                string key = kvp.Key;

                if (t == typeof(Vector3))
                {

                    updates.Add("`" + key + "X` = ?v_" + key + "X");
                    updates.Add("`" + key + "Y` = ?v_" + key + "Y");
                    updates.Add("`" + key + "Z` = ?v_" + key + "Z");
                }
                else if (t == typeof(GridVector) || t == typeof(EnvironmentController.WLVector2))
                {
                    updates.Add("`" + key + "X` = ?v_" + key + "X");
                    updates.Add("`" + key + "Y` = ?v_" + key + "Y");
                }
                else if (t == typeof(Quaternion))
                {
                    updates.Add("`" + key + "X` = ?v_" + key + "X");
                    updates.Add("`" + key + "Y` = ?v_" + key + "Y");
                    updates.Add("`" + key + "Z` = ?v_" + key + "Z");
                    updates.Add("`" + key + "W` = ?v_" + key + "W");
                }
                else if (t == typeof(Color))
                {
                    updates.Add("`" + key + "Red` = ?v_" + key + "Red");
                    updates.Add("`" + key + "Green` = ?v_" + key + "Green");
                    updates.Add("`" + key + "Blue` = ?v_" + key + "Blue");
                }
                else if (t == typeof(EnvironmentController.WLVector4))
                {
                    updates.Add("`" + key + "Red` = ?v_" + key + "Red");
                    updates.Add("`" + key + "Green` = ?v_" + key + "Green");
                    updates.Add("`" + key + "Blue` = ?v_" + key + "Blue");
                    updates.Add("`" + key + "Value` = ?v_" + key + "Value");
                }
                else if (t == typeof(ColorAlpha))
                {
                    updates.Add("`" + key + "Red` = ?v_" + key + "Red");
                    updates.Add("`" + key + "Green` = ?v_" + key + "Green");
                    updates.Add("`" + key + "Blue` = ?v_" + key + "Blue");
                    updates.Add("`" + key + "Alpha` = ?v_" + key + "Alpha");
                }
                else if (value == null)
                {
                    /* skip */
                }
                else
                {
                    updates.Add("`" + key + "` = ?v_" + key);
                }
            }
            return updates;
        }

        public static void UpdateSet(this MySqlConnection connection, string tablename, Dictionary<string, object> vals, string where)
        {
            string q1 = "UPDATE " + tablename + " SET ";


            q1 += string.Join(",", UpdateSetFromVals(vals));

            using (MySqlCommand command = new MySqlCommand(q1 + " WHERE " + where, connection))
            {
                AddParameters(command.Parameters, vals);
                if (command.ExecuteNonQuery() < 1)
                {
                    throw new MySQLInsertException();
                }
            }
        }

        public static void UpdateSet(this MySqlConnection connection, string tablename, Dictionary<string, object> vals, Dictionary<string, object> where)
        {
            string q1 = "UPDATE " + tablename + " SET ";

            q1 += string.Join(",", UpdateSetFromVals(vals));

            string wherestr = string.Empty;
            foreach(KeyValuePair<string, object> w in where)
            {
                if(wherestr.Length != 0)
                {
                    wherestr += " AND ";
                }
                wherestr += string.Format("{0} LIKE ?w_{0}", w.Key);
            }

            using (MySqlCommand command = new MySqlCommand(q1 + " WHERE " + wherestr, connection))
            {
                AddParameters(command.Parameters, vals);
                foreach(KeyValuePair<string, object> w in where)
                {
                    command.Parameters.AddWithValue("?w_" + w.Key, w.Value);
                }
                if (command.ExecuteNonQuery() < 1)
                {
                    throw new MySQLInsertException();
                }
            }
        }
        #endregion

        #region Data parsers
        public static EnvironmentController.WLVector4 GetWLVector4(this MySqlDataReader dbReader, string prefix)
        {
            return new EnvironmentController.WLVector4(
                (double)dbReader[prefix + "Red"],
                (double)dbReader[prefix + "Green"],
                (double)dbReader[prefix + "Blue"],
                (double)dbReader[prefix + "Value"]);
        }

        public static T GetEnum<T>(this MySqlDataReader dbreader, string prefix)
        {
            Type enumType = typeof(T).GetEnumUnderlyingType();
            object v = dbreader[prefix];
            return (T)Convert.ChangeType(v, enumType);
        }

        public static UUID GetUUID(this MySqlDataReader dbReader, string prefix)
        {
            object v = dbReader[prefix];
            Type t = v != null ? v.GetType() : null;
            if(t == typeof(Guid))
            {
                return new UUID((Guid)v);
            }
            
            if(t == typeof(string))
            {
                return new UUID((string)v);
            }

            throw new InvalidCastException("GetUUID could not convert value for " + prefix);
        }

        public static UUI GetUUI(this MySqlDataReader dbReader, string prefix)
        {
            object v = dbReader[prefix];
            Type t = v != null ? v.GetType() : null;
            if (t == typeof(Guid))
            {
                return new UUI((Guid)v);
            }

            if (t == typeof(string))
            {
                return new UUI((string)v);
            }

            throw new InvalidCastException("GetUUI could not convert value for " + prefix);
        }

        public static UGI GetUGI(this MySqlDataReader dbReader, string prefix)
        {
            object v = dbReader[prefix];
            Type t = v != null ? v.GetType() : null;
            if (t == typeof(Guid))
            {
                return new UGI((Guid)v);
            }

            if (t == typeof(string))
            {
                return new UGI((string)v);
            }

            throw new InvalidCastException("GetUGI could not convert value for " + prefix);
        }

        public static Date GetDate(this MySqlDataReader dbReader, string prefix)
        {
            ulong v;
            if (!ulong.TryParse(dbReader[prefix].ToString(), out v))
            {
                throw new InvalidCastException("GetDate could not convert value for "+ prefix);
            }
            return Date.UnixTimeToDateTime(v);
        }

        public static EnvironmentController.WLVector2 GetWLVector2(this MySqlDataReader dbReader, string prefix)
        {
            return new EnvironmentController.WLVector2(
                (double)dbReader[prefix + "X"],
                (double)dbReader[prefix + "Y"]);
        }

        public static Vector3 GetVector3(this MySqlDataReader dbReader, string prefix)
        {
            return new Vector3(
                (double)dbReader[prefix + "X"],
                (double)dbReader[prefix + "Y"],
                (double)dbReader[prefix + "Z"]);
        }

        public static Quaternion GetQuaternion(this MySqlDataReader dbReader, string prefix)
        {
            return new Quaternion(
                (double)dbReader[prefix + "X"],
                (double)dbReader[prefix + "Y"],
                (double)dbReader[prefix + "Z"],
                (double)dbReader[prefix + "W"]);
        }

        public static Color GetColor(this MySqlDataReader dbReader, string prefix)
        {
            return new Color(
                (double)dbReader[prefix + "Red"],
                (double)dbReader[prefix + "Green"],
                (double)dbReader[prefix + "Blue"]);
        }

        public static ColorAlpha GetColorAlpha(this MySqlDataReader dbReader, string prefix)
        {
            return new ColorAlpha(
                (double)dbReader[prefix + "Red"],
                (double)dbReader[prefix + "Green"],
                (double)dbReader[prefix + "Blue"],
                (double)dbReader[prefix + "Alpha"]);
        }

        public static bool GetBool(this MySqlDataReader dbReader, string prefix)
        {
            object o = dbReader[prefix];
            Type t = o != null ? o.GetType() : null;
            if(t == typeof(uint))
            {
                return (uint)o != 0;
            }
            else if(t == typeof(int))
            {
                return (int)o != 0;
            }
            else if(t == typeof(sbyte))
            {
                return (sbyte)o != 0;
            }
            else if (t == typeof(byte))
            {
                return (byte)o != 0;
            }
            else
            {
                throw new InvalidCastException("GetBoolean could not convert value for " + prefix + ": got type " + o.GetType().FullName);
            }
        }

        public static byte[] GetBytes(this MySqlDataReader dbReader, string prefix)
        {
            object o = dbReader[prefix];
            Type t = o != null ? o.GetType() : null;
            if(t == typeof(DBNull))
            {
                return new byte[0];
            }
            return (byte[])o;
        }

        public static Uri GetUri(this MySqlDataReader dbReader, string prefix)
        {
            object o = dbReader[prefix];
            Type t = o != null ? o.GetType() : null;
            if(t == typeof(DBNull))
            {
                return null;
            }
            string s = (string)o;
            if(s.Length == 0)
            {
                return null;
            }
            return new Uri(s);
        }

        public static GridVector GetGridVector(this MySqlDataReader dbReader, string prefix)
        {
            return new GridVector(dbReader.GetUInt32(prefix + "X"), dbReader.GetUInt32(prefix + "Y"));
        }
        #endregion

        #region Migrations helper
        public static uint GetTableRevision(this MySqlConnection connection, string name)
        {
            using(MySqlCommand cmd = new MySqlCommand("SHOW TABLE STATUS WHERE name=?name", connection))
            {
                cmd.Parameters.AddWithValue("?name", name);
                using (MySqlDataReader dbReader = cmd.ExecuteReader())
                {
                    if (dbReader.Read())
                    {
                        uint u;
                        if(!uint.TryParse((string)dbReader["Comment"], out u))
                        {
                            throw new InvalidDataException("Comment is not a parseable number");
                        }
                        return u;
                    }
                }
            }
            return 0;
        }
        #endregion
    }
}
