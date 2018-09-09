using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WRSDataMigrationInt.Infrastructure.DBHelper
{
    public static class DateTimeExtension
    {
        public static readonly DateTime DBNull = Convert.ToDateTime("1900-01-01 00:00:00");

        /// <summary>
        /// 判断DateTime在数据库中是否存储为象征意义的null
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static bool IsDBNull(this DateTime dateTime)
        {
            return dateTime == DBNull;
        }

        /// <summary>
        /// 判断DateTime在数据库中是否存储为象征意义的null
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static bool IsDBNull(this DateTime? dateTime)
        {
            return dateTime == null || dateTime == DBNull;
        }

        /// <summary>
        /// 格式化为字符串
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string Format(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

    }
}
