﻿using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace OneCSharp.Metadata
{
    public sealed class SQLHelper
    {
        private sealed class SqlFieldInfo
        {
            public SqlFieldInfo() { }
            public int ORDINAL_POSITION;
            public string COLUMN_NAME;
            public string DATA_TYPE;
            public int CHARACTER_MAXIMUM_LENGTH;
            public byte NUMERIC_PRECISION;
            public int NUMERIC_SCALE;
            public bool IS_NULLABLE;
            public bool IsFound;
        }
        private sealed class ClusteredIndexInfo
        {
            public ClusteredIndexInfo() { }
            public string NAME;
            public bool IS_UNIQUE;
            public bool IS_PRIMARY_KEY;
            public List<ClusteredIndexColumnInfo> COLUMNS = new List<ClusteredIndexColumnInfo>();
            public bool HasNullableColumns
            {
                get
                {
                    bool result = false;
                    foreach (ClusteredIndexColumnInfo item in COLUMNS)
                    {
                        if (item.IS_NULLABLE)
                        {
                            return true;
                        }
                    }
                    return result;
                }
            }
            public ClusteredIndexColumnInfo GetColumnByName(string name)
            {
                ClusteredIndexColumnInfo info = null;
                for (int i = 0; i < COLUMNS.Count; i++)
                {
                    if (COLUMNS[i].NAME == name) return COLUMNS[i];
                }
                return info;
            }
        }
        private sealed class ClusteredIndexColumnInfo
        {
            public ClusteredIndexColumnInfo() { }
            public byte KEY_ORDINAL;
            public string NAME;
            public bool IS_NULLABLE;
        }
        private List<SqlFieldInfo> GetSqlFields(string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"SELECT");
            sb.AppendLine(@"    ORDINAL_POSITION, COLUMN_NAME, DATA_TYPE,");
            sb.AppendLine(@"    ISNULL(CHARACTER_MAXIMUM_LENGTH, 0) AS CHARACTER_MAXIMUM_LENGTH,");
            sb.AppendLine(@"    ISNULL(NUMERIC_PRECISION, 0) AS NUMERIC_PRECISION,");
            sb.AppendLine(@"    ISNULL(NUMERIC_SCALE, 0) AS NUMERIC_SCALE,");
            sb.AppendLine(@"    CASE WHEN IS_NULLABLE = 'NO' THEN CAST(0x00 AS bit) ELSE CAST(0x01 AS bit) END AS IS_NULLABLE");
            sb.AppendLine(@"FROM");
            sb.AppendLine(@"    INFORMATION_SCHEMA.COLUMNS");
            sb.AppendLine(@"WHERE");
            sb.AppendLine(@"    TABLE_NAME = N'{0}'");
            sb.AppendLine(@"ORDER BY");
            sb.AppendLine(@"    ORDINAL_POSITION ASC;");

            string sql = string.Format(sb.ToString(), tableName);

            List<SqlFieldInfo> list = new List<SqlFieldInfo>();
            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SqlFieldInfo item = new SqlFieldInfo()
                            {
                                ORDINAL_POSITION = reader.GetInt32(0),
                                COLUMN_NAME = reader.GetString(1),
                                DATA_TYPE = reader.GetString(2),
                                CHARACTER_MAXIMUM_LENGTH = reader.GetInt32(3),
                                NUMERIC_PRECISION = reader.GetByte(4),
                                NUMERIC_SCALE = reader.GetInt32(5),
                                IS_NULLABLE = reader.GetBoolean(6)
                            };
                            list.Add(item);
                        }
                    }
                }
            }
            return list;
        }
        private ClusteredIndexInfo GetClusteredIndexInfo(string tableName)
        {
            ClusteredIndexInfo info = null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"SELECT");
            sb.AppendLine(@"    i.name,");
            sb.AppendLine(@"    i.is_unique,");
            sb.AppendLine(@"    i.is_primary_key,");
            sb.AppendLine(@"    c.key_ordinal,");
            sb.AppendLine(@"    f.name,");
            sb.AppendLine(@"    f.is_nullable");
            sb.AppendLine(@"FROM sys.indexes AS i");
            sb.AppendLine(@"INNER JOIN sys.tables AS t ON t.object_id = i.object_id");
            sb.AppendLine(@"INNER JOIN sys.index_columns AS c ON c.object_id = t.object_id AND c.index_id = i.index_id");
            sb.AppendLine(@"INNER JOIN sys.columns AS f ON f.object_id = t.object_id AND f.column_id = c.column_id");
            sb.AppendLine(@"WHERE");
            sb.AppendLine(@"    t.object_id = OBJECT_ID(@table) AND i.type = 1 -- CLUSTERED");
            sb.AppendLine(@"ORDER BY");
            sb.AppendLine(@"c.key_ordinal ASC;");
            string sql = sb.ToString();

            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                connection.Open();

                command.Parameters.AddWithValue("table", tableName);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        info = new ClusteredIndexInfo()
                        {
                            NAME = reader.GetString(0),
                            IS_UNIQUE = reader.GetBoolean(1),
                            IS_PRIMARY_KEY = reader.GetBoolean(2)
                        };
                        info.COLUMNS.Add(new ClusteredIndexColumnInfo()
                        {
                            KEY_ORDINAL = reader.GetByte(3),
                            NAME = reader.GetString(4),
                            IS_NULLABLE = reader.GetBoolean(5)
                        });
                        while (reader.Read())
                        {
                            info.COLUMNS.Add(new ClusteredIndexColumnInfo()
                            {
                                KEY_ORDINAL = reader.GetByte(3),
                                NAME = reader.GetString(4),
                                IS_NULLABLE = reader.GetBoolean(5)
                            });
                        }
                    }
                }
            }
            return info;
        }


        public void UUID_1C_to_SQL(string UUID)
        {
            // TODO: convert one UUID to another
            //61ac5c5b-6053-4846-bfee-1de510c2baf8 // 1C
            //E51DEEBF-C210-F8BA-4846-605361AC5C5B // SQL
        }
        public void UUID_SQL_to_1C(string UUID)
        {
            // TODO: convert one UUID to another
            //E51DEEBF-C210-F8BA-4846-605361AC5C5B // SQL
            //61ac5c5b-6053-4846-bfee-1de510c2baf8 // 1C
        }

        public string ConnectionString { get; set; }
        public void Load(InfoBase infoBase)
        {
            foreach (Namespace ns in infoBase.Namespaces)
            {
                foreach (var item in ns.DbObjects)
                {
                    GetSQLMetadata(item);
                }
            }
        }
        private void GetSQLMetadata(DbObject metaObject)
        {
            ReadSQLMetadata(metaObject);
            foreach (var nestedObject in metaObject.NestedObjects)
            {
                ReadSQLMetadata(nestedObject);
            }
        }
        private void ReadSQLMetadata(DbObject metaObject)
        {
            List<SqlFieldInfo> sql_fields = GetSqlFields(metaObject.TableName);
            if (sql_fields.Count == 0) return;

            ClusteredIndexInfo indexInfo = this.GetClusteredIndexInfo(metaObject.TableName);
            if (indexInfo == null) { /* TODO: handle situation somehow*/ }

            foreach (var property in metaObject.Properties)
            {
                var fields = sql_fields.Where(f => f.COLUMN_NAME.Contains(property.DbName));
                foreach (var field in fields)
                {
                    AddDbField(property, field, indexInfo);
                    field.IsFound = true;
                }
            }

            int position = 0;
            var nonFounds = sql_fields.Where(f => f.IsFound == false);
            foreach (var field in nonFounds)
            {
                AddDbProperty(metaObject, field, indexInfo, position);
                position++;
                //field.IsFound = true; СтандартныеРеквизиты
            }
        }
        private void AddDbField(DbProperty property, SqlFieldInfo info, ClusteredIndexInfo indexInfo)
        {
            DbField field = new DbField()
            {
                Parent = property,
                Name = info.COLUMN_NAME
            };
            property.Fields.Add(field);
            
            field.TypeName = info.DATA_TYPE;
            field.Length = info.CHARACTER_MAXIMUM_LENGTH;
            field.Precision = info.NUMERIC_PRECISION;
            field.Scale = info.NUMERIC_SCALE;
            field.IsNullable = info.IS_NULLABLE;

            DefineDbFieldPurpose(field);

            if (indexInfo != null)
            {
                ClusteredIndexColumnInfo columnInfo = indexInfo.GetColumnByName(info.COLUMN_NAME);
                if (columnInfo != null)
                {
                    field.IsPrimaryKey = true;
                    field.KeyOrdinal = columnInfo.KEY_ORDINAL;
                }
            }
        }
        private void AddDbProperty(DbObject metaObject, SqlFieldInfo info, ClusteredIndexInfo indexInfo, int position)
        {
            DbProperty property = new DbProperty
            {
                Parent = metaObject,
                Name = info.COLUMN_NAME.Replace("_", string.Empty),
                DbName = info.COLUMN_NAME
            };
            metaObject.Properties.Insert(position, property);
            AddDbField(property, info, indexInfo);
            DefineSystemPropertyType(property);
        }
        private void DefineSystemPropertyType(DbProperty property)
        {
            string name = property.Name;

            if (name == DBToken.IDRRef)
            {
                property.Name = "Ссылка";
                property.Types.Add(new DbType() { Name = "UUID", TypeCode = -6 });
                return;
            }
            else if (name == DBToken.RecorderRRef) // TODO: определять при чтении метаданных из Config
            {
                property.Name = "Регистратор (ссылка)";
                property.Types.Add(new DbType() { Name = "UUID", TypeCode = -6 });
                return;
            }
            else if (name == DBToken.RecorderTRef) // TODO: определять при чтении метаданных из Config
            {
                property.Name = "Регистратор (тип)";
                property.Types.Add(new DbType() { Name = "Numeric", TypeCode = -4 });
                return;
            }
            if (name == DBToken.EnumOrder)
            {
                property.Name = "Порядок";
                property.Types.Add(new DbType() { Name = "Numeric", TypeCode = -4 });
                return;
            }
            else if (name == DBToken.Version)
            {
                if (property.Fields.Count > 0)
                {
                    property.Fields[0].Purpose = DbFieldPurpose.Version;
                }
                property.Name = "Версия";
                property.Types.Add(new DbType() { Name = "Version", TypeCode = -7 });
                return;
            }
            else if (name == DBToken.Marked)
            {
                property.Name = "ПометкаУдаления";
                property.Types.Add(new DbType() { Name = "Boolean", TypeCode = -1 });
                return;
            }
            else if (name == DBToken.DateTime)
            {
                property.Name = "Дата";
                property.Types.Add(new DbType() { Name = "DateTime", TypeCode = -3 });
                return;
            }
            else if (name == DBToken.NumberPrefix)
            {
                property.Name = "МоментВремени";
                property.Types.Add(new DbType() { Name = "DateTime", TypeCode = -3 });
                return;
            }
            else if (name == DBToken.Number)
            {
                property.Name = "Номер";
                if (property.Fields.Count > 0)
                {
                    if (property.Fields[0].TypeName.Contains("char"))
                    {
                        property.Types.Add(new DbType() { Name = "String", TypeCode = -2 });
                    }
                    else
                    {
                        property.Types.Add(new DbType() { Name = "Numeric", TypeCode = -4 });
                    }
                }
                else
                {
                    property.Types.Add(new DbType() { Name = "String", TypeCode = -2 });
                }
                return;
            }
            else if (name == DBToken.Posted)
            {
                property.Name = "Проведён";
                property.Types.Add(new DbType() { Name = "Boolean", TypeCode = -1 });
                return;
            }
            else if (name == DBToken.PredefinedID)
            {
                property.Name = "ИдентификаторПредопределённого";
                property.Types.Add(new DbType() { Name = "UUID", TypeCode = -6 });
                return;
            }
            else if (name == DBToken.Description)
            {
                property.Name = "Наименование";
                property.Types.Add(new DbType() { Name = "String", TypeCode = -2 });
                return;
            }
            else if (name == DBToken.Code)
            {
                property.Name = "Код";
                if (property.Fields.Count > 0)
                {
                    if (property.Fields[0].TypeName.Contains("char"))
                    {
                        property.Types.Add(new DbType() { Name = "String", TypeCode = -2 });
                    }
                    else
                    {
                        property.Types.Add(new DbType() { Name = "Numeric", TypeCode = -4 });
                    }
                }
                else
                {
                    property.Types.Add(new DbType() { Name = "String", TypeCode = -2 });
                }
                return;
            }
            else if (name == DBToken.Folder)
            {
                property.Name = "ЭтоГруппа";
                property.Types.Add(new DbType() { Name = "Boolean", TypeCode = -1 });
                return;
            }
            else if (name == DBToken.KeyField)
            {
                property.Name = "КлючСтроки";
                property.Types.Add(new DbType() { Name = "Numeric", TypeCode = -4 });
                return;
            }
            else if (name.Contains(DBToken.LineNo))
            {
                property.Name = "НомерСтроки";
                property.Types.Add(new DbType() { Name = "Numeric", TypeCode = -4 });
                return;
            }
            else if (name == DBToken.ParentIDRRef)
            {
                property.Name = "Родитель";
                property.Types.Add(new DbType()
                {
                    Name = property.Parent.TableName,
                    TypeCode = property.Parent.TypeCode,
                    DbObject = property.Parent
                });
                return;
            }
            else if (name.Contains(DBToken.OwnerID))
            {
                // На самом деле определеяется при чтении метаданных из файла Config
                // [_OwnerIDRRef] | [_OwnerID_TYPE] + [_OwnerID_RTRef] + [_OwnerID_RRRef]
                return;
            }
            else if (name.Contains(DBToken.IDRRef)) // табличная часть
            {
                property.Name = "Ссылка";
                property.Types.Add(new DbType() { Name = "UUID", TypeCode = -6 });
                return;
            }
            else if (name == DBToken.Period)
            {
                property.Name = "Период";
                property.Types.Add(new DbType() { Name = "DateTime", TypeCode = -3 });
                return;
            }
            else if (name == DBToken.Active)
            {
                property.Name = "Активность";
                property.Types.Add(new DbType() { Name = "Boolean", TypeCode = -1 });
                return;
            }
            else if (name == DBToken.RecordKind) // Перечисление: Приход | Расход
            {
                property.Name = "ВидДвижения";
                property.Types.Add(new DbType() { Name = "Numeric", TypeCode = -4 });
                return;
            }
        }
        private void DefineDbFieldPurpose(DbField field)
        {
            if (string.IsNullOrEmpty(field.Name))
            {
                field.Purpose = DbFieldPurpose.Value;
                return;
            }

            if (field.TypeName == "image" || field.TypeName == "varbinary")
            {
                field.Purpose = DbFieldPurpose.Binary;
                return;
            }

            if (char.IsDigit(field.Name[field.Name.Length - 1]))
            {
                field.Purpose = DbFieldPurpose.Value;
                return;
            }

            if (field.Name.EndsWith(DBToken.RRRef))
            {
                field.Purpose = DbFieldPurpose.Object;
                return;
            }
            else if (field.Name.EndsWith(DBToken.RTRef))
            {
                field.Purpose = DbFieldPurpose.TypeCode;
                return;
            }
            else if (field.Name.EndsWith(DBToken.TYPE))
            {
                field.Purpose = DbFieldPurpose.Discriminator;
                return;
            }
            else if (field.Name.EndsWith(DBToken.RRef))
            {
                field.Purpose = DbFieldPurpose.Object;
                return;
            }
            else if (field.Name.EndsWith(DBToken.TRef))
            {
                field.Purpose = DbFieldPurpose.TypeCode;
                return;
            }
            else if (field.Name.EndsWith(DBToken.S))
            {
                field.Purpose = DbFieldPurpose.String;
                return;
            }
            else if (field.Name.EndsWith(DBToken.N))
            {
                field.Purpose = DbFieldPurpose.Numeric;
                return;
            }
            else if (field.Name.EndsWith(DBToken.L))
            {
                field.Purpose = DbFieldPurpose.Boolean;
                return;
            }
            else if (field.Name.EndsWith(DBToken.T))
            {
                field.Purpose = DbFieldPurpose.DateTime;
                return;
            }

            field.Purpose = DbFieldPurpose.Value;
        }
    }
}
