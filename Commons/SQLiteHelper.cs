﻿using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;
using System.Collections;
using System.Data.SQLite;
using System.Data.Common;
using System.Text;

/// <summary>
/// SQLiteHelper is a utility class similar to "SQLHelper" in MS
/// Data Access Application Block and follows similar pattern.
/// </summary>
public static class SQLiteHelper
{
    public static string connectionString = "data source=MemServerMointorDB.db" + ";Pooling=true;FailIfMissing=false";
    /// <summary>
    /// 获取分页SQL
    /// </summary>
    /// <param name="tblName">表名</param>
    /// <param name="fldSort">排序字段，例如：id asc或简写id，可写多个字段（如果是可重复字段建议最后带上主键，例如name desc,id）</param>
    /// <param name="condition">条件(不需要where)</param>
    /// <param name="start">起始索引，如每页10条则第1页的起始索引为0，第2页的起始索引为10</param>
    /// <param name="count">要取得的数据条数</param>
    /// <returns>返回用于分页的SQL语句</returns>
    private static string GetPagerSQL(string tblName, string fldSort, string condition, int start, int count)
    {
        StringBuilder strSql = new StringBuilder("select * from " + tblName);
        if (!string.IsNullOrEmpty(condition))
        {
            strSql.AppendFormat(" where {0} order by {1}", condition, fldSort);
        }
        else
        {
            strSql.AppendFormat(" order by {0}", fldSort);
        }
        strSql.AppendFormat(" limit {0},{1}", start, count);

        return strSql.ToString();
    }

    /// <summary>
    /// 分页获取数据
    /// </summary>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="tblName">表名</param>
    /// <param name="fldSort">排序字段，例如：id asc或简写id，可写多个字段（如果是可重复字段建议最后带上主键，例如name desc,id）</param>
    /// <param name="condition">条件(不需要where)</param>
    /// <param name="start">起始索引，如每页10条则第1页的起始索引为0，第2页的起始索引为10</param>
    /// <param name="count">要取得的数据条数</param>
    public static DbDataReader GetPageList(string connectionString, string tblName, string fldSort, string condition, int start, int count)
    {
        string sql = GetPagerSQL(tblName, fldSort, condition, start, count);
        return ExecuteReader(connectionString, CommandType.Text, sql, null);
    }

    /// <summary>
    /// 执行查询，返回DataSet
    /// </summary>
    public static DataSet ExecuteQuery(string connectionString, CommandType cmdType, string cmdText,
        params DbParameter[] cmdParms)
    {

        using (SQLiteConnection conn = new SQLiteConnection(connectionString))
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, cmdParms);
                using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                {
                    DataSet ds = new DataSet();
                    da.Fill(ds, "ds");
                    cmd.Parameters.Clear();
                    return ds;
                }
            }
        }
    }

    /// <summary>
    /// 执行查询，返回DataSet
    /// </summary>
    public static DataSet ExecuteQuery( CommandType cmdType, string cmdText,
        params DbParameter[] cmdParms)
    {

        using (SQLiteConnection conn = new SQLiteConnection(connectionString))
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                PrepareCommand(cmd, conn, null, cmdType, cmdText, cmdParms);
                using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                {
                    DataSet ds = new DataSet();
                    da.Fill(ds, "ds");
                    cmd.Parameters.Clear();
                    return ds;
                }
            }
        }
    }

    /// <summary>
    /// 在事务中执行查询，返回DataSet
    /// </summary>
    public static DataSet ExecuteQuery(DbTransaction trans, CommandType cmdType, string cmdText,
        params DbParameter[] cmdParms)
    {

        SQLiteCommand cmd = new SQLiteCommand();
        PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, cmdParms);
        SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
        DataSet ds = new DataSet();
        da.Fill(ds, "ds");
        cmd.Parameters.Clear();
        return ds;

    }

    /// <summary>
    /// 执行 Transact-SQL 语句并返回受影响的行数。
    /// </summary>
    public static int ExecuteNonQuery(string connectionString, CommandType cmdType, string cmdText,
        params DbParameter[] cmdParms)
    {
        lock (connectionString)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    PrepareCommand(cmd, conn, null, cmdType, cmdText, cmdParms);
                    int val = cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                    return val;
                }
            }
        }
    }

    public static int ExecuteNonQuery(CommandType cmdType, string cmdText,
       params DbParameter[] cmdParms)
    {
        lock (connectionString)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    PrepareCommand(cmd, conn, null, cmdType, cmdText, cmdParms);
                    int val = cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                    return val;
                }
            }
        }
    }

    /// <summary>
    /// 在事务中执行 Transact-SQL 语句并返回受影响的行数。
    /// </summary>
    public static int ExecuteNonQuery(DbTransaction trans, CommandType cmdType, string cmdText,
        params DbParameter[] cmdParms)
    {

        SQLiteCommand cmd = new SQLiteCommand();
        PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, cmdParms);
        int val = cmd.ExecuteNonQuery();
        cmd.Parameters.Clear();
        return val;
    }

    /// <summary>
    /// 执行查询，返回DataReader
    /// </summary>
    public static DbDataReader ExecuteReader(string connectionString, CommandType cmdType, string cmdText,
        params DbParameter[] cmdParms)
    {

        SQLiteCommand cmd = new SQLiteCommand();
        SQLiteConnection conn = new SQLiteConnection(connectionString);

        try
        {
            PrepareCommand(cmd, conn, null, cmdType, cmdText, cmdParms);
            SQLiteDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            cmd.Parameters.Clear();
            return rdr;
        }
        catch (Exception e)
        {
            conn.Close();
            throw e;
        }
    }

    /// <summary>
    /// 在事务中执行查询，返回DataReader
    /// </summary>
    public static DbDataReader ExecuteReader(DbTransaction trans, CommandType cmdType, string cmdText,
        params DbParameter[] cmdParms)
    {

        SQLiteCommand cmd = new SQLiteCommand();
        PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, cmdParms);
        SQLiteDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
        cmd.Parameters.Clear();
        return rdr;
    }

    /// <summary>
    /// 执行查询，并返回查询所返回的结果集中第一行的第一列。忽略其他列或行。
    /// </summary>
    public static object ExecuteScalar(string connectionString, CommandType cmdType, string cmdText,
        params DbParameter[] cmdParms)
    {

        SQLiteCommand cmd = new SQLiteCommand();

        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
        {
            PrepareCommand(cmd, connection, null, cmdType, cmdText, cmdParms);
            object val = cmd.ExecuteScalar();
            cmd.Parameters.Clear();
            return val;
        }
    }

    /// <summary>
    /// 执行查询，并返回查询所返回的结果集中第一行的第一列。忽略其他列或行。
    /// </summary>
    public static object ExecuteScalar(CommandType cmdType, string cmdText,
        params DbParameter[] cmdParms)
    {

        SQLiteCommand cmd = new SQLiteCommand();

        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
        {
            PrepareCommand(cmd, connection, null, cmdType, cmdText, cmdParms);
            object val = cmd.ExecuteScalar();
            cmd.Parameters.Clear();
            return val;
        }
    }

    /// <summary>
    /// 在事务中执行查询，并返回查询所返回的结果集中第一行的第一列。忽略其他列或行。
    /// </summary>
    public static object ExecuteScalar(DbTransaction trans, CommandType cmdType, string cmdText,
        params DbParameter[] cmdParms)
    {

        SQLiteCommand cmd = new SQLiteCommand();
        PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, cmdParms);
        object val = cmd.ExecuteScalar();
        cmd.Parameters.Clear();
        return val;
    }

    /// <summary>
    /// 生成要执行的命令
    /// </summary>
    private static void PrepareCommand(DbCommand cmd, DbConnection conn, DbTransaction trans, CommandType cmdType,
        string cmdText, DbParameter[] cmdParms)
    {
        // 如果存在参数，则表示用户是用参数形式的SQL语句，可以替换
        if (cmdParms != null && cmdParms.Length > 0)
            cmdText = cmdText.Replace("?", "@").Replace(":", "@");

        if (conn.State != ConnectionState.Open)
            conn.Open();

        cmd.Connection = conn;
        cmd.CommandText = cmdText;
        if (trans != null)
            cmd.Transaction = trans;
        cmd.CommandType = cmdType;

        if (cmdParms != null)
        {
            foreach (DbParameter parm in cmdParms)
            {
                // 如果存在参数，则表示用户是用参数形式的SQL语句，可以替换
                parm.ParameterName = parm.ParameterName.Replace("?", "@").Replace(":", "@");
                if (parm.Value == null)
                    parm.Value = DBNull.Value;
                cmd.Parameters.Add(parm);
            }
        }
    }

    public static DataSet ExecDataSet(string connStr, string sqlStr)
    {
        using (SQLiteConnection conn = new SQLiteConnection(connStr))
        {
            conn.Open(); SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = sqlStr; cmd.CommandType = CommandType.Text;
            SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            return ds;
        }
    }
}

