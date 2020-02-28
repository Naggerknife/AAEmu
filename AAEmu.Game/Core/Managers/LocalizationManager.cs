using NLog;
using AAEmu.Commons.Utils;
using System.Collections.Generic;
using AAEmu.Game.Utils.DB;
using AAEmu.Game.Models.Game.Items;
using AAEmu.Game.Models.Game;

namespace AAEmu.Game.Core.Managers
{
    public class LocalizationManager : Singleton<LocalizationManager>
    {
        private static Logger _log = LogManager.GetCurrentClassLogger();

        private Dictionary<string, Dictionary<string, Dictionary<uint, LocalizedText>>> _localizedTexts;

        public void Load()
        {
            _localizedTexts = new Dictionary<string, Dictionary<string, Dictionary<uint, LocalizedText>>>();
            _log.Info("Loading Localization Manager...");

            using (var connection = SQLite.CreateConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM localized_texts";
                    command.Prepare();
                    using (var reader = new SQLiteWrapperReader(command.ExecuteReader()))
                    {
                        while (reader.Read())
                        {
                            var template = new LocalizedText();
                            template.TableName = reader.GetString("tbl_name");
                            template.ColumnName = reader.GetString("tbl_column_name");
                            template.Index = reader.GetUInt32("idx");
                            template.English = reader.GetString("en_us");
                            //template.Id = reader.GetUInt32("id");     
                            //template.Korean = reader.GetString("ko"); 
                            //template.Chinese = reader.GetString("zh_cn"); 
                            //template.Japanese = reader.GetString("ja"); 
                            //template.Russian = reader.GetString("ru"); 
                            //template.Taiwanese = reader.GetString("zh_tw"); 
                            //template.German = reader.GetString("de"); 
                            //template.French = reader.GetString("fr");

                            if (!_localizedTexts.ContainsKey(template.TableName))
                                _localizedTexts.Add(template.TableName, new Dictionary<string, Dictionary<uint, LocalizedText>>());

                            if (!_localizedTexts[template.TableName].ContainsKey(template.ColumnName))
                                _localizedTexts[template.TableName].Add(template.ColumnName, new Dictionary<uint, LocalizedText>());

                            if (!_localizedTexts[template.TableName][template.ColumnName].ContainsKey(template.Index))
                                _localizedTexts[template.TableName][template.ColumnName].Add(template.Index, template);
                        }
                    }
                }
            }
        }

        public string GetEnglishLocalizedText(string tableName, string columnName, uint id)
        {
            if (_localizedTexts.ContainsKey(tableName) && _localizedTexts[tableName].ContainsKey(columnName) && _localizedTexts[tableName][columnName].ContainsKey(id))
                return _localizedTexts[tableName][columnName][id].English;
            else
                return null;
        }
    }
}
